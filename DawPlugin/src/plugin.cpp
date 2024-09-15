
#include "DistrhoPlugin.hpp"
#include "asio.hpp"
#include "choc/containers/choc_Value.h"
#include "choc/memory/choc_Base64.h"
#include "choc/text/choc_JSON.h"
#include "extra/String.hpp"
#include "gzip/compress.hpp"
#include "gzip/decompress.hpp"
#include "uuid_v4/uuid_v4.h"
#include <filesystem>
#include <random>
#include <vector>

START_NAMESPACE_DISTRHO

// -----------------------------------------------------------------------------------------------------------

/**
  Plugin to show how to get some basic information sent to the UI.
 */
class OpenUtauPlugin : public Plugin {
public:
  OpenUtauPlugin()
      : Plugin(0, 0, 3),
        acceptor(io_service, asio::ip::tcp::endpoint(asio::ip::tcp::v4(), 0)) {
    port = acceptor.local_endpoint().port();
    acceptor.async_accept(std::bind(&OpenUtauPlugin::onAccept, this,
                                    std::placeholders::_1,
                                    std::placeholders::_2));

    UUIDv4::UUIDGenerator<std::mt19937_64> uuidGenerator;
    UUIDv4::UUID uuid = uuidGenerator.getUUID();
    this->uuid = uuid.str();

    std::filesystem::path tempPath = std::filesystem::temp_directory_path();
    std::filesystem::path socketPath = tempPath / "OpenUtau" / "PluginServers" /
                                       std::format("{}.json", uuid.str());

    this->socketPath = socketPath;
  }
  ~OpenUtauPlugin() override {
    io_service.stop();
    std::filesystem::remove(this->socketPath);
  }

protected:
  /* --------------------------------------------------------------------------------------------------------
   * Information */

  /**
     Get the plugin label.
     This label is a short restricted name consisting of only _, a-z, A-Z and
     0-9 characters.
   */
  const char *getLabel() const override { return "OpenUtau Bridge"; }

  /**
     Get an extensive comment/description about the plugin.
   */
  const char *getDescription() const override {
    return "Plugin to show how to get some basic information sent to the UI.";
  }

  /**
     Get the plugin author/maker.
   */
  const char *getMaker() const override { return "stakira"; }

  /**
     Get the plugin homepage.
   */
  const char *getHomePage() const override {
    return "https://github.com/stakira/OpenUtau/";
  }

  void initState(uint32_t index, State &state) override {
    switch (index) {
    case 0:
      state.key = "name";
      state.label = "Plugin Name";
      state.defaultValue = "";
      break;
    case 1:
      state.key = "port";
      state.label = "Port";
      break;
    case 2:
      state.key = "ustx";
      state.label = "USTx";
      break;
    case 3:
      state.key = "mixes";
      state.label = "Mixes";
      break;
    }
  }

  String getState(const char *key) const override {
    // DPF cannot handle binary data, so we need to encode it to base64

    if (strcmp(key, "name") == 0) {
      return String(name.c_str());
    } else if (strcmp(key, "port") == 0) {
      return String(port);
    } else if (strcmp(key, "ustx") == 0) {
      std::string encoded = choc::base64::encodeToString(ustx);
      return String(encoded.c_str());
    } else if (strcmp(key, "mixes") == 0) {
      choc::value::Value value = choc::value::createEmptyArray();
      for (const auto &mix : mixes) {
        std::string encoded = choc::base64::encodeToString(mix);
        value.addArrayElement(encoded);
      }

      std::string json = choc::json::toString(value);
      std::string compressed = gzip::compress(json.c_str(), json.size());
      std::string encoded = choc::base64::encodeToString(compressed);

      return String(encoded.c_str());
    }
    return String();
  }

  void setState(const char *key, const char *value) override {
    if (strcmp(key, "name") == 0) {
      this->name = value;
    } else if (strcmp(key, "port") == 0) {
    } else if (strcmp(key, "ustx") == 0) {
      std::vector<uint8_t> decoded;
      choc::base64::decodeToContainer(decoded, value);
      this->ustx = std::string(decoded.begin(), decoded.end());
    } else if (strcmp(key, "mixes") == 0) {
      std::vector<uint8_t> decoded;
      choc::base64::decodeToContainer(decoded, value);
      std::string decompressed =
          gzip::decompress((char *)decoded.data(), decoded.size());
      choc::value::Value value = choc::json::parse(std::string(decompressed));
      std::vector<std::vector<float>> mixes;
      for (choc::value::ValueView encodedValue : value) {
        std::string encoded(encodedValue.getString());
        std::vector<uint8_t> decoded;
        choc::base64::decodeToContainer(decoded, encoded);
        std::vector<float> mix;
        mix.resize(decoded.size() / 4);
        for (size_t i = 0; i < decoded.size(); i += 4) {
          mix[i / 4] = *(float *)&decoded[i];
        }
        mixes.push_back(mix);
      }

      this->mixes = mixes;
      this->resample();
    }
  }

  /**
     Get the plugin license name (a single line of text).
     For commercial plugins this should return some short copyright information.
   */
  const char *getLicense() const override { return "ISC"; }

  /**
     Get the plugin version, in hexadecimal.
   */
  uint32_t getVersion() const override { return d_version(1, 0, 0); }

  /* --------------------------------------------------------------------------------------------------------
   * Init */

  /**
     Initialize the audio port @a index.@n
     This function will be called once, shortly after the plugin is created.
   */
  void initAudioPort(bool input, uint32_t index, AudioPort &port) override {
    // treat meter audio ports as stereo
    port.groupId = kPortGroupStereo;

    // everything else is as default
    Plugin::initAudioPort(input, index, port);
  }

  /* --------------------------------------------------------------------------------------------------------
   * Audio/MIDI Processing */

  /**
     Run/process function for plugins without MIDI input.
     @note Some parameters might be null if there are no audio inputs or
     outputs.
   */
  void run(const float **inputs, float **outputs, uint32_t frames,
           const MidiEvent *midiEvents, uint32_t midiEventCount) override {
    float *const outL = outputs[0];
    float *const outR = outputs[1];

    for (uint32_t i = 0; i < frames; ++i) {
      outL[i] = this->resampledMixes[0][i];
      outR[i] = this->resampledMixes[1][i];
    }
  };

  /* --------------------------------------------------------------------------------------------------------
   * Callbacks (optional) */

  /**
     Optional callback to inform the plugin about a buffer size change.
     This function will only be called when the plugin is deactivated.
     @note This value is only a hint!
           Hosts might call run() with a higher or lower number of frames.
   */
  void bufferSizeChanged(uint32_t newBufferSize) override {}

  void sampleRateChanged(double newSampleRate) override {
    this->sampleRate = newSampleRate;
    this->resample();
  }

  // -------------------------------------------------------------------------------------------------------

private:
  void onAccept(const asio::error_code &error, asio::ip::tcp::socket socket) {

    acceptor.async_accept(std::bind(&OpenUtauPlugin::onAccept, this,
                                    std::placeholders::_1,
                                    std::placeholders::_2));
    if (!error) {
      if (!inUse) {
        inUse = true;
        socket.write_some(asio::buffer(this->formatMessage(
            "init",
            choc::value::createObject(""))));
        std::string messageBuffer;
        char buffer[1024];
        while (true) {
          size_t len = socket.read_some(asio::buffer(buffer));
          if (len == 0) {
            break;
          }
          messageBuffer.append(buffer, len);

          size_t pos;
          while ((pos = messageBuffer.find('\n')) != std::string::npos) {
            std::string message = messageBuffer.substr(0, pos);
            messageBuffer.erase(0, pos + 1);
            if (message == "close") {
              socket.close();
              inUse = false;
              return;
            }

            size_t sep = message.find(' ');
            std::string kind = message.substr(0, sep);
            std::string payload = message.substr(sep + 1);
            choc::value::ValueView value = choc::json::parse(payload);

            this->onMessage(kind, value);
          }
        }

        socket.close();
        inUse = false;
      } else {
        socket.write_some(asio::buffer(this->formatMessage(
            "error",
            choc::value::createObject(
                "", "message", "Plugin is connected to another client"))));
        socket.close();
      }
    }
  }

  void onMessage(const std::string &kind,
                 const choc::value::ValueView &payload) {
    if (kind == "status") {
      std::string json = choc::json::toString(payload);
      setState("status", json.c_str());
    }
  }

  std::string formatMessage(const std::string &kind,
                            const choc::value::ValueView &payload) {
    std::string json = choc::json::toString(payload);
    return std::format("{} {}", kind, json);
  }

  void resample() {
    // OpenUtau returns 44100Hz audio, 2 channels, 32-bit float
    std::vector<std::vector<float>> resampledMixes;
    for (const auto &mix : mixes) {
      std::vector<float> resampled;
      auto numSamples = mix.size();
      resampled.resize(std::ceil(numSamples * 44100.0 / sampleRate));
      for (int lr = 0; lr < 2; lr++) {
        int i = -2;
        for (float samplePos = 0; samplePos < numSamples;
             samplePos += 44100.0 / sampleRate * 2) {
          i += 2;
          auto i0 = std::floor(samplePos) + lr;
          auto i1 = std::ceil(samplePos) + lr + 1;
          auto f = samplePos - i0;
          resampled[i + lr] = mix[i0] * (1 - f) + mix[i1] * f;
        }
      }

      resampledMixes.push_back(resampled);
    }
    this->resampledMixes = resampledMixes;
  }

  int port;
  double sampleRate;
  bool inUse;
  std::string name;
  std::string ustx;
  std::vector<std::vector<float>> mixes;
  std::vector<std::vector<float>> resampledMixes;
  std::string uuid;

  std::filesystem::path socketPath;

  asio::io_service io_service;
  asio::ip::tcp::acceptor acceptor;

  /**
     Set our plugin class as non-copyable and add a leak detector just in case.
   */
  DISTRHO_DECLARE_NON_COPYABLE_WITH_LEAK_DETECTOR(OpenUtauPlugin)
};

/* ------------------------------------------------------------------------------------------------------------
 * Plugin entry point, called by DPF to create a new plugin instance. */

Plugin *createPlugin() { return new OpenUtauPlugin(); }

// -----------------------------------------------------------------------------------------------------------

END_NAMESPACE_DISTRHO

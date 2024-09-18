#include "plugin.hpp"
#include "DistrhoPlugin.hpp"
#include "asio.hpp"
#include "choc/containers/choc_Value.h"
#include "choc/memory/choc_Base64.h"
#include "choc/text/choc_JSON.h"
#include "extra/String.hpp"
#include "gzip/compress.hpp"
#include "gzip/decompress.hpp"
#include "uuid/v4/uuid.h"
#include <cstdio>
#include <filesystem>
#include <fstream>
#include <string>
#include <thread>
#include <vector>

std::vector<uint8_t> gunzip(const char *data, size_t size) {
  std::vector<uint8_t> decompressed;
  gzip::Decompressor decompressor;
  decompressor.decompress(decompressed, data, size);
  return decompressed;
}

std::jthread *ioThread = nullptr;
asio::io_context *ioContext = nullptr;
asio::io_context &getIoContext() {
  if (ioContext == nullptr) {
    ioContext = new asio::io_context();
    ioThread = new std::jthread([&]() { ioContext->run(); });
    std::atexit([]() {
      ioContext->stop();
      ioThread->join();
      delete ioThread;
      delete ioContext;
    });
  }
  return *ioContext;
}

// note: OpenUtau returns 44100Hz, 2ch, 32bit float audio

START_NAMESPACE_DISTRHO

// -----------------------------------------------------------------------------------------------------------
OpenUtauPlugin::OpenUtauPlugin()
    : Plugin(0, 0, 3)

{

  if (this->isDummyInstance()) {
    return;
  }

  this->mixes = std::make_shared<std::vector<std::vector<float>>>();

  auto currentTime = std::chrono::system_clock::now();
  std::string uuid = uuid::v4::UUID::New().String();
  setState("uuid", uuid.c_str());

  setState("name", uuid.c_str());
  this->inUse = false;
}
OpenUtauPlugin::~OpenUtauPlugin() {
  if (std::filesystem::exists(this->socketPath)) {
    std::filesystem::remove(this->socketPath);
  }
  if (acceptor) {
    acceptor->close();
  }
}

/* --------------------------------------------------------------------------------------------------------
 * Information */

/**
       Get the plugin label.
       This label is a short restricted name consisting of only _, a-z, A-Z
   and 0-9 characters.
 */
const char *OpenUtauPlugin::getLabel() const { return "OpenUtau"; }

/**
   Get an extensive comment/description about the plugin.
 */
const char *OpenUtauPlugin::getDescription() const {
  return "Plugin to show how to get some basic information sent to the UI.";
}

/**
   Get the plugin author/maker.
 */
const char *OpenUtauPlugin::getMaker() const { return "stakira"; }

/**
   Get the plugin homepage.
 */
const char *OpenUtauPlugin::getHomePage() const {
  return "https://github.com/stakira/OpenUtau/";
}

void OpenUtauPlugin::initState(uint32_t index, State &state) {
  switch (index) {
  case 0:
    state.key = "name";
    state.label = "Plugin Name";
    state.defaultValue = "";
    break;
  case 1:
    state.key = "ustx";
    state.label = "USTx";
    break;
  case 2:
    state.key = "mixes";
    state.label = "Mixes";
    break;
  }
}

String OpenUtauPlugin::getState(const char *key) const {
  // DPF cannot handle binary data, so we need to encode it to base64

  if (strcmp(key, "name") == 0) {
    return String(name.c_str());
  } else if (strcmp(key, "ustx") == 0) {
    std::string encoded = choc::base64::encodeToString(ustx);
    return String(encoded.c_str());
  } else if (strcmp(key, "mixes") == 0) {
    choc::value::Value value = choc::value::createEmptyArray();
    for (const auto &mix : *mixes) {
      std::string compressed =
          gzip::compress((char *)mix.data(), mix.size() * sizeof(float));
      std::string encoded = choc::base64::encodeToString(compressed);
      value.addArrayElement(encoded);
    }

    return String(choc::json::toString(value).c_str());
  }
  return String();
}

void OpenUtauPlugin::setState(const char *key, const char *value) {
  if (strcmp(key, "name") == 0) {
    this->name = value;
  } else if (strcmp(key, "uuid") == 0) {
    this->uuid = value;
  } else if (strcmp(key, "ustx") == 0) {
    std::vector<uint8_t> decoded;
    choc::base64::decodeToContainer(decoded, value);
    this->ustx = std::string(decoded.begin(), decoded.end());
  } else if (strcmp(key, "mixes") == 0) {
    choc::value::Value jsonValue = choc::json::parse(std::string(value));
    std::vector<std::vector<float>> mixes;
    for (choc::value::ValueView encodedValue : jsonValue) {
      std::string encoded(encodedValue.getString());
      std::vector<uint8_t> decoded;
      choc::base64::decodeToContainer(decoded, encoded);
      std::vector<uint8_t> decompressed =
          gunzip((char *)decoded.data(), decoded.size());
      std::vector<float> mix((float *)decompressed.data(),
                             (float *)decompressed.data() +
                                 decompressed.size() / sizeof(float));
      mixes.push_back(mix);
    }

    *this->mixes = mixes;
  }
}

/**
   Get the plugin license name (a single line of text).
   For commercial plugins this should return some short copyright information.
 */
const char *OpenUtauPlugin::getLicense() const { return "ISC"; }

/**
   Get the plugin version, in hexadecimal.
 */
uint32_t OpenUtauPlugin::getVersion() const { return d_version(1, 0, 0); }

/* --------------------------------------------------------------------------------------------------------
 * Init */

/**
       Initialize the audio port @a index.@n
       This function will be called once, shortly after the plugin is created.
 */
void OpenUtauPlugin::initAudioPort(bool input, uint32_t index,
                                   AudioPort &port) {
  // treat meter audio ports as stereo
  port.groupId = kPortGroupStereo;

  // everything else is as default
  Plugin::initAudioPort(input, index, port);
}

/* --------------------------------------------------------------------------------------------------------
 * Audio/MIDI Processing */

void OpenUtauPlugin::run(const float **inputs, float **outputs, uint32_t frames,
                         const MidiEvent *midiEvents, uint32_t midiEventCount) {
  initializeNetwork();
  float *const outL = outputs[0];
  float *const outR = outputs[1];
  auto timePosition = this->getTimePosition();

  for (uint32_t i = 0; i < frames; ++i) {
    outL[i] = 0;
    outR[i] = 0;
  }
  if (this->mixes->size() > 0 && timePosition.playing) {
    auto sampleRate = getSampleRate();
    auto mixes = this->mixes;
    for (uint32_t i = 0; i < frames; ++i) {
      auto frame = (i + timePosition.frame) * 44100.0 / sampleRate;
      for (uint8_t lr = 0; lr < 2; ++lr) {
        float mix = 0;
        auto left = (uint32_t)frame;
        auto right = left + 1;
        auto leftSampleIndex = left * 2 + lr;
        auto rightSampleIndex = right * 2 + lr;
        auto ratio = frame - left;
        if (leftSampleIndex < (*mixes)[0].size() &&
            rightSampleIndex < (*mixes)[0].size()) {
          auto leftSample = (*mixes)[0][leftSampleIndex];
          auto rightSample = (*mixes)[0][rightSampleIndex];
          mix = leftSample * (1 - ratio) + rightSample * ratio;

          if (lr == 0) {
            outL[i] = mix;
          } else {
            outR[i] = mix;
          }
        }
      }
    }
  }
};

/* --------------------------------------------------------------------------------------------------------
 * Callbacks (optional) */

/**
       Optional callback to inform the plugin about a buffer size change.
       This function will only be called when the plugin is deactivated.
       @note This value is only a hint!
                 Hosts might call run() with a higher or lower number of
   frames.
 */
void OpenUtauPlugin::bufferSizeChanged(uint32_t newBufferSize) {}

void OpenUtauPlugin::sampleRateChanged(double newSampleRate) {}
void OpenUtauPlugin::onAccept(std::shared_ptr<OpenUtauPlugin> self,
                              const asio::error_code &error,
                              asio::ip::tcp::socket socket) {
  if (!error) {
    self->willAccept();
    if (!self->inUse) {
      self->inUse = true;
      socket.write_some(
          asio::buffer(formatMessage("init", choc::value::createObject(""))));
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
            self->inUse = false;
            return;
          }

          size_t sep = message.find(' ');
          std::string kind = message.substr(0, sep);
          std::string payload = message.substr(sep + 1);
          choc::value::Value value = choc::json::parse(payload);

          self->onMessage(kind, value);
        }
      }

      socket.close();
      self->inUse = false;
    } else {
      socket.write_some(asio::buffer(formatMessage(
          "error",
          choc::value::createObject("", "message",
                                    "Plugin is connected to another client"))));
      socket.close();
    }
  }
}

void OpenUtauPlugin::willAccept() {
  acceptor->async_accept(std::bind(
      &OpenUtauPlugin::onAccept, std::shared_ptr<OpenUtauPlugin>(this),
      std::placeholders::_1, std::placeholders::_2));
}

void OpenUtauPlugin::initializeNetwork() {
  // Constructor might be called during the initial loading of the plugin, so
  // initialize the ioThread here.
  if (!initializedNetwork) {
    this->acceptor = std::make_shared<asio::ip::tcp::acceptor>(
        getIoContext(), asio::ip::tcp::endpoint(
                            asio::ip::address::from_string("127.0.0.1"), 0));
    int port = acceptor->local_endpoint().port();
    this->port = port;

    std::filesystem::path tempPath = std::filesystem::temp_directory_path();
    std::filesystem::path socketPath = tempPath / "OpenUtau" / "PluginServers" /
                                       std::format("{}.json", this->uuid);
    std::string socketContent = choc::json::toString(
        choc::value::createObject("", "port", port, "name", this->uuid));

    std::filesystem::create_directories(socketPath.parent_path());
    std::ofstream socketFile(socketPath);
    socketFile << socketContent;
    socketFile.close();

    this->socketPath = socketPath;

    initializedNetwork = true;
    willAccept();
  }
}

void OpenUtauPlugin::onMessage(const std::string kind,
                               const choc::value::Value payload) {
  if (kind == "status") {
    std::string ustx = payload["ustx"].get<std::string>();
    setState("ustx", ustx.c_str());
    std::string mixesJson = choc::json::toString(payload["mixes"]);
    setState("mixes", mixesJson.c_str());
  }
}

std::string
OpenUtauPlugin::formatMessage(const std::string &kind,
                              const choc::value::ValueView &payload) {
  std::string json = choc::json::toString(payload);
  return std::format("{} {}\n", kind, json);
}

/* ------------------------------------------------------------------------------------------------------------
 * Plugin entry point, called by DPF to create a new plugin instance. */

Plugin *createPlugin() { return new OpenUtauPlugin(); }

// -----------------------------------------------------------------------------------------------------------

END_NAMESPACE_DISTRHO

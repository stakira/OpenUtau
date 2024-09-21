#include "plugin.hpp"
#include "DistrhoPlugin.hpp"
#include "DistrhoPluginInfo.h"
#include "asio.hpp"
#include "choc/containers/choc_Value.h"
#include "choc/memory/choc_Base64.h"
#include "choc/text/choc_JSON.h"
#include "common.hpp"
#include "extra/String.hpp"
#include "gzip/compress.hpp"
#include "uuid/v4/uuid.h"
#include <cstdio>
#include <filesystem>
#include <fstream>
#include <string>
#include <thread>
#include <vector>

// std::jthread *ioThread = nullptr;
std::unique_ptr<std::jthread> ioThread;
std::shared_ptr<asio::io_context> ioContext = nullptr;
std::shared_ptr<asio::io_context> getIoContext() {
  if (!ioContext) {
    ioContext = std::make_shared<asio::io_context>();
    ioThread = std::make_unique<std::jthread>([](std::stop_token st) {
      while (!st.stop_requested()) {
        try {
          ioContext->poll();
        } catch (asio::system_error &e) {
          // ignore
        }
      }
    });

    std::atexit([]() {
      ioContext->stop();
      ioThread->request_stop();
      ioThread->join();
    });
  }
  return ioContext;
}

// note: OpenUtau returns 44100Hz, 2ch, 32bit float audio

START_NAMESPACE_DISTRHO

// -----------------------------------------------------------------------------------------------------------
OpenUtauPlugin::OpenUtauPlugin()
    : Plugin(0, 0, 5)

{

  if (this->isDummyInstance()) {
    return;
  }

  this->mixes = std::vector<std::vector<float>>();

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
  case 3:
    state.key = "trackNames";
    state.label = "Track Names";
    break;
  case 4:
    state.key = "mapping";
    state.label = "Output Mapping";
    break;
  }
}

String OpenUtauPlugin::getState(const char *rawKey) const {
  // DPF cannot handle binary data, so we need to encode it to base64
  std::string key(rawKey);

  if (key == "name") {
    return String(name.c_str());
  } else if (key == "uuid") {
    return String(uuid.c_str());
  } else if (key == "ustx") {
    std::string encoded = choc::base64::encodeToString(ustx);
    return String(encoded.c_str());
  } else if (key == "mixes") {
    choc::value::Value value = choc::value::createEmptyArray();
    for (const auto &mix : mixes) {
      std::string compressed =
          gzip::compress((char *)mix.data(), mix.size() * sizeof(float));
      std::string encoded = choc::base64::encodeToString(compressed);
      value.addArrayElement(encoded);
    }

    return String(choc::json::toString(value).c_str());
  } else if (key == "trackNames") {
    return String(Structures::serializeTrackNames(trackNames).c_str());
  } else if (key == "mapping") {
    return String(Structures::serializeOutputMap(outputMap).c_str());
  }
  return String();
}

void OpenUtauPlugin::setState(const char *rawKey, const char *value) {
  std::string key(rawKey);
  if (key == "name") {
    this->name = value;
    this->updatePluginServerFile();
  } else if (key == "uuid") {
    this->uuid = value;
  } else if (key == "ustx") {
    this->ustx = Utils::unBase64ToString(value);
  } else if (key == "mixes") {
    choc::value::Value jsonValue = choc::json::parse(std::string(value));
    std::vector<std::vector<float>> mixes;
    for (choc::value::ValueView encodedValue : jsonValue) {
      std::string encoded(encodedValue.getString());
      if (encoded.length() == 0) {
        mixes.push_back(std::vector<float>());
        continue;
      }
      std::vector<uint8_t> decoded = Utils::unBase64ToVector(encoded);
      std::vector<uint8_t> decompressed =
          Utils::gunzip((char *)decoded.data(), decoded.size());
      std::vector<float> mix((float *)decompressed.data(),
                             (float *)decompressed.data() +
                                 decompressed.size() / sizeof(float));
      mixes.push_back(mix);
    }

    {
      this->requestWrite();
      this->mixes = mixes;
      this->doneWriting();
    }
    resampleMixes(this->currentSampleRate);
  } else if (key == "trackNames") {
    this->trackNames = Structures::deserializeTrackNames(value);
    syncMapping();
  } else if (key == "mapping") {
    this->outputMap = Structures::deserializeOutputMap(value);
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
uint32_t OpenUtauPlugin::getVersion() const {
  return d_version(Constants::majorVersion, Constants::minorVersion,
                   Constants::patchVersion);
}

/* --------------------------------------------------------------------------------------------------------
 * Init */

/**
       Initialize the audio port @a index.@n
       This function will be called once, shortly after the plugin is created.
 */
void OpenUtauPlugin::initAudioPort(bool input, uint32_t index,
                                   AudioPort &port) {
  port.groupId = index / 2;
  port.hints = kPortGroupStereo;
  auto name = std::format("Channel {}", index / 2 + 1);
  port.name = String(name.c_str());
}

/* --------------------------------------------------------------------------------------------------------
 * Audio/MIDI Processing */

void OpenUtauPlugin::run(const float **inputs, float **outputs, uint32_t frames,
                         const MidiEvent *midiEvents, uint32_t midiEventCount) {
  initializeNetwork();
  auto timePosition = this->getTimePosition();

  for (uint32_t i = 0; i < DISTRHO_PLUGIN_NUM_OUTPUTS; ++i) {
    for (uint32_t j = 0; j < frames; ++j) {
      outputs[i][j] = 0;
    }
  }

  auto sampleRate = getSampleRate();
  if (this->resampledMixes.size() > 0 && timePosition.playing &&
      !this->writing.load()) {
    this->readingCount++;
    if (this->currentSampleRate == sampleRate) {
      for (uint32_t j = 0; j < mixes.size(); ++j) {
        if (j >= this->outputMap.size()) {
          break;
        }
        const auto &mapping = outputMap[j];
        const auto &left = resampledMixes[j].first;
        const auto &right = resampledMixes[j].second;

        for (uint32_t i = 0; i < frames; ++i) {
          auto frame = (i + timePosition.frame);
          for (uint32_t k = 0; k < DISTRHO_PLUGIN_NUM_OUTPUTS; ++k) {
            if (mapping.first[k] && frame < left.size()) {
              outputs[k][i] += left[frame];
            }
            if (mapping.second[k] && frame < right.size()) {
              outputs[k][i] += right[frame];
            }
          }
        }
      }
    } else {
      resampleMixes(sampleRate);
    }
    this->readingCount--;
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

void OpenUtauPlugin::sampleRateChanged(double newSampleRate) {
  resampleMixes(newSampleRate);
}
void OpenUtauPlugin::onAccept(std::shared_ptr<OpenUtauPlugin> self,
                              const asio::error_code &error,
                              asio::ip::tcp::socket socket) {
  if (!error) {
    self->willAccept();
    if (!self->inUse) {
      self->inUse = true;
      socket.write_some(asio::buffer(formatMessage(
          "init", choc::value::createObject("", "ustx", self->ustx))));
      std::string messageBuffer;
      char buffer[16 * 1024];
      while (true) {
        size_t len;
        try {
          len = socket.read_some(asio::buffer(buffer));
        } catch (asio::system_error &e) {
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

      try {
        socket.close();
      } catch (asio::system_error &e) {
        // ignore
      }
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
        getIoContext()->get_executor(),
        asio::ip::tcp::endpoint(asio::ip::address::from_string("127.0.0.1"),
                                0));
    int port = acceptor->local_endpoint().port();
    this->port = port;

    updatePluginServerFile();
    initializedNetwork = true;
    willAccept();
  }
}

void OpenUtauPlugin::updatePluginServerFile() {
  std::filesystem::path tempPath = std::filesystem::temp_directory_path();
  std::filesystem::path socketPath = tempPath / "OpenUtau" / "PluginServers" /
                                     std::format("{}.json", this->uuid);
  std::string socketContent = choc::json::toString(
      choc::value::createObject("", "port", port, "name", this->name));

  std::filesystem::create_directories(socketPath.parent_path());
  std::ofstream socketFile(socketPath);
  socketFile << socketContent;
  socketFile.close();

  this->socketPath = socketPath;
}

void OpenUtauPlugin::onMessage(const std::string kind,
                               const choc::value::Value payload) {
  if (kind == "status") {
    this->lastSync = std::chrono::system_clock::now();

    std::string ustx = payload["ustx"].get<std::string>();
    setState("ustx", ustx.c_str());
    std::string mixesJson = choc::json::toString(payload["mixes"]);
    setState("mixes", mixesJson.c_str());
    std::vector<std::string> trackNames;
    for (auto track : payload["trackNames"]) {
      trackNames.push_back(track.get<std::string>());
    }
    setState("trackNames", Structures::serializeTrackNames(trackNames).c_str());

    syncMapping();
  }
}

void OpenUtauPlugin::syncMapping() {
  this->requestWrite();

  auto trackNames = this->trackNames;
  auto outputMap = this->outputMap;
  if (trackNames.size() < outputMap.size()) {
    outputMap.resize(trackNames.size());
  } else if (trackNames.size() > outputMap.size()) {
    bool customized = false;
    auto defaultLeft = std::vector<bool>(DISTRHO_PLUGIN_NUM_OUTPUTS, false);
    auto defaultRight = std::vector<bool>(DISTRHO_PLUGIN_NUM_OUTPUTS, false);
    defaultLeft[0] = true;
    defaultRight[1] = true;
    for (const auto &mapping : outputMap) {
      if (mapping.first != defaultLeft || mapping.second != defaultRight) {
        customized = true;
        break;
      }
    }
    for (size_t i = outputMap.size(); i < trackNames.size(); ++i) {
      auto leftChannel = std::vector<bool>(DISTRHO_PLUGIN_NUM_OUTPUTS, false);
      auto rightChannel = std::vector<bool>(DISTRHO_PLUGIN_NUM_OUTPUTS, false);
      if (customized) {
        auto index = i % 16;
        auto left = index * 2;
        auto right = left + 1;
        leftChannel[left] = true;
        rightChannel[right] = true;
      } else {
        leftChannel[0] = true;
        rightChannel[1] = true;
      }

      outputMap.push_back({leftChannel, rightChannel});
    }
  }

  this->outputMap = outputMap;
  this->doneWriting();
  setState("mapping", Structures::serializeOutputMap(outputMap).c_str());
}

void OpenUtauPlugin::resampleMixes(double newSampleRate) {
  requestWrite();

  std::vector<std::pair<std::vector<float>, std::vector<float>>> resampledMixes;
  for (const auto &mix : mixes) {
    std::vector<float> resampledLeft;
    std::vector<float> resampledRight;
    resampledLeft.resize(mix.size() * newSampleRate / 44100.0 / 2 + 1);
    resampledRight.resize(mix.size() * newSampleRate / 44100.0 / 2 + 1);
    for (size_t i = 0; i < resampledLeft.size(); ++i) {
      auto leftSource = i * 44100.0 / newSampleRate * 2;
      auto rightSource = leftSource + 1;
      auto leftLeftIndex = (size_t)leftSource;
      auto rightLeftIndex = (size_t)rightSource;
      auto leftRightIndex = leftLeftIndex + 2;
      auto rightRightIndex = rightLeftIndex + 2;
      auto fraction = leftSource - leftLeftIndex;
      if (rightRightIndex < mix.size()) {
        resampledLeft[i] = mix[leftLeftIndex] * (1 - fraction) +
                           mix[leftRightIndex] * fraction;
        resampledRight[i] = mix[rightLeftIndex] * (1 - fraction) +
                            mix[rightRightIndex] * fraction;
      } else if (rightLeftIndex < mix.size()) {
        resampledLeft[i] = mix[leftLeftIndex];
        resampledRight[i] = mix[rightLeftIndex];
      } else {
        resampledLeft[i] = 0;
        resampledRight[i] = 0;
      }
    }

    resampledMixes.push_back({resampledLeft, resampledRight});
  }
  this->resampledMixes = resampledMixes;
  this->currentSampleRate = newSampleRate;

  doneWriting();
}

void OpenUtauPlugin::requestWrite() {
  while (this->writing.exchange(true)) {
    std::this_thread::sleep_for(std::chrono::milliseconds(100));
  }
  while (this->readingCount > 0) {
    std::this_thread::sleep_for(std::chrono::milliseconds(100));
  }
}
void OpenUtauPlugin::doneWriting() { this->writing.store(false); }

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

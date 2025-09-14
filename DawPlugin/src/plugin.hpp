#pragma once
#include "DistrhoPlugin.hpp"
#include "alternate_shared_mutex.hpp"
#include "asio.hpp"
#include "choc/containers/choc_Value.h"
#include "common.hpp"
#include "extra/String.hpp"
#include "yamc_rwlock_sched.hpp"
#include <cstdint>
#include <filesystem>
#include <map>
#include <string>
#include <thread>
#include <unordered_map>
#include <vector>

// note: OpenUtau returns 44100Hz, 2ch, 32bit float audio

using AudioHash = int;
class Part {
public:
  int trackNo;
  double startMs;
  double endMs;

  std::optional<AudioHash> hash;

  static Part deserialize(const choc::value::ValueView &value);
  choc::value::Value serialize() const;
};

// -----------------------------------------------------------------------------------------------------------

class OpenUtauPlugin : public Plugin {
public:
  OpenUtauPlugin();

  ~OpenUtauPlugin() override;

  int port;
  bool connected;
  std::string name;
  std::optional<std::chrono::time_point<std::chrono::system_clock>> lastSync;

  std::vector<Structures::Track> tracks;
  Structures::OutputMap outputMap;

  bool isProcessing();

protected:
  // --------------------------------------------------------------------------------------------------------
  const char *getLabel() const override;

  const char *getDescription() const override;

  const char *getMaker() const override;

  const char *getHomePage() const override;

  void initState(uint32_t index, State &state) override;

  String getState(const char *key) const override;

  void setState(const char *key, const char *value) override;

  const char *getLicense() const override;

  uint32_t getVersion() const override;

  // --------------------------------------------------------------------------------------------------------
  void initAudioPort(bool input, uint32_t index, AudioPort &port) override;
  void initPortGroup(uint32_t groupId, PortGroup &group) override;

  // --------------------------------------------------------------------------------------------------------

  void run(const float **inputs, float **outputs, uint32_t frames,
           const MidiEvent *midiEvents, uint32_t midiEventCount) override;

  // --------------------------------------------------------------------------------------------------------

  void sampleRateChanged(double newSampleRate) override;

  // -------------------------------------------------------------------------------------------------------

private:
  static void onAccept(OpenUtauPlugin *self, const asio::error_code &error,
                       asio::ip::tcp::socket socket);

  void willAccept();

  void initializeNetwork();

  choc::value::Value onRequest(const std::string kind,
                               const choc::value::Value payload);
  void onNotification(const std::string kind, const choc::value::Value payload);

  static std::string formatMessage(const std::string &kind,
                                   const choc::value::ValueView &payload);

  void syncMapping();
  void updatePluginServerFile();
  void requestResampleMixes(double newSampleRate);
  void resampleMixes(double newSampleRate);

  std::string ustx;
  std::string uuid;

  std::chrono::time_point<std::chrono::system_clock> lastPing;

  yamc::alternate::basic_shared_mutex<yamc::rwlock::WriterPrefer>
      audioBuffersMutex;
  std::map<AudioHash, std::vector<float>> audioBuffers;

  yamc::alternate::basic_shared_mutex<yamc::rwlock::WriterPrefer> partsMutex;
  std::map<int, std::vector<Part>> parts;

  yamc::alternate::basic_shared_mutex<yamc::rwlock::WriterPrefer> mixMutex;
  bool isProcessingMix = false;
  std::vector<std::pair<std::vector<float>, std::vector<float>>> mixes;
  double currentSampleRate = 44100.0;

  std::filesystem::path socketPath;

  std::unique_ptr<asio::ip::tcp::acceptor> acceptor;
  std::unique_ptr<std::jthread> acceptorThread;

  std::unordered_map<std::string, std::jthread> threads;

  yamc::alternate::basic_shared_mutex<yamc::rwlock::WriterPrefer> tracksMutex;
  std::mutex partMutex;

  DISTRHO_DECLARE_NON_COPYABLE_WITH_LEAK_DETECTOR(OpenUtauPlugin)
};
// -----------------------------------------------------------------------------------------------------------

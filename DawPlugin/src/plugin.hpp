#pragma once
#include "DistrhoPlugin.hpp"
#include "alternate_shared_mutex.hpp"
#include "asio.hpp"
#include "choc/containers/choc_Value.h"
#include "common.hpp"
#include "extra/String.hpp"
#include "yamc_rwlock_sched.hpp"
#include <filesystem>
#include <map>
#include <string>
#include <unordered_map>
#include <vector>

// note: OpenUtau returns 44100Hz, 2ch, 32bit float audio

using AudioHash = uint32_t;
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

/**
  Plugin to show how to get somebasic information sent to the UI.
 */
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
  /* --------------------------------------------------------------------------------------------------------
   * Information */

  /**
         Get the plugin label.
         This label is a short restricted name consisting of only _, a-z, A-Z
     and 0-9 characters.
   */
  const char *getLabel() const override;

  /**
     Get an extensive comment/description about the plugin.
   */
  const char *getDescription() const override;

  /**
     Get the plugin author/maker.
   */
  const char *getMaker() const override;

  /**
     Get the plugin homepage.
   */
  const char *getHomePage() const override;

  void initState(uint32_t index, State &state) override;

  String getState(const char *key) const override;

  void setState(const char *key, const char *value) override;

  /**
     Get the plugin license name (a single line of text).
     For commercial plugins this should return some short copyright information.
   */
  const char *getLicense() const override;

  /**
     Get the plugin version, in hexadecimal.
   */
  uint32_t getVersion() const override;

  /* --------------------------------------------------------------------------------------------------------
   * Init */

  /**
         Initialize the audio port @a index.@n
         This function will be called once, shortly after the plugin is created.
   */
  void initAudioPort(bool input, uint32_t index, AudioPort &port) override;

  /* --------------------------------------------------------------------------------------------------------
   * Audio/MIDI Processing */

  void run(const float **inputs, float **outputs, uint32_t frames,
           const MidiEvent *midiEvents, uint32_t midiEventCount) override;
  ;

  /* --------------------------------------------------------------------------------------------------------
   * Callbacks (optional) */

  /**
         Optional callback to inform the plugin about a buffer size change.
         This function will only be called when the plugin is deactivated.
         @note This value is only a hint!
                   Hosts might call run() with a higher or lower number of
     frames.
   */
  void bufferSizeChanged(uint32_t newBufferSize) override;

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

  yamc::alternate::basic_shared_mutex<yamc::rwlock::WriterPrefer> audioBuffersMutex;
  std::map<AudioHash, std::vector<float>> audioBuffers;

  yamc::alternate::basic_shared_mutex<yamc::rwlock::WriterPrefer> partsMutex;
  std::map<int, std::vector<Part>> parts;

  yamc::alternate::basic_shared_mutex<yamc::rwlock::WriterPrefer> mixMutex;
  std::vector<std::pair<std::vector<float>, std::vector<float>>> mixes;
  double currentSampleRate = 44100.0;

  std::filesystem::path socketPath;

  std::unique_ptr<asio::ip::tcp::acceptor> acceptor;
  std::unique_ptr<std::jthread> acceptorThread;

  std::unordered_map<std::string, std::jthread> threads;

  yamc::alternate::basic_shared_mutex<yamc::rwlock::WriterPrefer> tracksMutex;
  std::mutex partMutex;

  /**
     Set our plugin class as non-copyable and add a leak detector just in case.
   */
  DISTRHO_DECLARE_NON_COPYABLE_WITH_LEAK_DETECTOR(OpenUtauPlugin)
};
// -----------------------------------------------------------------------------------------------------------

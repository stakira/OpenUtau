#pragma once
#include "DistrhoPlugin.hpp"
#include "asio.hpp"
#include "choc/containers/choc_Value.h"
#include "common.hpp"
#include "extra/String.hpp"
#include <filesystem>
#include <string>
#include <vector>

// note: OpenUtau returns 44100Hz, 2ch, 32bit float audio

START_NAMESPACE_DISTRHO

// -----------------------------------------------------------------------------------------------------------

/**
  Plugin to show how to get some basic information sent to the UI.
 */
class OpenUtauPlugin : public Plugin {
public:
  OpenUtauPlugin();

  ~OpenUtauPlugin() override;

  int port;
  bool connected;
  std::string name;
  std::optional<std::chrono::time_point<std::chrono::system_clock>> lastSync;

  std::vector<std::string> trackNames;
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

  void onMessage(const std::string kind, const choc::value::Value payload);

  static std::string formatMessage(const std::string &kind,
                                   const choc::value::ValueView &payload);

  void syncMapping();
  void updatePluginServerFile();
  void resampleMixes(double newSampleRate);
  void requestWrite();
  void doneWriting();

  std::string ustx;
  std::string uuid;

  std::chrono::time_point<std::chrono::system_clock> lastPing;

  std::atomic<bool> writing = false;
  std::atomic<int> readingCount = 0;

  std::vector<std::vector<float>> mixes;
  std::vector<std::pair<std::vector<float>, std::vector<float>>> resampledMixes;
  double currentSampleRate = 44100.0;

  std::filesystem::path socketPath;

  std::atomic<bool> networkInitialized = false;
  std::unique_ptr<asio::ip::tcp::acceptor> acceptor;
  std::unique_ptr<std::jthread> acceptorThread;

  std::mutex statusMutex;

  /**
     Set our plugin class as non-copyable and add a leak detector just in case.
   */
  DISTRHO_DECLARE_NON_COPYABLE_WITH_LEAK_DETECTOR(OpenUtauPlugin)
};
// -----------------------------------------------------------------------------------------------------------

END_NAMESPACE_DISTRHO

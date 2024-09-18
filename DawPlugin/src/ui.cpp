#include "DistrhoUI.hpp"
#include "plugin.hpp"
#include <string>

START_NAMESPACE_DISTRHO

// --------------------------------------------------------------------------------------------------------------------

class OpenUtauUI : public UI {
public:
  /**
     UI class constructor.
     The UI should be initialized to a default state that matches the plugin
     side.
   */
  OpenUtauUI() : UI(DISTRHO_UI_DEFAULT_WIDTH, DISTRHO_UI_DEFAULT_HEIGHT) {
    const double scaleFactor = getScaleFactor();

    if (d_isEqual(scaleFactor, 1.0)) {
      setGeometryConstraints(DISTRHO_UI_DEFAULT_WIDTH,
                             DISTRHO_UI_DEFAULT_HEIGHT);
    } else {
      const uint width = DISTRHO_UI_DEFAULT_WIDTH * scaleFactor;
      const uint height = DISTRHO_UI_DEFAULT_HEIGHT * scaleFactor;
      setGeometryConstraints(width, height);
      setSize(width, height);
    }
  }

protected:
  // ----------------------------------------------------------------------------------------------------------------
  // DSP/Plugin Callbacks

  /**
     A parameter has changed on the plugin side.@n
     This is called by the host to inform the UI about parameter changes.
   */
  void parameterChanged(uint32_t, float) override {}

  void stateChanged(const char *, const char *) override {}

  // ----------------------------------------------------------------------------------------------------------------
  // Widget Callbacks

  /**
     ImGui specific onDisplay function.
   */
  void onImGuiDisplay() override {
    ImGui::SetNextWindowPos(ImVec2(0, 0));
    ImGui::SetNextWindowSize(ImVec2(getWidth(), getHeight()));

    auto plugin = getPlugin();

    ImGui::Text("OpenUtau Bridge: %s", "v1.0.0");
    ImGui::Text("Open OpenUtau, and select this:");
    ImGui::Text("  %s (%d)", plugin->name.c_str(), plugin->port);
  }

  OpenUtauPlugin *getPlugin() {
    return static_cast<OpenUtauPlugin *>(getPluginInstancePointer());
  }

  // ----------------------------------------------------------------------------------------------------------------

  DISTRHO_DECLARE_NON_COPYABLE_WITH_LEAK_DETECTOR(OpenUtauUI)
};

// --------------------------------------------------------------------------------------------------------------------

UI *createUI() { return new OpenUtauUI(); }

// --------------------------------------------------------------------------------------------------------------------

END_NAMESPACE_DISTRHO

//
// Created by SeleDreams on 15/10/2021.
//

#ifndef OPENUTAU_PLUGIN_OPENUTAUPLUGIN_H
#define OPENUTAU_PLUGIN_OPENUTAUPLUGIN_H
#include <DistrhoPlugin.hpp>
#include <string>
#include <process.hpp>
START_NAMESPACE_DISTRHO
class OpenUTAUPlugin : public Plugin {
public:
    OpenUTAUPlugin();
    ~OpenUTAUPlugin() override;
protected:
    void initAudioPort(bool input, uint32_t index, AudioPort& port) override;
    void run(const float **, float **outputs, uint32_t frames, const MidiEvent *midiEvents,
             uint32_t midiEventCount) override;
    const char *getLabel() const override {
        return "OpenUTAU";
    }

    const char *getMaker() const override {
        return "OpenUTAU by Stakira, Plugin by SeleDreams";
    }

    const char *getLicense() const override {
        return "MIT";
    }

    const char *getHomePage() const override{
        return "https://github.com/stakira/OpenUtau";
    }

    uint32_t getVersion() const override{
        return d_version(0,0,1);
    }

    int64_t getUniqueId() const override
    {
        return d_cconst('O','t','a','u');
    }
    static std::string env(const char *name)
    {
        const char *ret = getenv(name);
        if (!ret) return std::string();
        return std::string(ret);
    }
    static TinyProcessLib::Process *uiProcess;
};
Plugin* createPlugin()
{
    return new OpenUTAUPlugin;
}
END_NAMESPACE_DISTRHO
#endif //OPENUTAU_PLUGIN_OPENUTAUPLUGIN_H

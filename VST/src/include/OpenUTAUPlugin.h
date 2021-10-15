//
// Created by SeleDreams on 15/10/2021.
//

#ifndef OPENUTAU_PLUGIN_OPENUTAUPLUGIN_H
#define OPENUTAU_PLUGIN_OPENUTAUPLUGIN_H
#include <DistrhoPlugin.hpp>
START_NAMESPACE_DISTRHO
class OpenUTAUPlugin : public Plugin {
public:
    OpenUTAUPlugin() : Plugin(0,0,0)
    {
    }
protected:
    void run(const float** inputs, float** outputs, uint32_t frames) override;

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
        return "https://github.com/SeleDreams/OpenUTAUPlugin";
    }

    uint32_t getVersion() const override{
        return d_version(0,0,1);
    }

    int64_t getUniqueId() const override
    {
        return d_cconst('O','t','a','u');
    }
};

Plugin* createPlugin()
{
    return new OpenUTAUPlugin;
}
END_NAMESPACE_DISTRHO
#endif //OPENUTAU_PLUGIN_OPENUTAUPLUGIN_H

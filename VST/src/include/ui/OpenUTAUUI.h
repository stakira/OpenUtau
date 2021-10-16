//
// Created by SeleDreams on 15/10/2021.
//

#ifndef OPENUTAU_PLUGIN_OPENUTAUUI_H
#define OPENUTAU_PLUGIN_OPENUTAUUI_H

#include <DistrhoUI.hpp>

namespace OpenUTAU{
    class OpenUTAUPlugin;
    class OpenUTAUUI : public UI
    {
    public:
        OpenUTAUUI();
        static float getParameterValue(uint32_t index) ;
        void onDisplay() override;
    protected:
        void parameterChanged(uint32_t,float value) override;
        void uiIdle() override;
    };
}

START_NAMESPACE_DISTRHO
UI *createUI()
{
    return new OpenUTAU::OpenUTAUUI;
}
END_NAMESPACE_DISTRHO
#endif //OPENUTAU_PLUGIN_OPENUTAUUI_H

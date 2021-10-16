#include "OpenUTAUPlugin.h"
#include <DistrhoPlugin.hpp>
#include <memory>
#include <iostream>

using namespace OpenUTAU;
TinyProcessLib::Process *OpenUTAUPlugin::uiProcess = nullptr;

void OpenUTAUPlugin::run(const float **, float **outputs, uint32_t frames, const MidiEvent *midiEvents,
                         uint32_t midiEventCount) {
    auto position = getTimePosition();
    if(position.playing)
    {
        std::cout << position.frame << std::endl;
    }

    if (uiProcess) return;
    std::string openUtauPath = R"(C:\Users\SeleDreams\Dropbox\Documents\Development\OpenUtau\OpenUtau\bin\Debug\netcoreapp3.1)";
    std::string path = openUtauPath + "/OpenUtau.exe -vst";
    uiProcess = new TinyProcessLib::Process(path,openUtauPath,[](const char *bytes,size_t n){
        std::cout << "OpenUtau closed !" << std::endl;
    });

}

void OpenUTAUPlugin::initAudioPort(bool input, uint32_t index, AudioPort &port) {
    Plugin::initAudioPort(input, index, port);
}

OpenUTAUPlugin::OpenUTAUPlugin() : Plugin(0,0,0){
}

OpenUTAUPlugin::~OpenUTAUPlugin() {
    if (uiProcess)
    {
        uiProcess->kill(true);
        uiProcess = nullptr;
    }
}

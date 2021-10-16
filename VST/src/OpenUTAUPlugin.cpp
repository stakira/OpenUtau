#include "OpenUTAUPlugin.h"
#include <DistrhoPlugin.hpp>
#include <memory>
#include <iostream>


using namespace OpenUTAU;

void OpenUTAUPlugin::run(const float **, float **outputs, uint32_t frames, const MidiEvent *midiEvents,
                         uint32_t midiEventCount) {
    auto position = getTimePosition();
    if (position.bbt.valid && position.playing)
    {
        auto &bbt = position.bbt;
        double currentTicks = (bbt.barStartTick + (bbt.beat * bbt.ticksPerBeat) + bbt.tick) - (bbt.ticksPerBeat);
        server->setPlaying(true);
        server->setTicks(currentTicks);
        server->setTicksPerBeat(bbt.ticksPerBeat);
        std::cout << currentTicks << std::endl;
    }
    else{
        server->setPlaying(false);
    }
    if (server->isRunning() && server->hasClient())
    {
        server->Update();
    }

    if (!uiProcess && !server->isRunning())
    {
        server->Run();
        uiProcess = std::make_unique<TinyProcessLib::Process>(openUtauPath + "/OpenUtau.exe -vst -port 1556",openUtauPath,[](const char *bytes, size_t n){
            std::cout << "OpenUtau closed !" << std::endl;
        });
        std::cout << "OpenUTAU created! " << openUtauPath << std::endl;
    }
}

OpenUTAUPlugin::OpenUTAUPlugin() : Plugin(0,0,0), server(std::make_shared<OpenUTAUServer>(IPV::V4,1556)), uiProcess(nullptr) {
}

OpenUTAUPlugin::~OpenUTAUPlugin() {
    int exit_status;
    if (uiProcess && !uiProcess->try_get_exit_status(exit_status))
    {
        uiProcess->kill(true);
    }
}

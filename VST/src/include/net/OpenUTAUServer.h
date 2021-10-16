//
// Created by SeleDreams on 15/10/2021.
//

#ifndef OPENUTAU_PLUGIN_OPENUTAUSERVER_H
#define OPENUTAU_PLUGIN_OPENUTAUSERVER_H
#include <asio.hpp>
#include "net/TCPConnection.h"
#include "TCPConnection.h"

namespace OpenUTAU
{
    enum class IPV{
        V4,
        V6
    };

class OpenUTAUServer : public std::enable_shared_from_this<OpenUTAUServer>
    {
    public:
        explicit OpenUTAUServer(IPV ipv,int setPort = 5193);
        int Run();
        bool isRunning() const {return running;}
        bool hasClient() const { return !connections.empty() && connections[0]->Started();}
        void setTicks(double newTicks) { ticks = newTicks;}
        double getTicks() const { return ticks;}
        void setTicksPerBeat(double newTicksPerBeat) {ticksPerBeat = newTicksPerBeat;}
        double getTicksPerBeat() const { return ticksPerBeat;}
        void setPlaying(bool newPlaying){playing = newPlaying;}
        bool isPlaying() const {return playing;}
        void Update();
    private:
        void StartAccept();
    private:
        bool running{false};
        int port;
        IPV ipVersion;
        asio::io_context ioContext;
        std::unique_ptr<asio::ip::tcp::acceptor> acceptor;
        std::unique_ptr<std::thread> serverThread;
        std::vector<std::shared_ptr<TCPConnection>> connections{};
        std::atomic<double> ticks;
        std::atomic<double> ticksPerBeat;
        std::atomic_bool playing;
    };
}
#endif //OPENUTAU_PLUGIN_OPENUTAUSERVER_H

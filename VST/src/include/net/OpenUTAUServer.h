//
// Created by SeleDreams on 15/10/2021.
//

#ifndef OPENUTAU_PLUGIN_OPENUTAUSERVER_H
#define OPENUTAU_PLUGIN_OPENUTAUSERVER_H
#include <asio.hpp>
#include "net/MessageHeader.h"
#include "net/MessageTypesEnum.h"
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
    private:
        void StartAccept();
    private:
        bool running{false};
        int port;
        IPV ipVersion;
        asio::io_context ioContext;
        std::unique_ptr<asio::ip::tcp::acceptor> acceptor;
        std::unique_ptr<std::thread> serverThread;
    };
}
#endif //OPENUTAU_PLUGIN_OPENUTAUSERVER_H

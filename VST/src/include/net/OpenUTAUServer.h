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

    class OpenUTAUServer
    {
    public:
        explicit OpenUTAUServer(IPV ipv,int setPort = 5193);
        bool Start();
        void Stop();
        int Run();
    private:
        int port;
        IPV ipVersion;
        asio::io_context ioContext;
        asio::ip::tcp::acceptor acceptor;
    };
}
#endif //OPENUTAU_PLUGIN_OPENUTAUSERVER_H

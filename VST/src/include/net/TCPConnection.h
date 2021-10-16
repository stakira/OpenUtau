//
// Created by SeleDreams on 16/10/2021.
//

#ifndef OPENUTAU_PLUGIN_TCPCONNECTION_H
#define OPENUTAU_PLUGIN_TCPCONNECTION_H
#include <asio.hpp>
#include "VSTData.h"

namespace OpenUTAU {
    class OpenUTAUServer;
class TCPConnection : public std::enable_shared_from_this<TCPConnection>{
    public:
        static std::shared_ptr<TCPConnection> Create(asio::io_context &ioContext,std::shared_ptr<OpenUTAUServer> serverPtr){
            return std::shared_ptr<TCPConnection>(new TCPConnection(ioContext,serverPtr));
        }
        asio::ip::tcp::socket &getSocket() {
            return socket;
        }
        bool Started() const {return started;}
        void Start();
        void Update();
    private:
        explicit TCPConnection(asio::io_context &ioContext,std::shared_ptr<OpenUTAUServer> &serverPtr);

    private:
        std::shared_ptr<OpenUTAUServer> server;
        bool started{false};
        asio::ip::tcp::socket socket;
        std::string message {"hello world\n"};
        VSTData data{};
    };
}
#endif //OPENUTAU_PLUGIN_TCPCONNECTION_H

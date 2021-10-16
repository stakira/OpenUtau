//
// Created by SeleDreams on 16/10/2021.
//

#ifndef OPENUTAU_PLUGIN_TCPCONNECTION_H
#define OPENUTAU_PLUGIN_TCPCONNECTION_H
#include <asio.hpp>
namespace OpenUTAU {
class TCPConnection : public std::enable_shared_from_this<TCPConnection>{
    public:
        static std::shared_ptr<TCPConnection> Create(asio::io_context &ioContext){
            return std::shared_ptr<TCPConnection>(new TCPConnection(ioContext));
        }
        asio::ip::tcp::socket &getSocket() {
            return socket;
        }

        void Start();
    private:
        explicit TCPConnection(asio::io_context &ioContext);

    private:
        asio::ip::tcp::socket socket;
        std::string message {"hello world\n"};
    };
}
#endif //OPENUTAU_PLUGIN_TCPCONNECTION_H

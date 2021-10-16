//
// Created by SeleDreams on 15/10/2021.
//
#include <iostream>
#include <net/TCPConnection.h>
#include "net/OpenUTAUServer.h"

using namespace OpenUTAU;
using asio::ip::tcp;


OpenUTAUServer::OpenUTAUServer(IPV ipv, int setPort) :
        port{setPort},
        ipVersion{ipv},
        acceptor(nullptr) {
}

int OpenUTAUServer::Run() {
    try {
        StartAccept();
        ioContext.run();
    }
    catch (std::exception &ex) {
        std::cerr << ex.what() << std::endl;
        return -1;
    }
    return 0;
}

void OpenUTAUServer::StartAccept() {
    running = true;
    acceptor = std::make_unique<asio::ip::tcp::acceptor>(ioContext, tcp::endpoint(ipVersion == IPV::V4 ? tcp::v4() : tcp::v6(), port));
    // Create connection
    auto connection = TCPConnection::Create(ioContext);
    std::cout << "Starting server on port " << port << std::endl;
    // Asynchronously accept connection
    acceptor->async_accept(connection->getSocket(), [connection, this](const asio::error_code &error) {
        std::cout << "Connection started " << std::endl;
        if (!error) {
            connection->Start();
        }
        //StartAccept();
    });
}



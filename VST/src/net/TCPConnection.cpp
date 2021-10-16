//
// Created by SeleDreams on 16/10/2021.
//
#include <iostream>
#include "net/TCPConnection.h"
#include "net/OpenUTAUServer.h"
#include "net/VSTData.h"

OpenUTAU::TCPConnection::TCPConnection(asio::io_context &ioContext, std::shared_ptr<OpenUTAUServer> &serverPtr)
        : server(serverPtr), socket(ioContext) {

}
void OpenUTAU::TCPConnection::Start() {
    std::cout << "TCP client connected !" << std::endl;
    started = true;
    /*asio::streambuf buffer;

    socket.async_receive(buffer.prepare(512),
    [this](const asio::error_code &error, size_t
    bytesTransferred) {
        if (error == asio::error::eof) {
            std::cout << "client disconnected properly" << std::endl;
        } else {
            std::cout << "client disconnected by force" << std::endl;
        }
    });*/
}

void OpenUTAU::TCPConnection::Update() {
    auto strongThis = weak_from_this().lock();
    if (!strongThis) {
        std::cerr << "An error occurred creating the strong this reference" << std::endl;
        return;
    }
    data.playing = server->isPlaying();
    data.ticks = server->getTicks();
    data.ticksPerBeat = server->getTicksPerBeat();
    asio::async_write(socket, asio::buffer(&data, sizeof(VSTData)),
                      [strongThis](const asio::error_code &error, size_t bytesTransferred) {
                          if (error) {
                              std::cout << "Failled to send message !" << std::endl;
                          } else {
                              std::cout << "Sent message !" << std::endl;
                          }
                      });
}


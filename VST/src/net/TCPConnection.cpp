//
// Created by SeleDreams on 16/10/2021.
//
#include <iostream>
#include "net/TCPConnection.h"

OpenUTAU::TCPConnection::TCPConnection(asio::io_context &ioContext) : socket(ioContext) {

}

void OpenUTAU::TCPConnection::Start() {
    std::cout << "TCP client connected !" << std::endl;
    auto strongThis = weak_from_this().lock();
    if (!strongThis) {
        std::cerr << "An error occurred creating the strong this reference" << std::endl;
        return;
    }
    asio::async_write(socket, asio::buffer(message),
                      [strongThis](const asio::error_code &error, size_t bytesTransferred) {
                          if (error) {
                              std::cout << "Failled to send message !" << std::endl;
                          } else {
                              std::cout << "Sent message !" << std::endl;
                          }
                      });
}


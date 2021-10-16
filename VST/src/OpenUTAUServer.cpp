//
// Created by SeleDreams on 15/10/2021.
//
#include <iostream>
#include "net/OpenUTAUServer.h"
using namespace OpenUTAU;
using asio::ip::tcp;

OpenUTAUServer::OpenUTAUServer(IPV ipv, int setPort) :
        port{setPort},
        ipVersion{ipv},
        acceptor(ioContext,tcp::endpoint (ipVersion == IPV::V4 ? tcp::v4() : tcp::v6(),port))
{

}

bool OpenUTAUServer::Start() {
    return false;
}

void OpenUTAUServer::Stop() {

}

int OpenUTAUServer::Run() {
    try{
        ioContext.run();
    }
    catch(std::exception &ex)
    {
        std::cerr << ex.what() << std::endl;
        return -1;
    }
    return 0;
}



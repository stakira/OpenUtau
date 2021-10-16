//
// Created by SeleDreams on 15/10/2021.
//

#ifndef OPENUTAU_PLUGIN_MESSAGEHEADER_H
#define OPENUTAU_PLUGIN_MESSAGEHEADER_H
#include "MessageTypesEnum.h"
namespace OpenUTAU
{
    struct MessageHeader
    {
        MessageTypes id;
        uint32_t size = 0;
    };
}
#endif //OPENUTAU_PLUGIN_MESSAGE_H

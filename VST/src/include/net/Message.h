//
// Created by SeleDreams on 15/10/2021.
//

#ifndef OPENUTAU_PLUGIN_MESSAGE_H
#define OPENUTAU_PLUGIN_MESSAGE_H
#include "MessageHeader.h"
#include <vector>
namespace OpenUTAU{
    struct Message
    {
        MessageHeader header;
        std::vector<uint8_t> body;

        size_t size() const {
            return sizeof(MessageHeader) + body.size();
        }

        friend std::ostream& operator << (std::ostream& os, const Message &msg)
        {
            os << "ID : " << msg.header.id << " Size: " << msg.header.size;
            return os;
        }
        template<typename DataType>
        friend Message &operator << (Message &msg,const DataType &data)
        {
            static_assert(std::is_standard_layout<DataType>::value,"Data is too complex");
            size_t i = msg.body.size();
            msg.body.resize(msg.body.size() + sizeof(DataType));
            std::memcpy(msg.body.data() + i, &data, sizeof(DataType));
            msg.header.size = msg.size();
            return msg;
        }

        template<typename DataType>
        friend Message &operator >> (Message &msg,const DataType &data)
        {
            static_assert(std::is_standard_layout<DataType>::value,"Data is too complex");
            size_t i = msg.body.size() - sizeof(DataType);
            std::memcpy(&data,msg.body.data() + i, sizeof(DataType));
            msg.body.resize(i);
            msg.header.size = msg.size();
            return msg;
        }
    };
}
#endif //OPENUTAU_PLUGIN_MESSAGE_H

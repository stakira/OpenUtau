//
// Created by SeleDreams on 15/10/2021.
//

#ifndef OPENUTAU_PLUGIN_THREADSAFEQUEUE_H
#define OPENUTAU_PLUGIN_THREADSAFEQUEUE_H
namespace OpenUTAU{
    template<typename T>
    class tsqueue{
    public:
    protected:
        std::mutex muxQueue;
        std::deque<T> deqQueue;
    };
}
#endif //OPENUTAU_PLUGIN_THREADSAFEQUEUE_H

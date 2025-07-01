#ifndef WORLDLINE_COMMON_TIMER_H_
#define WORLDLINE_COMMON_TIMER_H_

#include <chrono>
#include <string>
#include <utility>
#include <vector>

namespace worldline {

class Timer {
 public:
  Timer(std::string name);
  void AddPoint(std::string name);
  void Print();

 private:
  std::string name_;
  std::vector<std::pair<std::string, std::chrono::steady_clock::time_point>>
      time_points_;
};

}  // namespace worldline

#endif  // WORLDLINE_COMMON_TIMER_H_

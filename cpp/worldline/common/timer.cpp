#include "timer.h"

#include <chrono>
#include <iostream>
#include <string>
#include <utility>
#include <vector>

namespace worldline {

Timer::Timer(std::string name) : name_(name) { AddPoint("start"); }

void Timer::AddPoint(std::string name) {
  time_points_.push_back(
      std::make_pair(name, std::chrono::steady_clock::now()));
}

void Timer::Print() {
  auto start = time_points_.front().second;
  auto end = time_points_.back().second;
  auto total =
      std::chrono::duration_cast<std::chrono::microseconds>(end - start);
  for (int i = 1; i < time_points_.size(); ++i) {
    auto t0 = time_points_[i - 1].second;
    auto t1 = time_points_[i].second;
    auto dur = std::chrono::duration_cast<std::chrono::microseconds>(t1 - t0);
    std::cout << name_ << " " << dur.count() * 0.001 << "ms "
              << (100.0 * dur.count() / total.count()) << "% "
              << time_points_[i].first << std::endl;
  }
  std::cout << name_ << " " << total.count() * 0.001 << "ms in total" << std::endl;
}

}  // namespace worldline

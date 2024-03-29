#include "vec_utils.h"

#include <algorithm>
#include <iostream>
#include <iterator>
#include <string>
#include <vector>

#include "npy.hpp"

namespace worldline {

std::vector<std::vector<double>> vec2d(int width, int length, double value) {
  return std::vector<std::vector<double>>(length,
                                          std::vector<double>(width, value));
}

std::vector<double*> vec2d_wrapper(std::vector<std::vector<double>>& vec) {
  std::vector<double*> result;
  result.reserve(vec.size());
  std::transform(vec.begin(), vec.end(), std::back_inserter(result),
                 [](std::vector<double>& v) { return v.data(); });
  return result;
}

std::vector<double> vec2d_to_1d(const std::vector<std::vector<double>>& vec) {
  std::vector<double> result;
  result.reserve(vec.size() * vec[0].size());
  for (const std::vector<double>& v : vec) {
    std::copy(v.begin(), v.end(), std::back_inserter(result));
  }
  return result;
}

std::vector<double> vec_lerp(const std::vector<double>& vec0,
                             const std::vector<double>& vec1, double t) {
  std::vector<double> result(vec0.size());
  for (int i = 0; i < vec0.size(); ++i) {
    result[i] = vec0[i] * (1.0 - t) + vec1[i] * t;
  }
  return result;
}

void vec_print(const std::vector<double>& vec) {
  std::cout << "[";
  for (double v : vec) {
    std::cout << v << ", ";
  }
  std::cout << "]" << std::endl;
}

double vec_maxabs(const std::vector<double>& vec) {
  auto result = std::minmax_element(vec.begin(), vec.end());
  return std::max(std::abs(*result.first), std::abs(*result.second));
}

void save_vec(const std::string& filename, const std::vector<double>& vec) {
  unsigned long shape[1];
  shape[0] = vec.size();
  npy::SaveArrayAsNumpy<double>(filename, false, 1, shape, vec);
}

void save_vec2d(const std::string& filename,
                const std::vector<std::vector<double>>& vec) {
  std::vector<double> temp = vec2d_to_1d(vec);
  unsigned long shape[2];
  shape[0] = vec.size();
  shape[1] = vec[0].size();
  npy::SaveArrayAsNumpy<double>(filename, false, 2, shape, temp);
}

}  // namespace worldline

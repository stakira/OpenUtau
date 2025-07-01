#ifndef WORLDLINE_MODEL_MODEL_UTILS_H_
#define WORLDLINE_MODEL_MODEL_UTILS_H_

#include <string>
#include <vector>

namespace worldline {

std::vector<std::vector<double>> vec2d(int width, int length, double value);

std::vector<double*> vec2d_wrapper(std::vector<std::vector<double>>& vec);

std::vector<double> vec2d_to_1d(const std::vector<std::vector<double>>& vec);

std::vector<double> vec_lerp(const std::vector<double>& vec0,
                             const std::vector<double>& vec1, double t);

void vec_print(const std::vector<double>& vec);

double vec_maxabs(const std::vector<double>& vec);

void save_vec(const std::string& filename, const std::vector<double>& vec);

void save_vec2d(const std::string& filename,
                const std::vector<std::vector<double>>& vec);

}  // namespace worldline

#endif  // WORLDLINE_MODEL_MODEL_UTILS_H_

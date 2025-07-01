#include "pyin_estimator.h"

#include <cmath>
#include <vector>

extern "C" {
#include "pyin.h"
}

namespace worldline {

void PyinEstimator::Estimate(const std::vector<double>& samples, int fs,
                             double frame_ms, std::vector<double>* f0,
                             std::vector<double>* time_axis) {
  int nhop = static_cast<int>(std::round(fs * frame_ms / 1000.0));
  int f0_len = 0;
  pyin_config config = pyin_init(nhop);
  double* raw_f0 = pyin_analyze(config, const_cast<double*>(samples.data()),
                                samples.size(), fs, &f0_len);
  // shift left by 1
  *f0 = std::vector<double>(f0_len - 1);
  *time_axis = std::vector<double>(f0_len - 1);
  std::copy(raw_f0 + 1, raw_f0 + f0_len, f0->data());
  delete[] raw_f0;
  for (int i = 0; i < time_axis->size(); ++i) {
    time_axis->data()[i] = 1.0 * nhop * i / fs;
  }
}

}  // namespace worldline

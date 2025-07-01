#include "dio_ss_estimator.h"

#include <cmath>
#include <vector>

#include "world/constantnumbers.h"
#include "world/dio.h"
#include "world/stonemask.h"
#include "worldline/f0/dio_estimator.h"

namespace worldline {

static double avg_f0(double* data, int count) {
  int non_zeros = 0;
  double sum = 0;
  for (int i = 0; i < count; ++i) {
    if (data[i] > world::kFloorF0StoneMask) {
      non_zeros++;
      sum += data[i];
    }
  }
  if (non_zeros == 0) {
    return 0;
  }
  return sum / non_zeros;
}

void DioSsEstimator::Estimate(const std::vector<double>& samples, int fs,
                              double frame_ms, std::vector<double>* f0,
                              std::vector<double>* time_axis) {
  const int supersampling = 5;
  DioEstimator::Estimate(samples, fs, frame_ms / supersampling, f0, time_axis);
  int f0_len = GetSamplesForDIO(fs, samples.size(), frame_ms);
  for (int i = 0; i < f0_len; ++i) {
    int index = std::min(i * supersampling, (int)f0->size() - 1);
    int count = std::min(supersampling, (int)f0->size() - index);
    f0->data()[i] = avg_f0(f0->data() + index, count);
    time_axis->data()[i] = time_axis->data()[index];
  }
  f0->resize(f0_len);
  time_axis->resize(f0_len);
}

}  // namespace worldline

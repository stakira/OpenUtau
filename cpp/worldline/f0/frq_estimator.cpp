#include "frq_estimator.h"

#include <cmath>
#include <string>
#include <vector>

#include "world/constantnumbers.h"
#include "world/dio.h"
#include "worldline/classic/frq.h"

extern "C" {
#include "pyin.h"
}

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

FrqEstimator::FrqEstimator(const std::string_view frq_data) {
  frq_data_ = LoadFrq(frq_data);
}

void FrqEstimator::Estimate(const std::vector<double>& samples, int fs,
                            double frame_ms, std::vector<double>* f0,
                            std::vector<double>* time_axis) {
  int f0_len = GetSamplesForDIO(fs, samples.size(), frame_ms);
  *f0 = std::vector<double>(f0_len);
  *time_axis = std::vector<double>(f0_len);
  double hop_size = fs * frame_ms / 1000.0;
  double ratio = hop_size / frq_data_.hop_size;
  for (int i = 0; i < f0_len; ++i) {
    int low = std::max(0, (int)std::floor(ratio * i));
    int high =
        std::min((size_t)std::ceil(ratio * (1 + i)), frq_data_.f0.size());
    // when high <= low, avg_f0 simply returns 0.
    (*f0)[i] = avg_f0(frq_data_.f0.data() + low, high - low);
    (*time_axis)[i] = i * frame_ms / 1000.0;
  }
}

}  // namespace worldline

#include "dio_estimator.h"

#include <vector>

#include "world/dio.h"
#include "world/stonemask.h"

namespace worldline {

void DioEstimator::Estimate(const std::vector<double>& samples, int fs,
                            double frame_ms, std::vector<double>* f0,
                            std::vector<double>* time_axis) {
  int f0_len = GetSamplesForDIO(fs, samples.size(), frame_ms);
  *f0 = std::vector<double>(f0_len);
  *time_axis = std::vector<double>(f0_len);
  std::vector<double> raw_f0(f0_len);

  DioOption dio_option;
  InitializeDioOption(&dio_option);
  dio_option.frame_period = frame_ms;
  Dio(samples.data(), samples.size(), fs, &dio_option, time_axis->data(),
      raw_f0.data());
  StoneMask(samples.data(), samples.size(), fs, time_axis->data(),
            raw_f0.data(), f0_len, f0->data());
}

}  // namespace worldline

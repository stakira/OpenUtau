#ifndef WORLDLINE_F0_FRQ_ESTIMATOR_H_
#define WORLDLINE_F0_FRQ_ESTIMATOR_H_

#include <string>
#include <vector>

#include "f0_estimator.h"
#include "worldline/classic/frq.h"

namespace worldline {

class FrqEstimator : public F0Estimator {
 public:
  FrqEstimator(const std::string_view frq_data);

  void Estimate(const std::vector<double>& samples, int fs, double frame_ms,
                std::vector<double>* f0,
                std::vector<double>* time_axis) override;

 private:
  FrqData frq_data_;
};

}  // namespace worldline

#endif  // WORLDLINE_F0_FRQ_ESTIMATOR_H_

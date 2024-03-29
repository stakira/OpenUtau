#ifndef WORLDLINE_CLASSIC_RESAMPLER_H_
#define WORLDLINE_CLASSIC_RESAMPLER_H_

#include <memory>
#include <string>
#include <vector>

#include "worldline/model/model.h"
#include "worldline/synth_request.h"

namespace worldline {

class Resampler {
 public:
  Resampler(SynthRequest request);
  Resampler(std::vector<std::string> args);

  std::vector<double> Resample();

 private:
  void ApplyEffects(std::vector<std::vector<double>>* tension,
                    std::vector<double>* breathiness,
                    std::vector<double>* voicing);
  void ApplyPitch();

  SynthRequest request_;
  std::unique_ptr<Model> model_;
};

}  // namespace worldline

#endif  // WORLDLINE_CLASSIC_RESAMPLER_H_

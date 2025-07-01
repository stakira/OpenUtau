#ifndef WORLDLINE_PHRASE_SYNTH_H_
#define WORLDLINE_PHRASE_SYNTH_H_

#include <memory>
#include <string>
#include <vector>

#include "worldline/model/model.h"
#include "worldline/synth_request.h"

namespace worldline {

typedef void(/*__stdcall*/ *LogCallback)(const char* log);

class PhraseSynth {
 public:
  void AddRequest(const SynthRequest& request, double pos_ms, double skip_ms,
                  double length_ms, double fade_in_ms, double fade_out_ms,
                  LogCallback logCallback);
  void SetCurves(double* const f0, double* gender, double* tension,
                 double* breathiness, double* voicing, int length,
                 LogCallback logCallback);
  std::vector<double> Synth(LogCallback logCallback);

 private:
  struct ModelTiming {
    int left_extra;
    int skip;
    int p0;
    int p1;
    int p3;
    int p4;
  };

  std::vector<Model> models_;
  std::vector<ModelTiming> timings_;

  std::vector<double> f0_;
  std::vector<double> gender_;
  std::vector<double> tension_;
  std::vector<double> breathiness_;
  std::vector<double> voicing_;
};

}  // namespace worldline

#endif  // WORLDLINE_PHRASE_SYNTH_H_

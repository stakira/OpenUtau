#include "phrase_synth.h"

#include <algorithm>
#include <cmath>
#include <iterator>
#include <vector>

#include "world/constantnumbers.h"
#include "worldline/classic/timing.h"
#include "worldline/common/vec_utils.h"
#include "worldline/f0/f0_estimator.h"
#include "worldline/f0/frq_estimator.h"
#include "worldline/f0/pyin_estimator.h"
#include "worldline/model/effects.h"

namespace worldline {

constexpr double frame_ms = 10;
const int padding = 2;

static int ceil_int(double v) { return static_cast<int>(ceil(v)); }
static int floor_int(double v) { return static_cast<int>(floor(v)); }
static int round_int(double v) { return static_cast<int>(round(v)); }

void PhraseSynth::AddRequest(const SynthRequest& request, double pos_ms,
                             double skip_ms, double length_ms,
                             double fade_in_ms, double fade_out_ms,
                             LogCallback logCallback) {
  std::vector<double> samples;
  samples.reserve(request.sample_length);
  std::copy(request.sample, request.sample + request.sample_length,
            std::back_inserter(samples));

  std::unique_ptr<F0Estimator> f0_estimator = nullptr;
  if (request.frq_length > 0) {
    std::string_view frq_data(request.frq, request.frq_length);
    f0_estimator = std::make_unique<FrqEstimator>(frq_data);
  } else {
    f0_estimator = std::make_unique<PyinEstimator>();
  }

  Model model(std::move(samples), request.sample_fs, frame_ms,
              std::move(f0_estimator));

  double src_max = vec_maxabs(model.samples());

  model.BuildF0();

  auto mapping = GetTimeMapping(model, request);

  // Trim model to input region.
  double in_start_ms = request.offset;
  double in_length_ms = GetInTotalMs(model, request);
  int in_start_frame = static_cast<int>(in_start_ms / frame_ms);
  int in_length_frame =
      static_cast<int>(std::ceil(in_start_ms + in_length_ms) / frame_ms) -
      in_start_frame;
  double left_trimmed = in_start_frame * frame_ms;

  model.Trim(in_start_frame, in_length_frame);

  double seg_max = vec_maxabs(model.samples());
  AutoGain(model.samples(), src_max, seg_max, model.GetVoicedRatio(),
           request.volume, request.flag_P);

  model.BuildSp();
  model.BuildAp();

  ShiftTimeMapping(mapping, -left_trimmed);
  PadTimeMapping(mapping, padding);

  model.Remap(mapping);

  models_.push_back(std::move(model));
  ModelTiming timing;
  timing.left_extra = padding;
  timing.skip = (int)round(skip_ms / frame_ms);
  timing.p0 = (int)round(pos_ms / frame_ms);
  timing.p1 = (int)round((pos_ms + fade_in_ms) / frame_ms);
  timing.p3 = (int)round((pos_ms + length_ms - fade_out_ms) / frame_ms);
  timing.p4 = (int)round((pos_ms + length_ms) / frame_ms);
  timing.p0 = std::max(0, timing.p0);
  timing.p1 = std::max(timing.p0 + 1, timing.p1);
  timing.p3 = std::min(timing.p4 - 1, timing.p3);
  timings_.push_back(std::move(timing));
}

void PhraseSynth::SetCurves(double* const f0, double* gender, double* tension,
                            double* breathiness, double* voicing, int length,
                            LogCallback logCallback) {
  std::copy(f0, f0 + length, std::back_inserter(f0_));
  std::copy(gender, gender + length, std::back_inserter(gender_));
  std::copy(tension, tension + length, std::back_inserter(tension_));
  std::copy(breathiness, breathiness + length,
            std::back_inserter(breathiness_));
  std::copy(voicing, voicing + length, std::back_inserter(voicing_));
}

std::vector<double> PhraseSynth::Synth(LogCallback logCallback) {
  int fs = models_[0].fs();
  int fft_size = models_[0].fft_size();
  int width = models_[0].sp()[0].size();

  std::vector<double> f0;
  std::vector<std::vector<double>> sp;
  std::vector<std::vector<double>> ap;
  std::vector<int> dirty;

  for (int k = 0; k < models_.size(); ++k) {
    auto& model = models_[k];
    auto& timing = timings_[k];
    f0.resize(timing.p4, 0);
    sp.resize(timing.p4,
              std::vector<double>(width, world::kMySafeGuardMinimum));
    ap.resize(timing.p4, std::vector<double>(width, 1));
    dirty.resize(timing.p4, 0);

    for (int i = timing.p0; i < timing.p4; ++i) {
      double weight = 1;
      if (i < timing.p1) {
        weight = (double)(i - timing.p0) / (timing.p1 - timing.p0);
      } else if (i >= timing.p3) {
        weight = (double)(timing.p4 - i) / (timing.p4 - timing.p3);
      }
      int model_i = timing.left_extra + timing.skip + i - timing.p0;
      if (model_i < timing.left_extra) {
        continue;
      }
      if (dirty[i] == 0 || weight > 0.5) {
        f0[i] = model.f0()[model_i];
      }
      for (int j = 0; j < width; j++) {
        sp[i][j] = sp[i][j] + model.sp()[model_i][j] * weight;
      }
      double wa = dirty[i] == 0 ? 0 : 1.0 - weight;
      double wb = dirty[i] == 0 ? 1 : weight;
      for (int j = 0; j < width; j++) {
        ap[i][j] = ap[i][j] * wa + model.ap()[model_i][j] * wb;
      }
      dirty[i] = 1;
    }
  }
  int length = f0.size() + 1;
  f0.resize(length, f0.back());
  sp.resize(length, sp.back());
  ap.resize(length, ap.back());

  f0_.resize(length, f0_.back());
  gender_.resize(length, gender_.back());
  tension_.resize(length, tension_.back());
  breathiness_.resize(length, breathiness_.back());
  voicing_.resize(length, voicing_.back());

  std::vector<std::vector<double>> ten = vec2d(width, length, 1);
  std::vector<double> bre(length, 1);
  std::vector<double> voi(length, 1);
  for (int i = 0; i < length; ++i) {
    if (f0[i] > 0) {
      f0[i] = f0_[i];
    }
    ShiftGender(sp[i].data(), width, (gender_[i] - 0.5) * 200);
    bre[i] = breathiness_[i] > 0.5 ? breathiness_[i] * 4 : breathiness_[i] * 2;
    ten[i] =
        GetTensionCoefficients(f0_[i], fs, (tension_[i] - 0.5) * 200, width);
    voi[i] = voicing_[i];
  }

  Model final_model(models_[0].fs(), models_[0].frame_ms(),
                    models_[0].fft_size());
  final_model.f0() = std::move(f0);
  final_model.ap() = std::move(ap);
  final_model.sp() = std::move(sp);

  final_model.Synth(ten, bre, voi);
  std::vector<double> samples = std::move(final_model.samples());

  // 10ms fade out to ease abruptive ending.
  int fade_out_samples = static_cast<int>(fs * 10.0 / 1000.0);
  for (int i = 0; i < fade_out_samples && i < samples.size(); ++i) {
    samples[samples.size() - 1 - i] *= i * 1.0 / fade_out_samples;
  }
  return samples;
}

}  // namespace worldline

#include "resampler.h"

#include <algorithm>
#include <cmath>
#include <fstream>
#include <iostream>
#include <iterator>
#include <memory>
#include <optional>
#include <sstream>
#include <vector>

#include "audioio.h"
#include "classic_args.h"
#include "timing.h"
#include "world/constantnumbers.h"
#include "worldline/common/vec_utils.h"
#include "worldline/f0/f0_estimator.h"
#include "worldline/f0/frq_estimator.h"
#include "worldline/f0/pyin_estimator.h"
#include "worldline/model/effects.h"
#include "worldline/model/model.h"
#include "worldline/synth_request.h"

namespace worldline {

const double frame_ms = 10;
const int padding = 2;

Resampler::Resampler(SynthRequest request) : request_(request) {
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

  model_ = std::make_unique<Model>(std::move(samples), request.sample_fs,
                                   frame_ms, std::move(f0_estimator));
}

static std::string ReadFrqFile(const std::string& wav_path) {
  int last_dot_index = wav_path.find_last_of('.');
  if (last_dot_index <= 0) {
    return "";
  }
  std::string frq_path = wav_path.substr(0, last_dot_index) + "_" +
                         wav_path.substr(last_dot_index + 1) + ".frq";
  std::ifstream frq_stream(frq_path.c_str(), std::ios::binary);
  if (!frq_stream.good() || !frq_stream.is_open()) {
    std::cout << "frq file not found" << std::endl;
    return "";
  }
  std::cout << "frq file found" << std::endl;
  std::stringstream sstream;
  std::copy(std::istreambuf_iterator<char>(frq_stream),
            std::istreambuf_iterator<char>(),
            std::ostreambuf_iterator<char>(sstream));
  frq_stream.close();
  return sstream.str();
}

Resampler::Resampler(std::vector<std::string> args)
    : request_(ParseClassicArgs(args)) {
  std::vector<double> samples(GetAudioLength(args[0].c_str()), 0);
  int fs;
  int nbit;
  wavread(args[0].c_str(), &fs, &nbit, samples.data());
  std::string frq_data = ReadFrqFile(args[0]);
  std::unique_ptr<F0Estimator> f0_estimator;
  if (frq_data.empty()) {
    f0_estimator = std::make_unique<PyinEstimator>();
  } else {
    f0_estimator = std::make_unique<FrqEstimator>(frq_data);
  }
  model_ = std::make_unique<Model>(std::move(samples), fs, frame_ms,
                                   std::move(f0_estimator));
}

std::vector<double> Resampler::Resample() {
  double src_max = vec_maxabs(model_->samples());

  model_->BuildF0();

  auto mapping = GetTimeMapping(*model_, request_);

  // Trim model to input region.
  double start_ms = request_.offset;
  double length_ms = GetInTotalMs(*model_, request_);
  int start_frame = static_cast<int>(start_ms / frame_ms);
  int length_frame =
      static_cast<int>(std::ceil(start_ms + length_ms) / frame_ms) -
      start_frame;
  double left_trimmed = start_frame * frame_ms;
  double left_extra = start_ms - left_trimmed;

  model_->Trim(start_frame, length_frame);
  ShiftTimeMapping(mapping, -left_trimmed);

  model_->BuildSp();
  model_->BuildAp();

  PadTimeMapping(mapping, padding);
  left_extra += frame_ms * padding;

  model_->Remap(mapping);

  ApplyPitch();

  std::vector<std::vector<double>> tension;
  std::vector<double> breathiness;
  std::vector<double> voicing;
  ApplyEffects(&tension, &breathiness, &voicing);

  model_->Synth(tension, breathiness, voicing);

  // Trims left and right extra.
  std::vector<double> samples = std::move(model_->samples());
  int left_extra_samples = model_->MsToSamples(left_extra);
  int length_samples = model_->MsToSamples(request_.required_length);
  samples.erase(samples.begin(), samples.begin() + left_extra_samples);
  if (samples.size() > length_samples) {
    samples.erase(samples.begin() + length_samples, samples.end());
  }

  double out_max = vec_maxabs(samples);
  AutoGain(samples, src_max, out_max, model_->GetVoicedRatio(), request_.volume,
           request_.flag_P);
  return samples;
}

void Resampler::ApplyEffects(std::vector<std::vector<double>>* tension,
                             std::vector<double>* breathiness,
                             std::vector<double>* voicing) {
  if (request_.flag_g != 0) {
    ShiftGender(model_->sp(), request_.flag_g);
  }

  model_->SynthParams(tension, breathiness, voicing);

  for (int i = 0; i < model_->f0().size(); ++i) {
    tension->at(i) =
        GetTensionCoefficients(model_->f0()[i], model_->fs(), request_.flag_Mt,
                               model_->sp()[i].size());
  }

  double breathiness_value =
      1.0 + (request_.flag_Mb < 0 ? request_.flag_Mb * 0.01
                                  : request_.flag_Mb * 0.02);
  std::fill(breathiness->begin(), breathiness->end(), breathiness_value);

  double voicing_value = request_.flag_Mv * 0.01;
  std::fill(voicing->begin(), voicing->end(), voicing_value);
}

static constexpr double a = 1.05946309436;  // std::pow(2, 1.0 / 12);

void Resampler::ApplyPitch() {
  double step_ms = 60000.0 / request_.tempo / 480.0 * 5;
  double time_ms = 0;
  double left_pitch =
      request_.pitch_bend_length > 0 ? request_.pitch_bend[0] : 0;
  double right_pitch = request_.pitch_bend_length > 0
                           ? request_.pitch_bend[request_.pitch_bend_length - 1]
                           : 0;
  for (int i = 0; i < model_->f0().size(); ++i) {
    int t = time_ms / step_ms;
    int index = static_cast<int>(t);
    t -= index;
    int pitch;
    if (model_->f0()[i] < world::kFloorF0) {
      pitch = 0;
    } else if (index - padding < 0) {
      pitch = left_pitch;
    } else if (index - padding < request_.pitch_bend_length) {
      pitch = request_.pitch_bend[index - padding] * (1 - t) +
              request_.pitch_bend[index - padding + 1] * t;
    } else {
      pitch = right_pitch;
    }
    double tone = request_.tone + pitch * 0.01;
    double freq = 440.0 * std::pow(a, tone - 69);
    model_->f0()[i] = freq;
    time_ms += model_->frame_ms();
  }
}

}  // namespace worldline

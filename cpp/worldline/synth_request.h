#ifndef WORLDLINE_SYNTH_REQUEST_H_
#define WORLDLINE_SYNTH_REQUEST_H_

#include <cstdint>

extern "C" {

struct SynthRequest {
  std::int32_t sample_fs;
  std::int32_t sample_length;
  double* sample;
  std::int32_t frq_length = 0;
  char* frq = 0;
  std::int32_t tone;
  double con_vel;
  double offset;
  double required_length;
  double consonant;
  double cut_off;
  double volume;
  double modulation;
  double tempo;
  std::int32_t pitch_bend_length = 0;
  std::int32_t* pitch_bend = 0;

  int flag_g;
  int flag_O;
  int flag_P;
  int flag_Mt;
  int flag_Mb;
  int flag_Mv;
};

struct SynthOutput {
  std::int64_t data_length;
  char* data;
};
}

#endif  // WORLDLINE_SYNTH_REQUEST_H_

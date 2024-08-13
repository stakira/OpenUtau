//-----------------------------------------------------------------------------
// Copyright 2012-2015 Masanori Morise. All Rights Reserved.
// Author: mmorise [at] yamanashi.ac.jp (Masanori Morise)
//
// Voice synthesis based on f0, spectrogram and spectrogram of
// excitation signal.
//-----------------------------------------------------------------------------
#include "synthesisplatinum.h"

#include <math.h>

#include "world/common.h"
#include "world/constantnumbers.h"
#include "world/matlabfunctions.h"

namespace {

//-----------------------------------------------------------------------------
// GetOneFrameSegment() calculates a glottal vibration based on the spectral
// envelope and excitation signal.
// Caution:
//   minimum_phase and inverse_real_fft are allocated in advance. This is for
//   the rapid processing because set of FFT requires much computational cost.
//-----------------------------------------------------------------------------
void GetOneFrameSegment(double *f0, double **spectrogram,
    double **residual_spectrogram, int fft_size, int current_frame,
    MinimumPhaseAnalysis *minimum_phase, InverseRealFFT *inverse_real_fft,
    double *y) {
  for (int i = 0; i <= fft_size / 2; ++i)
    minimum_phase->log_spectrum[i] =
    log(spectrogram[current_frame][i]) / 2.0;
  GetMinimumPhaseSpectrum(minimum_phase);

  inverse_real_fft->spectrum[0][0] =
    minimum_phase->minimum_phase_spectrum[0][0] *
    residual_spectrogram[current_frame][0];
  inverse_real_fft->spectrum[0][1] = 0.0;

  for (int i = 1; i < fft_size / 2; ++i) {
    inverse_real_fft->spectrum[i][0] =
      minimum_phase->minimum_phase_spectrum[i][0] *
      residual_spectrogram[current_frame][(i - 1) * 2 + 1] -
      minimum_phase->minimum_phase_spectrum[i][1] *
      residual_spectrogram[current_frame][i * 2];
    inverse_real_fft->spectrum[i][1] =
      minimum_phase->minimum_phase_spectrum[i][0] *
      residual_spectrogram[current_frame][i * 2] +
      minimum_phase->minimum_phase_spectrum[i][1] *
      residual_spectrogram[current_frame][(i - 1) * 2 + 1];
  }
  inverse_real_fft->spectrum[fft_size / 2][0] =
    minimum_phase->minimum_phase_spectrum[fft_size / 2][0] *
    residual_spectrogram[current_frame][fft_size - 1];
  inverse_real_fft->spectrum[fft_size / 2][1] = 0;

  fft_execute(inverse_real_fft->inverse_fft);

  for (int i = 0; i < fft_size; ++i)
    y[i] = inverse_real_fft->waveform[i] / fft_size;
}

void GetTemporalParametersForTimeBase(double *f0, int f0_length, int fs,
    int y_length, double frame_period, double *time_axis,
    double *coarse_f0, double *coarse_vuv) {
  for (int i = 0; i < y_length; ++i)
    time_axis[i] = i / static_cast<double>(fs);
  for (int i = 0; i < f0_length + 1; ++i)
    coarse_f0[i] = f0[i];
  coarse_f0[f0_length] = coarse_f0[f0_length - 1] * 2 -
    coarse_f0[f0_length - 2];
  for (int i = 0; i < f0_length + 1; ++i)
    coarse_vuv[i] = f0[i] == 0.0 ? 0.0 : 1.0;
  coarse_vuv[f0_length] = coarse_vuv[f0_length - 1] * 2 -
    coarse_vuv[f0_length - 2];
}


int GetPulseLocationsForTimeBase(double *interpolated_f0, double *time_axis,
    int y_length, int fs, double *pulse_locations, double *fractional_index) {

  double *total_phase = new double[y_length];
  total_phase[0] = 2.0 * world::kPi * interpolated_f0[0] / fs;
  for (int i = 1; i < y_length; ++i)
    total_phase[i] = total_phase[i - 1] +
      2.0 * world::kPi * interpolated_f0[i] / fs;

  double *wrap_phase = new double[y_length];
  for (int i = 0; i < y_length; ++i)
    wrap_phase[i] = fmod(total_phase[i], 2.0 * world::kPi);

  double *wrap_phase_abs = new double[y_length];
  for (int i = 0; i < y_length - 1; ++i)
    wrap_phase_abs[i] = fabs(wrap_phase[i + 1] - wrap_phase[i]);

  int *tmp_index = new int[y_length];
  int number_of_pulses = 0;
  for (int i = 0; i < y_length - 1; ++i) {
    if (wrap_phase_abs[i] > world::kPi) {
      tmp_index[number_of_pulses] = i;
      pulse_locations[number_of_pulses++] = time_axis[i];
    }
  }

  for (int i = 0; i < number_of_pulses; ++i)
    fractional_index[i] =
      (fmod(total_phase[tmp_index[i]], world::kPi) - world::kPi) /
      (2.0 * world::kPi * interpolated_f0[tmp_index[i]] / fs);

  delete[] tmp_index;
  delete[] wrap_phase_abs;
  delete[] wrap_phase;
  delete[] total_phase;

  return number_of_pulses;
}

int GetTimeBase(double *f0, int f0_length, int fs,
    double frame_period, int y_length, double *pulse_locations,
    double *fractional_index) {
  double *time_axis = new double[y_length];
  double *coarse_f0 = new double[f0_length + 1];
  double *coarse_vuv = new double[f0_length + 1];
  double *interpolated_vuv = new double[y_length];
  double *interpolated_f0 = new double[y_length];
  GetTemporalParametersForTimeBase(f0, f0_length, fs, y_length, frame_period,
      time_axis, coarse_f0, coarse_vuv);

  interp1Q(0.0, frame_period, coarse_f0, f0_length + 1,
      time_axis, y_length, interpolated_f0);
  interp1Q(0.0, frame_period, coarse_vuv, f0_length + 1,
      time_axis, y_length, interpolated_vuv);
  for (int i = 0; i < y_length; ++i)
    interpolated_vuv[i] = interpolated_vuv[i] > 0.5 ? 1.0 : 0.0;

  for (int i = 0; i < y_length; ++i)
    interpolated_f0[i] = interpolated_vuv[i] ==
      0.0 ? world::kDefaultF0 : interpolated_f0[i];

  int number_of_pulses = GetPulseLocationsForTimeBase(interpolated_f0,
      time_axis, y_length, fs, pulse_locations, fractional_index);

  delete[] time_axis;
  delete[] coarse_f0;
  delete[] coarse_vuv;
  delete[] interpolated_vuv;
  delete[] interpolated_f0;

  return number_of_pulses;
}

}  // namespace

void SynthesisPlatinum(double *f0, int f0_length, double **spectrogram,
    double **residual_spectrogram, int fft_size, double frame_period,
    int fs, int y_length, double *y) {
  double *impulse_response = new double[fft_size];
  for (int i = 0; i < y_length; ++i) y[i] = 0.0;

  MinimumPhaseAnalysis minimum_phase = {0};
  InitializeMinimumPhaseAnalysis(fft_size, &minimum_phase);
  InverseRealFFT inverse_real_fft = {0};
  InitializeInverseRealFFT(fft_size, &inverse_real_fft);

  double *pulse_locations = new double[y_length];
  // fractional_index is for the future version of WORLD.s
  // This version does not use it.
  double *fractional_index = new double[y_length];
  int number_of_pulses = GetTimeBase(f0, f0_length, fs, frame_period / 1000.0,
      y_length, pulse_locations, fractional_index);

  // Length used for the synthesis is unclear.
  const int kFrameLength = fft_size / 2;

  int pulse_index;
  for (int i = 0; i < number_of_pulses; ++i) {
    pulse_index = matlab_round(pulse_locations[i] * fs);

    GetOneFrameSegment(f0, spectrogram, residual_spectrogram, fft_size,
        static_cast<int>(pulse_locations[i] / frame_period * 1000.0),
        &minimum_phase, &inverse_real_fft, impulse_response);

    for (int i = pulse_index;
        i < MyMinInt(pulse_index + kFrameLength, y_length - 1); ++i)
      y[i] += impulse_response[i - pulse_index];
  }

  DestroyMinimumPhaseAnalysis(&minimum_phase);
  DestroyInverseRealFFT(&inverse_real_fft);
  delete[] impulse_response;
  delete[] pulse_locations;
  delete[] fractional_index;
}

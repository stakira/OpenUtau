//-----------------------------------------------------------------------------
// Copyright 2012-2015 Masanori Morise. All Rights Reserved.
// Author: mmorise [at] yamanashi.ac.jp (Masanori Morise)
//
// Excitation signal extraction by PLATINUM.
// Ths excitation signal is calculated as the signal that is the convolution of
// the spectrum of windowed signal and inverse function of spectral envelop.
//-----------------------------------------------------------------------------
#include "platinum.h"

#include <math.h>

#include "world/common.h"
#include "world/constantnumbers.h"
#include "world/matlabfunctions.h"

namespace {

//-----------------------------------------------------------------------------
// GetNearestPulseIndex() calculates the nearest index of pulse_locations.
// Platinum() calculates the residual spectrum in all frame (frame_period
// interval), while the pulse_locations are not calculated in all frame.
//-----------------------------------------------------------------------------
int GetNearestPulseIndex(int pulse_count, double current_time,
    int fs, double *pulse_locations) {
  const double kSafeGuradForAmplitude = 100000.0;
  double minimum_value = kSafeGuradForAmplitude;  // safe guard

  int index = 0;
  int minimum_index = 0;
  double tmp = 0.0;
  for (int i = 0; i < pulse_count; ++i) {
    tmp = fabs(pulse_locations[i] - current_time);
    if (tmp < minimum_value) {
      minimum_value = tmp;
      minimum_index = i;
    }
    index = matlab_round(pulse_locations[minimum_index] * fs);
  }

  return index;
}

//-----------------------------------------------------------------------------
// GetOneFrameResidualSpec() calculates the residual spectrum.
// Residual spectrum is calculated by convoluting the spectrum of widnowed
// waveform and the inverse function of minimum phase spectrum.
//-----------------------------------------------------------------------------
void GetOneFrameResidualSpec(double *x, int x_length, int fs,
    double current_time, double current_t0, ForwardRealFFT *forward_real_fft,
    MinimumPhaseAnalysis *minimum_phase, double *pulse_locations,
    int pulse_count, double *residual_spectrum) {
  GetMinimumPhaseSpectrum(minimum_phase);

  int index = GetNearestPulseIndex(pulse_count,
    current_time, fs, pulse_locations);

  int window_length = matlab_round(current_t0 * 2.0);

  // Windowing and FFT
  for (int i = 0; i < window_length; ++i)
    forward_real_fft->waveform[i] =
      x[MyMinInt(x_length - 1, MyMaxInt(0, i + index - matlab_round(current_t0)))] *
      (0.5 - 0.5 * cos(2.0 * world::kPi * (i + 1.0) /
      (window_length + 1.0)));
  for (int i = window_length; i < minimum_phase->fft_size; ++i)
    forward_real_fft->waveform[i] = 0.0;

  fft_execute(forward_real_fft->forward_fft);

  // Convolution
  residual_spectrum[0] = forward_real_fft->spectrum[0][0] /
    minimum_phase->minimum_phase_spectrum[0][0];
  double tmp;
  for (int i = 0; i < minimum_phase->fft_size / 2 - 1; ++i) {
    tmp = minimum_phase->minimum_phase_spectrum[i + 1][0] *
      minimum_phase->minimum_phase_spectrum[i + 1][0] +
      minimum_phase->minimum_phase_spectrum[i + 1][1] *
      minimum_phase->minimum_phase_spectrum[i + 1][1];
    residual_spectrum[i * 2 + 1] =
      (minimum_phase->minimum_phase_spectrum[i + 1][0] *
      forward_real_fft->spectrum[i + 1][0] +
      minimum_phase->minimum_phase_spectrum[i + 1][1] *
      forward_real_fft->spectrum[i + 1][1]) / tmp;
    residual_spectrum[i * 2 + 2] =
      (-minimum_phase->minimum_phase_spectrum[i + 1][1] *
      forward_real_fft->spectrum[i + 1][0] +
      minimum_phase->minimum_phase_spectrum[i + 1][0] *
      forward_real_fft->spectrum[i + 1][1]) / tmp;
  }
  residual_spectrum[minimum_phase->fft_size - 1] =
    forward_real_fft->spectrum[minimum_phase->fft_size / 2][0] /
    minimum_phase->minimum_phase_spectrum[minimum_phase->fft_size / 2][0];
}

//-----------------------------------------------------------------------------
// GetWedgeInOneSection() is calculates a wedge in one voiced/unvoiced section.
//-----------------------------------------------------------------------------
int GetWedgeInOneSection(double *x, int x_length, int fs, double *f0,
    double frame_period, int start_index, int end_index) {
  int center_time = (start_index + end_index + 1) / 2;
  int t0 = matlab_round((fs / (f0[center_time] ==
    0.0 ? world::kDefaultF0 : f0[center_time])));
  int center_index =
    matlab_round((1 + center_time) * frame_period * fs);

  int wedge = 0;
  double peak_value = 0.0;
  double tmp_amplitude = 0.0;
  int tmp_index = 0;
  for (int j = 0; j < t0 * 2 + 1; ++j) {
    tmp_index = MyMaxInt(0, MyMinInt(x_length - 1, center_index - t0 + j));
    tmp_amplitude =
      fabs(x[tmp_index]);
    if (tmp_amplitude > peak_value) {
      peak_value = tmp_amplitude;
      wedge = tmp_index;
    }
  }
  return wedge;
}

//-----------------------------------------------------------------------------
// GetWedgeList() calculates the suitable peak amplitude of each voiced
// section. Peak amplitudes are used as "Wedge" to calculate the temporal
// positions used for windowing.
//-----------------------------------------------------------------------------
void GetWedgeList(double *x, int x_length, int number_of_voiced_sections,
    int *start_list, int *end_list, int fs, double frame_period, double *f0,
    int *wedge_list) {
  for (int i = 0; i < number_of_voiced_sections; ++i) {
    wedge_list[i] = GetWedgeInOneSection(x, x_length, fs,
        f0, frame_period, start_list[i], end_list[i]);
  }
}

//-----------------------------------------------------------------------------
// GetTemporalBoundaries() calculates the temporal boundaries in VUV.
//-----------------------------------------------------------------------------
void GetTemporalBoundaries(double *f0, int f0_length,
    int number_of_voiced_sections, int *start_list, int *end_list) {
  int start_count = 1;
  int end_count = 0;

  start_list[0] = 0;

  end_list[number_of_voiced_sections - 1] = f0_length - 1;
  for (int i = 1; i < f0_length; ++i) {
    if (f0[i] != 0.0 && f0[i - 1] == 0.0) {
      end_list[end_count++] = i - 1;
      start_list[start_count++] = i;
    }
    if (f0[i] == 0.0 && f0[i - 1] != 0.0) {
      end_list[end_count++] = i - 1;
      start_list[start_count++] = i;
    }
  }
}

//-----------------------------------------------------------------------------
// GetNumberOfVoicedSections() calculates the number of voiced sections.
//-----------------------------------------------------------------------------
int GetNumberOfVoicedSections(double *f0, int f0_length) {
  int number_of_voiced_sections = 0;
  for (int i = 1; i < f0_length; ++i)
    if (f0[i] != 0.0 && f0[i - 1] == 0.0) number_of_voiced_sections++;
  number_of_voiced_sections += number_of_voiced_sections - 1;
  if (f0[0] == 0) number_of_voiced_sections++;
  if (f0[f0_length - 1] == 0) number_of_voiced_sections++;

  return number_of_voiced_sections;
}

//-----------------------------------------------------------------------------
// GetPulseLocationsInOneSection() calculates the peak locations in one frame.
//-----------------------------------------------------------------------------
int GetPulseLocationsInOneSection(int fs, int x_length, int start_index,
    int end_index, double frame_period, int current_wedge,
    double *total_phase, int current_count, double *pulse_locations) {
  start_index =
    MyMaxInt(0, matlab_round(fs * start_index * frame_period));
  end_index = MyMinInt(x_length - 1,
      matlab_round(fs * (end_index + 1.0) * frame_period));

  double tmp = total_phase[current_wedge] - 2 * world::kPi *
    floor(total_phase[0] - total_phase[current_wedge] / 2.0 / world::kPi);
  for (int i = start_index; i < end_index; ++i)
    if (fabs(fmod(total_phase[i + 1] - tmp, 2.0 * world::kPi) -
        fmod(total_phase[i] - tmp, 2.0 * world::kPi)) > world::kPi / 2.0)
      pulse_locations[current_count++] = static_cast<double>(i) / fs;
  return current_count;
}

//-----------------------------------------------------------------------------
// GetTotalPhase() calculates the phase response from f0 contour.
// Sampling period of f0 time_axis[1] - time_axis[0], while the sampling
// period of total_phase is 1 / fs. total_phase is used to calculate the
// temporal positions of glottal pulse.
//-----------------------------------------------------------------------------
void GetTotalPhase(double *f0, int f0_length, int x_length, double *time_axis,
    int fs, double *total_phase) {
  double *fixed_f0  = new double[f0_length];
  double *time_axis_of_x = new double[x_length];
  double *interpolated_f0 = new double[x_length];

  for (int i = 0; i < f0_length; ++i)
    fixed_f0[i] = f0[i] == 0 ? world::kDefaultF0 : f0[i];
  for (int i = 0; i < x_length; ++i)
    time_axis_of_x[i] = static_cast<double>(i) / fs;

  interp1(time_axis, fixed_f0, f0_length, time_axis_of_x,
      x_length, interpolated_f0);
  total_phase[0] = interpolated_f0[0] * 2.0 * world::kPi / fs;
  for (int i = 1; i < x_length; ++i)
    total_phase[i] = total_phase[i - 1] +
    interpolated_f0[i] * 2.0 * world::kPi / fs;

  delete[] fixed_f0;
  delete[] interpolated_f0;
  delete[] time_axis_of_x;
}

//-----------------------------------------------------------------------------
// GetPulseLocations() calculates the temporal positions (maximum peak index)
// for windowing. These positions are calculated in each frame.
// Pulse means "glottal pulse"
//-----------------------------------------------------------------------------
int GetPulseLocations(double *x, int x_length, int fs, double *f0,
    int f0_length, double *time_axis, double frame_period,
    double *pulse_locations) {
  int number_of_voiced_sections = GetNumberOfVoicedSections(f0, f0_length);

  int *start_list = new int[number_of_voiced_sections];
  int *end_list = new int[number_of_voiced_sections];
  GetTemporalBoundaries(f0, f0_length, number_of_voiced_sections,
      start_list, end_list);

  int *wedge_list = new int[number_of_voiced_sections];
  GetWedgeList(x, x_length, number_of_voiced_sections, start_list, end_list,
      fs, frame_period, f0, wedge_list);

  double *total_phase = new double[x_length];
  GetTotalPhase(f0, f0_length, x_length, time_axis, fs, total_phase);

  int number_of_pulses = 0;
  for (int i = 0; i < number_of_voiced_sections; ++i) {
    number_of_pulses = GetPulseLocationsInOneSection(fs, x_length,
        start_list[i], end_list[i], frame_period, wedge_list[i], total_phase,
        number_of_pulses, pulse_locations);
  }

  delete[] total_phase;
  delete[] wedge_list;
  delete[] end_list;
  delete[] start_list;
  return number_of_pulses;
}

}  // namespace

void Platinum(double *x, int x_length, int fs, double *time_axis, double *f0,
    int f0_length, double **spectrogram, int fft_size,
    double **residual_spectrogram) {
  double frame_period = (time_axis[1] - time_axis[0]);

  double *pulse_locations = new double[x_length];
  int number_of_pulses = GetPulseLocations(x, x_length, fs, f0, f0_length,
      time_axis, frame_period, pulse_locations);

  double *residual_spectrum = new double[fft_size];
  for (int i = 0; i < fft_size; ++i)
    residual_spectrogram[0][i] = world::kMySafeGuardMinimum;

  // For minimum phase spectrum
  MinimumPhaseAnalysis minimum_phase = {0};
  InitializeMinimumPhaseAnalysis(fft_size, &minimum_phase);
  // For forward real FFT
  ForwardRealFFT forward_real_fft = {0};
  InitializeForwardRealFFT(fft_size, &forward_real_fft);

  double current_f0;
  for (int i = 0; i < f0_length; ++i) {
    current_f0 =
      f0[i] <= world::kFloorF0 ? world::kDefaultF0 : f0[i];
    for (int j = 0; j <= fft_size / 2; ++j)
      minimum_phase.log_spectrum[j] = log(spectrogram[i][j]) / 2.0;

    GetOneFrameResidualSpec(x, x_length, fs,
        i * frame_period, fs / current_f0,
        &forward_real_fft, &minimum_phase, pulse_locations,
        number_of_pulses, residual_spectrogram[i]);
  }
  DestroyMinimumPhaseAnalysis(&minimum_phase);
  DestroyForwardRealFFT(&forward_real_fft);

  delete[] residual_spectrum;
  delete[] pulse_locations;
}

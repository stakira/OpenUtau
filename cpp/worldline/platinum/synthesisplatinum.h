//-----------------------------------------------------------------------------
// Copyright 2012-2014 Masanori Morise. All Rights Reserved.
// Author: mmorise [at] yamanashi.ac.jp (Masanori Morise)
//-----------------------------------------------------------------------------
#ifndef WORLDLINE_PLATINUM_SYNTHESISPLATINUM_H_
#define WORLDLINE_PLATINUM_SYNTHESISPLATINUM_H_

//-----------------------------------------------------------------------------
// SynthesisPlatinum() synthesizes the voice from three parameters.
// Input:
//   f0                     : f0 contour
//   f0_length              : Length of f0
//   spectrogram            : Spectrogram (WORLD assumes spectrogram by Star())
//   residual_spectrogram   : Extracted spectrum of the excitation signal
//   fft_size               : FFT size used for Star() and Platinum()
//   frame_period           : Temporal inverval for Dio()
//   fs                     : Sampling frequency
//   y_length               : Length of Output (allocated in advance)
// Output:
//   y                      : Synthesized voice
//-----------------------------------------------------------------------------
void SynthesisPlatinum(double *f0, int f0_length, double **spectrogram,
                       double **residual_spectrogram, int fft_size,
                       double frame_period, int fs, int y_length, double *y);

#endif  // WORLDLINE_PLATINUM_SYNTHESISPLATINUM_H_

#ifndef WORLDLINE_MODEL_EFFECTS_H_
#define WORLDLINE_MODEL_EFFECTS_H_

#include <string>
#include <vector>

namespace worldline {

// value range [-100, 100]
void ShiftGender(std::vector<std::vector<double>>& sp, int value);

// value range [-100, 100]
void ShiftGender(double* sp, int width, int value);

// value range [-100, 100]
std::vector<double> GetTensionCoefficients(double f0, int fs, int value,
                                           int width);

void AutoGain(std::vector<double>& samples, double src_max, double out_max,
              double voiced_ratio, int volume, int peakComp);

}  // namespace worldline

#endif  // WORLDLINE_MODEL_EFFECTS_H_

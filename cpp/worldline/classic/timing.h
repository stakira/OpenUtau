#ifndef WORLDLINE_CLASSIC_TIMING_H_
#define WORLDLINE_CLASSIC_TIMING_H_

#include <vector>

#include "worldline/model/model.h"
#include "worldline/synth_request.h"

namespace worldline {

double GetInTotalMs(Model& model, const SynthRequest& request);

std::vector<double> GetTimeMapping(Model& model, const SynthRequest& request);

void ShiftTimeMapping(std::vector<double>& mapping, double shift);

void PadTimeMapping(std::vector<double>& mapping, int frames);

}  // namespace worldline

#endif  // WORLDLINE_CLASSIC_TIMING_H_

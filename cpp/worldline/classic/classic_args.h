#ifndef WORLDLINE_CLASSIC_CLASSIC_ARGS_H_
#define WORLDLINE_CLASSIC_CLASSIC_ARGS_H_

#include <string>
#include <vector>

#include "worldline/synth_request.h"

namespace worldline {

SynthRequest ParseClassicArgs(const std::vector<std::string>& args);

void LogClassicArgs(const SynthRequest& request, const std::string& logfile);

}  // namespace worldline

#endif  // WORLDLINE_CLASSIC_CLASSIC_ARGS_H_

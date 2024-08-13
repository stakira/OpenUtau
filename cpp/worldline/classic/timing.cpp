#include "timing.h"

#include <cmath>
#include <vector>

#include "worldline/model/model.h"
#include "worldline/synth_request.h"

namespace worldline {

double GetInTotalMs(Model& model, const SynthRequest& request) {
  return request.cut_off < 0
             ? -request.cut_off
             : model.total_ms() - request.offset - request.cut_off;
}

std::vector<double> GetTimeMapping(Model& model, const SynthRequest& request) {
  std::vector<double> mapping;
  double frame_ms = model.frame_ms();
  double con_speed = std::pow(0.5, 1.0 - request.con_vel / 100.0);
  double in_total_ms = GetInTotalMs(model, request);
  double in_con_ms = std::max(1.0, request.consonant);
  double in_vow_ms = std::max(1.0, in_total_ms - in_con_ms);
  double out_total_ms = request.required_length;
  double out_con_ms = in_con_ms / con_speed;
  double out_vow_ms = std::max(0.0, out_total_ms - out_con_ms) + frame_ms;
  double vow_speed = in_vow_ms / out_vow_ms;
  vow_speed = std::max(0.0, std::min(1.0, vow_speed));

  double pos_ms = request.offset;
  while (mapping.size() * frame_ms <= out_con_ms) {
    mapping.push_back(pos_ms);
    pos_ms += con_speed * frame_ms;
  }
  while (mapping.size() * frame_ms <= out_total_ms) {
    mapping.push_back(pos_ms);
    pos_ms += vow_speed * frame_ms;
  }
  return mapping;
}

void ShiftTimeMapping(std::vector<double>& mapping, double shift) {
  for (int i = 0; i < mapping.size(); ++i) {
    mapping[i] = mapping[i] + shift;
  }
}

void PadTimeMapping(std::vector<double>& mapping, int frames) {
  mapping.insert(mapping.begin(), frames, mapping.front());
  mapping.insert(mapping.end(), frames, mapping.back());
}

}  // namespace worldline

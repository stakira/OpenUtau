#ifndef WORLDLINE_CLASSIC_FRQ_H_
#define WORLDLINE_CLASSIC_FRQ_H_

#include <string>
#include <vector>

namespace worldline {

typedef struct {
  int hop_size;
  double avg_frq;
  std::vector<double> f0;
  std::vector<double> amp;
} FrqData;

FrqData LoadFrq(const std::string_view data);

std::string DumpFrq(const FrqData& frq_data);

}  // namespace worldline

#endif  // WORLDLINE_CLASSIC_FRQ_H_

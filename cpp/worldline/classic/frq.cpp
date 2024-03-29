#include "frq.h"

#include <cassert>
#include <string>
#include <vector>

namespace worldline {

static int ReadInt(const char* data) {
  return *reinterpret_cast<const int*>(data);
}

static double ReadDouble(const char* data) {
  return *reinterpret_cast<const double*>(data);
}

FrqData LoadFrq(const std::string_view data) {
  FrqData frq_data;
  assert(data.substr(0, 8) == "FREQ0003");
  frq_data.hop_size = ReadInt(data.data() + 8);
  frq_data.avg_frq = ReadDouble(data.data() + 12);
  int num_frames = ReadInt(data.data() + 36);
  frq_data.f0 = std::vector<double>(num_frames);
  frq_data.amp = std::vector<double>(num_frames);
  const double* ptr = reinterpret_cast<const double*>(data.data() + 40);
  for (int i = 0; i < num_frames; ++i) {
    frq_data.f0[i] = *(ptr++);
    frq_data.amp[i] = *(ptr++);
  }
  return frq_data;
}

static void WriteInt(std::string& s, int v) {
  s.append(reinterpret_cast<const char*>(&v), sizeof(int));
}

static void WriteDouble(std::string& s, double v) {
  s.append(reinterpret_cast<const char*>(&v), sizeof(double));
}

std::string DumpFrq(const FrqData& frq_data) {
  std::string result;
  result.append("FREQ0003");
  WriteInt(result, frq_data.hop_size);
  WriteDouble(result, frq_data.avg_frq);
  for (int i = 0; i < 4; ++i) {
    WriteInt(result, 0);
  }
  WriteInt(result, frq_data.f0.size());
  for (int i = 0; i < frq_data.f0.size(); ++i) {
    WriteDouble(result, frq_data.f0.data()[i]);
    WriteDouble(result, frq_data.amp.data()[i]);
  }
  return result;
}

}  // namespace worldline

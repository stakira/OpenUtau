#include "classic_args.h"

#include <cmath>
#include <fstream>
#include <iostream>
#include <string>

#include "absl/strings/numbers.h"

namespace worldline {

static constexpr int name_to_tone[] = {9, 11, 0, 2, 4, 5, 7};  // A to G

static bool ParseTone(std::string_view name, int* tone) {
  if (name.size() < 2 || name.size() > 3) {
    return false;
  }
  bool sharp = name.size() == 3;
  int octave;
  if (!absl::SimpleAtoi(name.substr(name.size() - 1, 1), &octave)) {
    return false;
  }
  int tone_index = name[0] - 'A';
  if (tone_index >= 7) {
    return false;
  }
  *tone = name_to_tone[tone_index];
  *tone = 12 * (octave + 1) + *tone + (sharp ? 1 : 0);
  return true;
}

void ParseClassicFlag(std::string_view flags, std::string_view flag, int* value,
                      int default_value) {
  int index = flags.find(flag);
  if (index < 0) {
    *value = default_value;
    return;
  }
  int start = index + flag.size();
  int pos = start;
  while (pos < flags.size() &&
         (flags[pos] == '-' && pos == start || std::isdigit(flags[pos]))) {
    pos++;
  }
  if (pos == start) {
    *value = default_value;
    return;
  }
  std::string_view value_str = flags.substr(start, pos - start);
  if (!absl::SimpleAtoi(value_str, value)) {
    *value = default_value;
  }
}

static void ParseFlags(SynthRequest& request, std::string flags) {
  ParseClassicFlag(flags, "g", &request.flag_g, 0);
  request.flag_g = std::clamp(request.flag_g, -100, 100);
  if (request.flag_g != 0) {
    std::cout << "Flag g: " << request.flag_g << std::endl;
  }
  ParseClassicFlag(flags, "O", &request.flag_O, 0);
  if (request.flag_O != 0) {
    std::cout << "Flag O: " << request.flag_O << std::endl;
  }
  ParseClassicFlag(flags, "P", &request.flag_P, 86);
  request.flag_P = std::clamp(request.flag_P, 0, 100);
  std::cout << "Flag P: " << request.flag_P << std::endl;
  ParseClassicFlag(flags, "Mt", &request.flag_Mt, 0);
  request.flag_Mt = std::clamp(request.flag_Mt, -100, 100);
  if (request.flag_Mt != 0) {
    std::cout << "Flag Mt: " << request.flag_Mt << std::endl;
  }
  ParseClassicFlag(flags, "Mb", &request.flag_Mb, 0);
  request.flag_Mb = std::clamp(request.flag_Mb, -100, 100);
  if (request.flag_Mb != 0) {
    std::cout << "Flag Mb: " << request.flag_Mb << std::endl;
  }
  ParseClassicFlag(flags, "Mv", &request.flag_Mv, 100);
  request.flag_Mv = std::clamp(request.flag_Mv, 0, 100);
  if (request.flag_Mv != 100) {
    std::cout << "Flag Mv: " << request.flag_Mv << std::endl;
  }
}

static bool ParseTempo(std::string arg, double* tempo) {
  arg = arg.size() > 0 && !std::isdigit(arg[0]) ? arg.substr(1) : arg;
  return absl::SimpleAtod(arg, tempo);
}

static int CharToInt(char c) {
  if (c >= 'A' && c <= 'Z') {
    return c - 'A';
  }
  if (c >= 'a' && c <= 'z') {
    return c - 'a' + 26;
  }
  if (c >= '0' && c <= '9') {
    return c - '0' + 52;
  }
  if (c == '+') {
    return 62;
  }
  // c == '-'
  return 63;
}

static std::vector<int> ParsePitchBend(const std::string& encoded) {
  std::vector<int> result;
  int p = 0;
  while (p < encoded.size()) {
    if (encoded[p] == '#') {
      p++;
      int count = 0;
      while (encoded[p] != '#') {
        count = count * 10 + (encoded[p] - '0');
        p++;
      }
      p++;
      int pitch = result.back();
      while (count-- > 0) {
        result.push_back(pitch);
      }
    } else {
      int pitch = (CharToInt(encoded[p++]) << 6);
      pitch += CharToInt(encoded[p++]);
      if (pitch > 2048) {
        pitch -= 4096;
      }
      result.push_back(pitch);
    }
  }
  return result;
}

SynthRequest ParseClassicArgs(const std::vector<std::string>& args) {
  SynthRequest request;
  if (args.size() <= 2 || !ParseTone(args[2], &request.tone)) {
    request.tone = 40;
  }
  if (args.size() <= 3 || !absl::SimpleAtod(args[3], &request.con_vel)) {
    request.con_vel = 100;
  }
  if (args.size() <= 4) {
    ParseFlags(request, "");
  } else {
    ParseFlags(request, args[4]);
  }
  if (args.size() <= 5 || !absl::SimpleAtod(args[5], &request.offset)) {
    request.offset = 0;
  }
  if (args.size() <= 6 ||
      !absl::SimpleAtod(args[6], &request.required_length)) {
    request.required_length = 0;
  }
  if (args.size() <= 7 || !absl::SimpleAtod(args[7], &request.consonant)) {
    request.consonant = 0;
  }
  if (args.size() <= 8 || !absl::SimpleAtod(args[8], &request.cut_off)) {
    request.cut_off = 0;
  }
  if (args.size() <= 9 || !absl::SimpleAtod(args[9], &request.volume)) {
    request.volume = 100;
  }
  if (args.size() <= 10 || !absl::SimpleAtod(args[10], &request.modulation)) {
    request.modulation = 100;
  }
  if (args.size() <= 11 || !ParseTempo(args[11], &request.tempo)) {
    request.tempo = 120;
  }
  if (args.size() <= 12) {
    request.pitch_bend_length = 0;
    request.pitch_bend = nullptr;
  } else {
    std::vector<int> pitch_bend = ParsePitchBend(args[12]);
    request.pitch_bend_length = pitch_bend.size();
    int* pitch_bend_array = new int[pitch_bend.size()];
    std::copy(pitch_bend.begin(), pitch_bend.end(), pitch_bend_array);
    request.pitch_bend = pitch_bend_array;
  }
  return request;
}

void LogClassicArgs(const SynthRequest& request, const std::string& logfile) {
  std::ofstream f;
  f.open(logfile, std::ios::out | std::ios::app);
  f << "{";
  f << "\"tone\": " << request.tone << ", ";
  f << "\"con_vel\": " << request.con_vel << ", ";
  f << "\"flags\": g" << request.flag_g << "Mt" << request.flag_Mt << "O"
    << request.flag_O << "P" << request.flag_P << ", ";
  f << "\"offset\": " << request.offset << ", ";
  f << "\"required_length\": " << request.required_length << ", ";
  f << "\"consonant\": " << request.consonant << ", ";
  f << "\"cut_off\": " << request.cut_off << ", ";
  f << "\"volume\": " << request.volume << ", ";
  f << "\"modulation\": " << request.modulation << ", ";
  f << "\"tempo\": " << request.tempo << ", ";
  f << "\"pitch_bend\": [";
  for (int i = 0; i < request.pitch_bend_length; ++i) {
    f << request.pitch_bend[i];
    if (i < request.pitch_bend_length - 1) {
      f << ", ";
    }
  }
  f << "]}" << std::endl;
  f.close();
}

}  // namespace worldline

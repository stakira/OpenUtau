#include <filesystem>
#include <fstream>
#include <iostream>
#include <optional>
#include <string>
#include <vector>

#include "absl/debugging/failure_signal_handler.h"
#include "absl/debugging/symbolize.h"
#include "absl/strings/str_join.h"
#include "audioio.h"
#include "world/synthesis.h"
#include "worldline/classic/resampler.h"
#include "worldline/model/effects.h"
#include "worldline/synth_request.h"

int main(int argc, char** argv) {
  absl::InitializeSymbolizer(argv[0]);
  absl::FailureSignalHandlerOptions options;
  absl::InstallFailureSignalHandler(options);

  bool debug = false;
  std::string exe_path = std::string(argv[0]);
  std::vector<std::string> args;
  for (int i = 1; i < argc; ++i) {
    args.push_back(std::string(argv[i]));
    std::cout << i << " " << argv[i] << std::endl;
  }

  if (args.size() < 4) {
    std::cout << "Worldline v0.0.6 - StAkira" << std::endl;
    std::cout
        << "args: <input wavfile> <output file> <pitch_percent> <velocity> "
           "[<flags> [<offset> <length_require> [<fixed length> [<end_blank> "
           "[<volume> [<modulation> [<pich bend>...]]]]]]]"
        << std::endl;
    std::cout << "flags:" << std::endl;
    std::cout << "  P =86(0~100): peak compression" << std::endl;
    std::cout << "  g =0(-100~100): gender" << std::endl;
    std::cout << "  Mt=0(-100~100): tension" << std::endl;
    std::cout << "  Mb=0(-100~100): breathiness" << std::endl;
    std::cout << "  Mv=100(0~100): voicing" << std::endl;
    return 0;
  }
  std::cout << "args: " << absl::StrJoin(args, " ") << std::endl;

  auto resampler = std::make_unique<worldline::Resampler>(args);
  auto y = resampler->Resample();

  std::string out_path =
      args.size() >= 2 ? args[1]
                       : args[0].substr(0, args[0].size() - 4) + ".out.wav";
  std::cout << "write output to " << out_path << std::endl;
  wavwrite(y.data(), y.size(), 44100, 16, out_path.c_str());
}

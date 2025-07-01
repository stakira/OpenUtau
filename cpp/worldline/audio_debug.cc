#include <cstdint>

#include "absl/debugging/failure_signal_handler.h"
#include "absl/debugging/symbolize.h"
#include "absl/flags/flag.h"
#include "absl/flags/parse.h"
#include "absl/log/globals.h"
#include "absl/log/initialize.h"
#include "absl/log/log.h"
#include "absl/strings/escaping.h"
#include "absl/strings/str_cat.h"
#include "absl/strings/string_view.h"
#include "miniaudio.h"
#include "worldline/audio_output.h"
#include "xxhash.h"

int main(int argc, char** argv) {
  absl::InitializeSymbolizer(argv[0]);
  absl::FailureSignalHandlerOptions options;
  absl::InstallFailureSignalHandler(options);

  absl::InitializeLog();
  absl::SetStderrThreshold(absl::LogSeverity::kInfo);

  absl::ParseCommandLine(argc, argv);

  for (int i = 0; i < ma_backend_coreaudio; i++) {
    LOG(INFO) << "============";
    LOG(INFO) << "Trying backend: " << ma_get_backend_name((ma_backend)i);

    ma_context context;
    ma_backend backends[1] = {(ma_backend)i};
    ma_result result = ma_context_init(backends, 1, NULL, &context);

    if (result != MA_SUCCESS) {
      LOG(ERROR) << "Failed to initialize context";
      LOG(ERROR) << "Error: " << ma_result_description(result);
      continue;
    }

    ma_device_info* playback_device_infos;
    ma_uint32 playback_device_count;
    ma_device_info* capture_device_infos;
    ma_uint32 capture_device_count;

    result = ma_context_get_devices(
        &context, &playback_device_infos, &playback_device_count,
        &capture_device_infos, &capture_device_count);

    if (result != MA_SUCCESS) {
      LOG(ERROR) << "Failed to get devices";
      LOG(ERROR) << "Error: " << ma_result_description(result);
      ma_context_uninit(&context);
      continue;
    }
    LOG(INFO) << "Playback device count: " << playback_device_count;

    for (int j = 0; j < playback_device_count; j++) {
      LOG(INFO) << "------------";
      LOG(INFO) << "Device: #" << j;
      ma_device_info* info = &playback_device_infos[j];
      LOG(INFO) << "Device name: " << info->name;
      LOG(INFO) << "Device name bytes: "
                << absl::BytesToHexString(std::string_view(info->name));
      uint64_t id = XXH64(&(info->id), sizeof(ma_device_id), 0);
      LOG(INFO) << "Device ID: " << absl::Hex(id);
    }
  }

  LOG(INFO) << "============";
  LOG(INFO) << "Done";

  return 0;
}

#include "worldline/audio_output.h"

#include "absl/log/log.h"
#include "absl/time/clock.h"
#include "absl/time/time.h"
#include "gmock/gmock.h"
#include "gtest/gtest.h"
#include "xxhash.h"

static void white_noise(float* buffer, uint32_t channels,
                        uint32_t frame_count) {
  for (uint32_t i = 0; i < frame_count; i++) {
    for (uint32_t j = 0; j < channels; j++) {
      if (j == 0) {
        buffer[i * channels + j] = (float)rand() / RAND_MAX * 2.0f - 1.0f;
        buffer[i * channels + j] *= 0.1f;
      } else {
        buffer[i * channels + j] = buffer[i * channels + j - 1];
      }
    }
  }
}

TEST(MiniAudioTest, Playback) {
  uint32_t api_id = 0;
  uint64_t id = 0;

  ou_audio_device_info_t device_infos[10];
  int32_t count = ou_get_audio_device_infos(device_infos, 10);
  LOG(INFO) << "Device count: " << count;
  for (int32_t i = 0; i < count; i++) {
    LOG(INFO) << "Device " << i << ": " << device_infos[i].name
              << " API: " << device_infos[i].api
              << " API Id: " << device_infos[i].api_id << " Index: " << i
              << " ID: " << absl::Hex(device_infos[i].id);
    if (i == 0) {
      api_id = device_infos[i].api_id;
      id = device_infos[i].id;
    }
  }
  ou_free_audio_device_infos(device_infos, count);

  ou_audio_context_t* context = ou_init_audio_device(api_id, id, &white_noise);
  LOG(INFO) << "Device API: " << ou_get_audio_device_api(context);
  LOG(INFO) << "Device Name: " << ou_get_audio_device_name(context);
  ou_audio_device_start(context);
  absl::SleepFor(absl::Seconds(1));
  ou_audio_device_stop(context);
  ou_free_audio_device(context);

  absl::SleepFor(absl::Seconds(1));

  context = ou_init_audio_device_auto(&white_noise);
  LOG(INFO) << "Auto Device API: " << ou_get_audio_device_api(context);
  LOG(INFO) << "Auto Device Name: " << ou_get_audio_device_name(context);
  ou_audio_device_start(context);
  absl::SleepFor(absl::Seconds(1));
  ou_audio_device_stop(context);
  ou_free_audio_device(context);
}

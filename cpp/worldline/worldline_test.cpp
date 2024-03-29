#include "worldline.h"

#include "gmock/gmock.h"
#include "gtest/gtest.h"

#if defined(_MSC_VER)
#include <windows.h>
#else
#include <dlfcn.h>
#endif

#if defined(_MSC_VER)
#define DLL_IMPORT __declspec(dllimport)
#elif defined(__GNUC__)
#define DLL_IMPORT __attribute__((visibility("default")))
#endif

extern "C" {
DLL_API PhraseSynth* PhraseSynthNew();
DLL_API void PhraseSynthDelete(PhraseSynth* phrase_synth);
}

TEST(WorldlineTest, TestF0) {
#if defined(_MSC_VER)
  HMODULE handle = LoadLibrary("worldline.dll");
  EXPECT_THAT(handle, testing::NotNull());
#else
  dlopen("libworldline", RTLD_LAZY);
#endif

  PhraseSynth* phrase_synth = PhraseSynthNew();
  EXPECT_THAT(phrase_synth, testing::NotNull());
  PhraseSynthDelete(phrase_synth);
}

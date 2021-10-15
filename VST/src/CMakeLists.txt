dpf_add_plugin(OpenUTAU TARGETS vst3 lv2 MONOLITHIC FILES_DSP OpenUTAUPlugin.cpp)
target_include_directories(OpenUTAU PUBLIC include)
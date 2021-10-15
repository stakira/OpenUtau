dpf_add_plugin(OpenUTAU TARGETS vst3 lv2 FILES_DSP OpenUTAUPlugin.cpp FILES_UI OpenUTAUUI.cpp)
target_include_directories(OpenUTAU PUBLIC include)
target_link_libraries(OpenUTAU PUBLIC tiny-process-library)
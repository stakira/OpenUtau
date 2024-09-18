cmake_minimum_required(VERSION 3.24)

set(DPF_ROOT_DIR "${CMAKE_CURRENT_SOURCE_DIR}/deps/DPF")

include(./deps/dpf/cmake/DPF-plugin.cmake)
project(openutau_daw_plugin)

# MSVC only: Use UTF-8 code page
if(MSVC)
  add_compile_options("/utf-8")
endif()

# Main sources
file(GLOB_RECURSE dsp_sources "src/plugin_*.cpp")
file(GLOB_RECURSE ui_sources "src/ui_*.cpp")
file(GLOB_RECURSE common_sources "src/*.cpp")

# Dependencies
file(GLOB_RECURSE imgui_sources "deps/imgui/*.cpp")

if(CMAKE_BUILD_TYPE STREQUAL "Debug")
  add_definitions(-DDEBUG)
  add_definitions(-DDPF_DEBUG)
elseif(CMAKE_BUILD_TYPE STREQUAL "Release")

elseif(NOT CMAKE_BUILD_TYPE)
  message(FATAL_ERROR "Build type not set")
else()
  message(FATAL_ERROR "Unknown build type: ${CMAKE_BUILD_TYPE}")
endif()

# Windows 10
add_definitions(-D_WIN32_WINNT=0x0A00)

list(REMOVE_ITEM common_sources ${dsp_sources} ${ui_sources})

dpf_add_plugin(
  openutau_daw_plugin
  TARGETS
    vst3
  UI_TYPE
    opengl
  FILES_COMMON
    ${common_sources}
  FILES_DSP
    src/plugin.cpp
    ${dsp_sources}
  FILES_UI
    src/ui.cpp
    ${ui_sources}
    deps/dpf_widgets/opengl/DearImGui.cpp)

# Including zlib before DPF causes "find_library" to fail with infinite
# recursion, so we include it after DPF
set(ZLIB_BUILD_EXAMPLES OFF)
set(RENAME_ZCONF OFF)
add_subdirectory(deps/zlib)
target_compile_features(openutau_daw_plugin PUBLIC cxx_std_20)

target_include_directories(
  openutau_daw_plugin
  PUBLIC "src"
         "deps"
         "deps/DPF/dgl"
         "deps"
         "deps/asio/asio/include"
         "deps/dpf_widgets"
         "deps/uuid-v4"
         "deps/zlib"
         "deps/gzip-hpp/include"
         "deps/libfar/include")

target_link_libraries(openutau_daw_plugin PRIVATE zlibstatic)

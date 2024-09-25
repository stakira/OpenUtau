cmake_minimum_required(VERSION 3.24)

set(DPF_ROOT_DIR "${CMAKE_CURRENT_SOURCE_DIR}/deps/dpf")

if(CMAKE_BUILD_TYPE STREQUAL "Debug")
  set(PROJECT_NAME openutau_daw_plugin_debug)
elseif(CMAKE_BUILD_TYPE STREQUAL "Release")
  set(PROJECT_NAME openutau_daw_plugin)
elseif(NOT CMAKE_BUILD_TYPE)
  message(FATAL_ERROR "Build type not set")
else()
  message(FATAL_ERROR "Unknown build type: ${CMAKE_BUILD_TYPE}")
endif()

include(./deps/dpf/cmake/DPF-plugin.cmake)
project(${PROJECT_NAME})

# MSVC only: Use UTF-8 code page
if(MSVC)
  add_compile_options("/utf-8")
endif()

if(CMAKE_BUILD_TYPE STREQUAL "Debug")
  add_definitions(-DDEBUG)
  add_definitions(-DDPF_DEBUG)
elseif(CMAKE_BUILD_TYPE STREQUAL "Release")
  # nothing atm
endif()

# Windows 10
add_definitions(-D_WIN32_WINNT=0x0A00)

dpf_add_plugin(
  ${PROJECT_NAME}
  TARGETS
    vst3
  UI_TYPE
    opengl
  FILES_COMMON
    src/common.cpp
  FILES_DSP
    src/plugin.cpp
  FILES_UI
    src/ui.cpp
    deps/dpf_widgets/opengl/DearImGui.cpp)

# Including other libraries before DPF causes "find_library" to fail with infinite
# recursion, so we include it after DPF
set(ZLIB_BUILD_EXAMPLES OFF)
set(RENAME_ZCONF OFF)
add_subdirectory(deps/zlib)
target_compile_features(${PROJECT_NAME} PUBLIC cxx_std_20)

target_include_directories(
  ${PROJECT_NAME}
  PUBLIC "src"
         "deps"
         "deps/dpf/dgl"
         "deps/dpf/distrho"
         "deps/asio/asio/include"
         "deps/dpf_widgets"
         "deps/uuid-v4"
         "deps/zlib"
         "deps/yamc/include"
         "deps/gzip-hpp/include")

target_link_libraries(${PROJECT_NAME} PRIVATE zlibstatic)

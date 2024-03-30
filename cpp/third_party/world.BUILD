package(default_visibility = ["//visibility:public"])

cc_library(
    name = "world",
    srcs = glob(["src/*.cpp"]),
    hdrs = glob(["src/world/*.h"]),
    includes = ["src"],
)

cc_library(
    name = "audioio",
    srcs = ["tools/audioio.cpp"],
    hdrs = ["tools/audioio.h"],
    includes = ["tools"],
)

cc_library(
    name = "parameterio",
    srcs = ["tools/parameterio.cpp"],
    hdrs = ["tools/parameterio.h"],
    includes = ["tools"],
)

filegroup(
    name = "vaiueo2d_wav",
    srcs = ["test/vaiueo2d.wav"],
)

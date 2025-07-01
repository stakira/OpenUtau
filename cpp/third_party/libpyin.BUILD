package(default_visibility = ["//visibility:public"])

cc_library(
    name = "libpyin",
    srcs = glob(["*.c"]),
    hdrs = glob(["*.h"]),
    defines = ["FP_TYPE=double"],
    includes = ["."],
    deps = [
        "@libgvps",
    ],
)

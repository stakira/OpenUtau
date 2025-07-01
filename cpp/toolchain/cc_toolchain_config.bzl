"toolchain rule"

load("@bazel_tools//tools/build_defs/cc:action_names.bzl", "ACTION_NAMES")
load(
    "@bazel_tools//tools/cpp:cc_toolchain_config_lib.bzl",
    "feature",
    "flag_group",
    "flag_set",
    "tool_path",
)

all_link_actions = [
    ACTION_NAMES.cpp_link_executable,
    ACTION_NAMES.cpp_link_dynamic_library,
    ACTION_NAMES.cpp_link_nodeps_dynamic_library,
]

def _impl(ctx):
    tool_paths = [
        tool_path(
            name = "gcc",
            path = ctx.attr.tool_path_prefx + "gcc",
        ),
        tool_path(
            name = "ld",
            path = ctx.attr.tool_path_prefx + "ld",
        ),
        tool_path(
            name = "ar",
            path = ctx.attr.tool_path_prefx + "ar",
        ),
        tool_path(
            name = "cpp",
            path = ctx.attr.tool_path_prefx + "cpp",
        ),
        tool_path(
            name = "gcov",
            path = ctx.attr.tool_path_prefx + "gcov",
        ),
        tool_path(
            name = "nm",
            path = ctx.attr.tool_path_prefx + "nm",
        ),
        tool_path(
            name = "objdump",
            path = ctx.attr.tool_path_prefx + "objdump",
        ),
        tool_path(
            name = "strip",
            path = ctx.attr.tool_path_prefx + "strip",
        ),
    ]

    features = [
        feature(name = "supports_pic", enabled = True),
        feature(
            name = "default_linker_flags",
            enabled = True,
            flag_sets = [
                flag_set(
                    actions = all_link_actions,
                    flag_groups = ([
                        flag_group(
                            flags = [
                                "-lstdc++",
                            ],
                        ),
                    ]),
                ),
            ],
        ),
        feature(
            name = "opt",
            flag_sets = [
                flag_set(
                    actions = [ACTION_NAMES.c_compile, ACTION_NAMES.cpp_compile],
                    flag_groups = [
                        flag_group(
                            flags = [
                                "-g0",
                                "-O2",
                                "-DNDEBUG",
                                "-ffunction-sections",
                                "-fdata-sections",
                            ],
                        ),
                    ],
                ),
                flag_set(
                    actions = [
                        ACTION_NAMES.cpp_link_dynamic_library,
                        ACTION_NAMES.cpp_link_nodeps_dynamic_library,
                        ACTION_NAMES.cpp_link_executable,
                    ],
                    flag_groups = [flag_group(flags = ["-Wl,--gc-sections"])],
                ),
            ],
        ),
    ]

    return cc_common.create_cc_toolchain_config_info(
        ctx = ctx,
        features = features,
        cxx_builtin_include_directories = ctx.attr.cxx_builtin_include_directories,
        toolchain_identifier = "local",
        host_system_name = "local",
        target_system_name = "local",
        target_cpu = ctx.attr.target_cpu,
        target_libc = "unknown",
        compiler = "gcc",
        abi_version = "unknown",
        abi_libc_version = "unknown",
        tool_paths = tool_paths,
    )

cc_toolchain_config = rule(
    implementation = _impl,
    attrs = {
        "cxx_builtin_include_directories": attr.string_list(),
        "target_cpu": attr.string(),
        "tool_path_prefx": attr.string(),
    },
    provides = [CcToolchainConfigInfo],
)

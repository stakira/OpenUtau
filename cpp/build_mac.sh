#!/bin/bash

build()
{
    bazel build //worldline:worldline -c opt $2
    chmod +w bazel-bin/worldline/libworldline.dylib
    cp bazel-bin/worldline/libworldline.dylib ../runtimes/osx/native/libworldline-$1.dylib
}

mkdir -p ../runtimes/osx/native

build x64 "--cpu=darwin_x86_64"
build arm64 "--cpu=darwin_arm64"

lipo -create ../runtimes/osx/native/libworldline-x64.dylib ../runtimes/osx/native/libworldline-arm64.dylib -output ../runtimes/osx/native/libworldline.dylib
rm ../runtimes/osx/native/libworldline-x64.dylib ../runtimes/osx/native/libworldline-arm64.dylib

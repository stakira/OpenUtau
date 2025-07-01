@goto :MAIN

:BUILD
@echo Building %~1

if not exist ..\runtimes\%~1\native mkdir ..\runtimes\%~1\native
bazel build //worldline:worldline -c opt --cpu=%~2
attrib -r bazel-bin\worldline\worldline.dll
copy bazel-bin\worldline\worldline.dll ..\runtimes\%~1\native

@EXIT /B

:MAIN
@call :BUILD win-x64 x64_windows
@call :BUILD win-x86 x64_x86_windows
@call :BUILD win-arm64 arm64_windows

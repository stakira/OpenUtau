using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using OpenUtau.Core.Util;
using Vortice.DXGI;
using Serilog;

namespace OpenUtau.Core {
    public class GpuInfo {
        public int deviceId;
        public string description = "";

        override public string ToString() {
            return $"[{deviceId}] {description}";
        }
    }

    public class Onnx {
        static Onnx() {
            ConfigureOnnxRuntimeLibrary();
        }

        private static bool cudaAvailable = OS.IsLinux() && CudaGpuDetector.IsCudaAvailable() && CudaGpuDetector.IsCuDnnAvailable();

    private static void ConfigureOnnxRuntimeLibrary() {
        if (OS.IsLinux()) {
            string runner = Preferences.Default.OnnxRunner ?? "CPU";
            string basePath = AppContext.BaseDirectory;
            string soPath;

            if (runner == "CUDA" && cudaAvailable) {
                soPath = Path.Combine(basePath, "runtimes", "linux-x64", "native", "CUDA", "libonnxruntime.so");
            } else {
                soPath = Path.Combine(basePath, "runtimes", "linux-x64", "native", "CPU", "libonnxruntime.so");
            }

            // Register resolver for the ONNX Runtime managed assembly
            NativeLibrary.SetDllImportResolver(
                typeof(InferenceSession).Assembly,
                (libraryName, assembly, searchPath) => {
                    if (libraryName == "onnxruntime") {
                        if (NativeLibrary.TryLoad(soPath, out IntPtr handle)) {
                            return handle;
                        }
                        throw new DllNotFoundException($"Could not load ONNX Runtime library from {soPath}");
                    }
                    return IntPtr.Zero;
                }
            );

            Log.Debug($"ONNX Runtime library set to: {soPath}");
        }
    }


        public static List<string> getRunnerOptions() {
            if (OS.IsWindows()) {
                return new List<string> {
                "CPU",
                "DirectML"
                };
            } else if (OS.IsMacOS()) {
                return new List<string> {
                "CPU",
                "CoreML"
                };
            } else if (cudaAvailable) {
                return new List<string> {
                "CPU",
                "CUDA"
                };
            }
            return new List<string> {
                "CPU"        
            };
        }

        public static List<GpuInfo> getGpuInfo() {
            List<GpuInfo> gpuList = new List<GpuInfo>();
            if (OS.IsWindows()) {
                DXGI.CreateDXGIFactory1(out IDXGIFactory1 factory);
                for(int deviceId = 0; deviceId < 32; deviceId++) {
                    factory.EnumAdapters1(deviceId, out IDXGIAdapter1 adapterOut);
                    if(adapterOut is null) {
                        break;
                    }
                    gpuList.Add(new GpuInfo {
                        deviceId = deviceId,
                        description = adapterOut.Description.Description
                    }) ;
                }
            } else if (cudaAvailable) {
                gpuList.AddRange(CudaGpuDetector.GetCudaDevices());
            }

            if (gpuList.Count == 0) {
                gpuList.Add(new GpuInfo {
                    deviceId = 0,
                });
            }
            return gpuList;
        }

        private static SessionOptions getOnnxSessionOptions(){
            SessionOptions options = new SessionOptions();
            List<string> runnerOptions = getRunnerOptions();
            string runner = Preferences.Default.OnnxRunner;
            if (String.IsNullOrEmpty(runner)) {
                runner = runnerOptions[0];
            }
            if (!runnerOptions.Contains(runner)) {
                runner = "CPU";
            }
            switch(runner){
                case "DirectML":
                    options.AppendExecutionProvider_DML(Preferences.Default.OnnxGpu);
                    break;
                case "CoreML":
                    options.AppendExecutionProvider_CoreML(CoreMLFlags.COREML_FLAG_ENABLE_ON_SUBGRAPH);
                    break;
                case "CUDA":
                    options.AppendExecutionProvider_CUDA(Preferences.Default.OnnxGpu);
                    break;
            }
            return options;
        }

        public static InferenceSession getInferenceSession(byte[] model, bool force_cpu = false) {
            if (force_cpu) {
                return new InferenceSession(model);
            } else {
                return new InferenceSession(model, getOnnxSessionOptions());
            }
        }

        public static InferenceSession getInferenceSession(string modelPath, bool force_cpu = false) {
            if (force_cpu) {
                return new InferenceSession(modelPath);
            } else {
                return new InferenceSession(modelPath, getOnnxSessionOptions());
            }
        }

        public static void VerifyInputNames(InferenceSession session, IEnumerable<NamedOnnxValue> inputs) {
            var sessionInputNames = session.InputNames.ToHashSet();
            var givenInputNames = inputs.Select(v => v.Name).ToHashSet();
            var missing = sessionInputNames
                .Except(givenInputNames)
                .OrderBy(s => s, StringComparer.InvariantCulture)
                .ToArray();
            if (missing.Length > 0) {
                throw new ArgumentException("Missing input(s) for the inference session: " + string.Join(", ", missing));
            }
            var unexpected = givenInputNames
                .Except(sessionInputNames)
                .OrderBy(s => s, StringComparer.InvariantCulture)
                .ToArray();
            if (unexpected.Length > 0) {
                throw new ArgumentException("Unexpected input(s) for the inference session: " + string.Join(", ", unexpected));
            }
        }
    }
}

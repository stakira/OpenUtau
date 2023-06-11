using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using OpenUtau.Core.Util;
using Vortice.DXGI;

namespace OpenUtau.Core {
    public class GpuInfo {
        public int deviceId;
        public string description = "";

        override public string ToString() {
            return $"[{deviceId}] {description}";
        }
    }

    public class Onnx {
        public static List<string> getRunnerOptions() {
            if (OS.IsWindows()) {
                return new List<string> {
                "cpu",
                "directml"
                };
            } else if (OS.IsMacOS()) {
                return new List<string> {
                "cpu",
                "coreml"
                };
            }
            return new List<string> {
                "cpu"
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
            }
            if (gpuList.Count == 0) {
                gpuList.Add(new GpuInfo {
                    deviceId = 0,
                });
            }
            return gpuList;
        }

        public static InferenceSession getInferenceSession(byte[] model) {
            SessionOptions options = new SessionOptions();
            List<string> runnerOptions = getRunnerOptions();
            string runner = Preferences.Default.OnnxRunner;
            if (String.IsNullOrEmpty(runner)) {
                runner = runnerOptions[0];
            }
            if (!(runnerOptions.Contains(runner))) {
                runner = "cpu";
            }
            switch(runner){
                case "directml":
                    options.AppendExecutionProvider_DML(Preferences.Default.OnnxGpu);
                    break;
                case "coreml":
                    options.AppendExecutionProvider_CoreML(CoreMLFlags.COREML_FLAG_ENABLE_ON_SUBGRAPH);
                    break;
            }
            return new InferenceSession(model,options);
        }
    }
}

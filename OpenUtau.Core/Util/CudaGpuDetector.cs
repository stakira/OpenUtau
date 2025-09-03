using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenUtau.Core.Util {
    public static class CudaGpuDetector {
        private const string CudaLib = "libcuda.so";

        // CUDA driver API
        [DllImport(CudaLib, EntryPoint = "cuInit")]
        private static extern int cuInit(uint flags);

        [DllImport(CudaLib, EntryPoint = "cuDriverGetVersion")]
        private static extern int cuDriverGetVersion(out int driverVersion);

        [DllImport(CudaLib, EntryPoint = "cuDeviceGetCount")]
        private static extern int cuDeviceGetCount(out int count);

        [DllImport(CudaLib, EntryPoint = "cuDeviceGetName")]
        private static extern int cuDeviceGetName(byte[] name, int len, int dev);

        // cuDNN
        [DllImport("libcudnn.so", EntryPoint = "cudnnGetVersion", SetLastError = true)]
        private static extern long cudnnGetVersion();

        public static bool IsCudaAvailable() {
            try {
                int res = cuInit(0);
                Console.Error.WriteLine($"[CUDA DETECTOR] cuInit -> {res}");
                if (res != 0) return false;

                res = cuDriverGetVersion(out int version);
                Console.Error.WriteLine($"[CUDA DETECTOR] cuDriverGetVersion -> {res}, version={version}");
                if (res != 0) return false;

                int major = version / 1000;
                int minor = (version % 1000) / 10;
                Console.Error.WriteLine($"[CUDA DETECTOR] Detected CUDA driver version {major}.{minor}");

                return major >= 12;
            } catch (DllNotFoundException ex) {
                Console.Error.WriteLine($"[CUDA DETECTOR] libcuda.so not found: {ex.Message}");
                return false;
            } catch (Exception ex) {
                Console.Error.WriteLine($"[CUDA DETECTOR] Exception in IsCudaAvailable: {ex}");
                return false;
            }
        }

        public static bool IsCuDnnAvailable() {
            try {
                long version = cudnnGetVersion();
                int major = (int)(version / 1000);
                int minor = (int)((version % 1000) / 100);
                Console.Error.WriteLine($"[CUDA DETECTOR] cuDNN version {major}.{minor} (raw {version})");

                return major >= 9;
            } catch (DllNotFoundException ex) {
                Console.Error.WriteLine($"[CUDA DETECTOR] libcudnn.so not found: {ex.Message}");
                return false;
            } catch (Exception ex) {
                Console.Error.WriteLine($"[CUDA DETECTOR] Exception in IsCuDnnAvailable: {ex}");
                return false;
            }
        }

        public static List<GpuInfo> GetCudaDevices() {
            var list = new List<GpuInfo>();
            try {
                int res = cuDeviceGetCount(out int count);
                Console.Error.WriteLine($"[CUDA DETECTOR] cuDeviceGetCount -> {res}, count={count}");
                if (res != 0) return list;

                for (int i = 0; i < count; i++) {
                    var nameBytes = new byte[256];
                    res = cuDeviceGetName(nameBytes, nameBytes.Length, i);
                    Console.Error.WriteLine($"[CUDA DETECTOR] cuDeviceGetName(dev={i}) -> {res}");
                    if (res == 0) {
                        string name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');
                        Console.Error.WriteLine($"[CUDA DETECTOR] Device {i}: {name}");
                        list.Add(new GpuInfo {
                            deviceId = i,
                            description = name
                        });
                    }
                }
            } catch (Exception ex) {
                Console.Error.WriteLine($"[CUDA DETECTOR] Exception in GetCudaDevices: {ex}");
            }
            return list;
        }
    }
}


using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace OpenUtau.Core.Util {
    public static class CudaGpuDetector {
        public static List<GpuInfo> GetCudaDevices() {
            var list = new List<GpuInfo>();

            try {
                var psi = new ProcessStartInfo {
                    FileName = "nvidia-smi",
                    Arguments = "--query-gpu=index,name --format=csv,noheader",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(psi)) {
                    if (proc == null) {
                        Console.Error.WriteLine("[CUDA DETECTOR] Failed to start nvidia-smi process.");
                        return list;
                    }

                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();

                    foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries)) {
                        var parts = line.Split(',', 2);
                        if (parts.Length != 2) continue;

                        if (int.TryParse(parts[0].Trim(), out int id)) {
                            list.Add(new GpuInfo {
                                deviceId = id,
                                description = parts[1].Trim()
                            });
                        }
                    }

                    Console.Error.WriteLine($"[CUDA DETECTOR] Found {list.Count} CUDA device(s).");
                }
            } catch (Exception ex) {
                Console.Error.WriteLine($"[CUDA DETECTOR] Exception: {ex}");
            }

            return list;
        }
    }
}



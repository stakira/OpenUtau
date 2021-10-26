using System.Collections.Generic;
using System.IO;
using OpenUtau.Core.ResamplerDriver.Factorys;
using Serilog;

namespace OpenUtau.Core.ResamplerDriver {
    public interface IResamplerDriver {
        string FilePath { get; }
        byte[] DoResampler(DriverModels.EngineInput Args, ILogger logger);
        DriverModels.EngineInfo GetInfo();
    }

    public class ResamplerDriver {
        public static IResamplerDriver Load(string filePath) {
            string ext = Path.GetExtension(filePath).ToLower();
            if (!File.Exists(filePath)) {
                return null;
            }
            if (OS.IsWindows() && ext == ".exe" ||
                !OS.IsWindows() && ext == ".sh") {
                return new ExeDriver(filePath);
            }
            if (ext == ".dll") {
                SharpDriver csDriver = new SharpDriver(filePath);
                if (csDriver.isLegalPlugin) {
                    return csDriver;
                }
            }
            if (OS.IsWindows() && ext == ".dll" ||
                OS.IsMacOS() && ext == ".dylib" ||
                OS.IsLinux() && ext == ".so") {
                CppDriver cppDriver = new CppDriver(filePath);
                if (cppDriver.isLegalPlugin) {
                    return cppDriver;
                }
            }
            return null;
        }

        public static List<IResamplerDriver> Search(string path) {
            var resamplers = new List<IResamplerDriver>();
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }
            var files = Directory.EnumerateFiles(path, "*.*", new EnumerationOptions() {
                RecurseSubdirectories = true
            });
            foreach (var file in files) {
                var driver = Load(file);
                if (driver != null) {
                    resamplers.Add(driver);
                }
            }
            return resamplers;
        }
    }
}

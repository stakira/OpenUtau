using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUtau.Core.ResamplerDriver.Factorys;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Core.ResamplerDriver {
    public interface IResamplerDriver {
        string Name { get; }
        string FilePath { get; }
        byte[] DoResampler(DriverModels.EngineInput Args, ILogger logger);
        void CheckPermissions();
    }

    public class ResamplerDrivers {
        private readonly static object lockObj = new object();
        private static Dictionary<string, IResamplerDriver> Resamplers;

        public static IResamplerDriver Load(string filePath, string basePath) {
            if (!File.Exists(filePath)) {
                return null;
            }
            string ext = Path.GetExtension(filePath).ToLower();
            if (OS.IsWindows()) {
                if (ext == ".exe" || ext == ".bat") {
                    return new ExeDriver(filePath, basePath);
                }
            } else {
                if (ext == ".sh" || string.IsNullOrEmpty(ext)) {
                    return new ExeDriver(filePath, basePath);
                }
            }
            if (ext == ".dll") {
                SharpDriver csDriver = new SharpDriver(filePath, basePath);
                if (csDriver.isLegalPlugin) {
                    return csDriver;
                }
            }
            if (OS.IsWindows() && ext == ".dll" ||
                OS.IsMacOS() && ext == ".dylib" ||
                OS.IsLinux() && ext == ".so") {
                CppDriver cppDriver = new CppDriver(filePath, basePath);
                if (cppDriver.isLegalPlugin) {
                    return cppDriver;
                }
            }
            return null;
        }

        public static void Search() {
            var resamplers = new Dictionary<string, IResamplerDriver>();
            string basePath = PathManager.Inst.LibsPath;
            string name = GetDefaultResamplerName();
            var driver = Load(Path.Combine(basePath, name), basePath);
            if (driver != null) {
                resamplers.Add(driver.Name, driver);
                if (string.IsNullOrEmpty(Preferences.Default.ExternalPreviewEngine)) {
                    Preferences.Default.ExternalPreviewEngine = driver.Name;
                    Preferences.Save();
                }
                if (string.IsNullOrEmpty(Preferences.Default.ExternalExportEngine)) {
                    Preferences.Default.ExternalExportEngine = driver.Name;
                    Preferences.Save();
                }
            }
            basePath = PathManager.Inst.ResamplersPath;
            Directory.CreateDirectory(basePath);
            foreach (var file in Directory.EnumerateFiles(basePath, "*", new EnumerationOptions() {
                RecurseSubdirectories = true
            })) {
                driver = Load(file, basePath);
                if (driver != null) {
                    resamplers.Add(driver.Name, driver);
                }
            }
            lock (lockObj) {
                Resamplers = resamplers;
            }
        }

        public static string GetDefaultResamplerName() {
            if (!OS.IsWindows()) {
                return "worldline64";
            }
            if (Environment.Is64BitProcess) {
                return "worldline64.exe";
            }
            return "worldline32.exe";
        }

        public static List<IResamplerDriver> GetResamplers() {
            lock (lockObj) {
                return Resamplers.Values.ToList();
            }
        }

        public static IResamplerDriver GetResampler(string name) {
            lock (lockObj) {
                if (Resamplers.TryGetValue(name, out var driver)) {
                    return driver;
                }
            }
            return null;
        }

        public static bool CheckPreviewResampler() {
            Search();
            if (Resamplers.Count == 0) {
                return false;
            }
            if (Resamplers.TryGetValue(
                Preferences.Default.ExternalPreviewEngine, out var resampler)
                && File.Exists(resampler.FilePath)) {
                return true;
            }
            return false;
        }
    }
}

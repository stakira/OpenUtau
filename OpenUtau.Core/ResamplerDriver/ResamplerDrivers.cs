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
        float[] DoResampler(DriverModels.EngineInput args, ILogger logger);
        string DoResamplerReturnsFile(DriverModels.EngineInput args, ILogger logger);
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
            return null;
        }

        public static void Search() {
            var resamplers = new Dictionary<string, IResamplerDriver>();
            IResamplerDriver defaultDriver = new WorldlineDriver();
            resamplers.Add(defaultDriver.Name, defaultDriver);
            string basePath = PathManager.Inst.ResamplersPath;
            try {
                Directory.CreateDirectory(basePath);
                foreach (var file in Directory.EnumerateFiles(basePath, "*", new EnumerationOptions() {
                    RecurseSubdirectories = true
                })) {
                    var driver = Load(file, basePath);
                    if (driver != null) {
                        resamplers.Add(driver.Name, driver);
                    }
                }
                lock (lockObj) {
                    Resamplers = resamplers;
                }
            } catch (Exception e) {
                Log.Error(e, "Failed to search resamplers.");
                Resamplers = new Dictionary<string, IResamplerDriver>();
            }
            if (string.IsNullOrEmpty(Preferences.Default.Resampler) ||
                !Resamplers.TryGetValue(Preferences.Default.Resampler, out var _)) {
                Preferences.Default.Resampler = defaultDriver.Name;
                Preferences.Save();
            }
        }

        public static string GetDefaultResamplerName() => "worldline";

        public static List<IResamplerDriver> GetResamplers() {
            lock (lockObj) {
                return Resamplers.Values.ToList();
            }
        }

        public static IResamplerDriver GetResampler(string name) {
            if (name.StartsWith("worldline")) {
                name = "worldline";
            }
            lock (lockObj) {
                if (Resamplers.TryGetValue(name, out var driver)) {
                    return driver;
                }
            }
            return null;
        }

        public static bool CheckResampler() {
            Search();
            if (Resamplers.Count == 0) {
                return false;
            }
            if (Resamplers.TryGetValue(Preferences.Default.Resampler, out var _)) {
                return true;
            }
            return false;
        }
    }
}

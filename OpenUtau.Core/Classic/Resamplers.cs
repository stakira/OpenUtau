using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUtau.Core;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Classic {
    public class Resamplers {
        private readonly static object lockObj = new object();
        private static Dictionary<string, IResampler> resamplers;

        public static IResampler Load(string filePath, string basePath) {
            if (!File.Exists(filePath)) {
                return null;
            }
            string ext = Path.GetExtension(filePath).ToLower();
            if (OS.IsWindows()) {
                if (ext == ".exe" || ext == ".bat") {
                    return new ExeResampler(filePath, basePath);
                }
            } else {
                if (ext == ".sh" || string.IsNullOrEmpty(ext)) {
                    return new ExeResampler(filePath, basePath);
                }
            }
            return null;
        }

        public static void Search() {
            var resamplers = new Dictionary<string, IResampler>();
            resamplers.Add(WorldlineResampler.name, new WorldlineResampler());
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
                    Resamplers.resamplers = resamplers;
                }
            } catch (Exception e) {
                Log.Error(e, "Failed to search resamplers.");
                Resamplers.resamplers = new Dictionary<string, IResampler>();
            }
            if (string.IsNullOrEmpty(Preferences.Default.Resampler) ||
                !Resamplers.resamplers.TryGetValue(Preferences.Default.Resampler, out var _)) {
                Preferences.Default.Resampler = WorldlineResampler.name;
                Preferences.Save();
            }
        }

        public static List<IResampler> GetResamplers() {
            lock (lockObj) {
                return resamplers.Values.ToList();
            }
        }

        public static IResampler GetResampler(string name) {
            if (string.IsNullOrEmpty(name) || name.StartsWith(WorldlineResampler.name)) {
                name = WorldlineResampler.name;
            }
            lock (lockObj) {
                if (resamplers.TryGetValue(name, out var resampler)) {
                    return resampler;
                } else {
                    return resamplers[WorldlineResampler.name];
                }
            }
        }

        public static bool CheckResampler() {
            Search();
            if (resamplers.Count == 0) {
                return false;
            }
            if (resamplers.TryGetValue(Preferences.Default.Resampler, out var _)) {
                return true;
            }
            return false;
        }
    }
}

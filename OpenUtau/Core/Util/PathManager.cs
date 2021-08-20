using System;
using System.IO;
using System.Reflection;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Core {

    public class PathManager {
        public const string UtauVoicePath = "%VOICE%";
        public const string DefaultSingerPath = "Singers";
        public const string DefaultCachePath = "Cache";
        public const string kExportPath = "Export";

        private static PathManager _inst;

        public PathManager() {
            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            Log.Logger.Information($"Assembly path = {assemblyPath}");
            HomePath = Directory.GetParent(assemblyPath).ToString();
            Log.Logger.Information($"Home path = {HomePath}");
        }

        public static PathManager Inst { get { if (_inst == null) { _inst = new PathManager(); } return _inst; } }
        public string HomePath { get; private set; }
        public string InstalledSingersPath => Path.Combine(HomePath, "Content", "Singers");

        public string TryMakeRelative(string path) {
            if (path.StartsWith(HomePath, StringComparison.Ordinal)) {
                path = path.Replace(HomePath, "");
                return path.TrimStart(Path.DirectorySeparatorChar);
            }
            return path;
        }

        public string GetSingerAbsPath(string path) {
            path = path.Replace(UtauVoicePath, "");
            foreach (var searchPath in Preferences.GetSingerSearchPaths()) {
                if (!Directory.Exists(searchPath)) {
                    continue;
                }
                var absPath = Path.Combine(searchPath, path);
                if (Directory.Exists(absPath)) {
                    return absPath;
                }
            }
            if (Directory.Exists(path)) {
                return path;
            }

            return string.Empty;
        }

        public void AddSingerSearchPath(string path) {
            path = TryMakeRelative(path);
            var paths = Preferences.GetSingerSearchPaths();
            if (!paths.Contains(path)) {
                paths.Add(path);
                Preferences.SetSingerSearchPaths(paths);
            }
        }

        public void RemoveSingerSearchPath(string path) {
            if (path == DefaultSingerPath) {
                return;
            }

            var paths = Preferences.GetSingerSearchPaths();
            if (paths.Contains(path)) {
                paths.Remove(path);
                Preferences.SetSingerSearchPaths(paths);
            }
        }

        public string GetCachePath(string projectPath) {
            string cachepath;
            if (string.IsNullOrEmpty(projectPath)) {
                cachepath = Path.Combine(HomePath, DefaultCachePath);
            } else {
                cachepath = Path.Combine(Path.GetDirectoryName(projectPath), DefaultCachePath);
            }

            if (!Directory.Exists(cachepath)) {
                Directory.CreateDirectory(cachepath);
            }

            return cachepath;
        }

        public string GetExportPath(string filepath, int trackNo) {
            var dir = Path.Combine(Path.GetDirectoryName(filepath), kExportPath);
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"{trackNo:D2}.wav");
        }

        public string GetEngineSearchPath() {
            return Path.Combine(HomePath, "Resamplers");
        }

        public string GetPreviewEnginePath() {
            return Path.Combine(GetEngineSearchPath(), Util.Preferences.Default.ExternalPreviewEngine);
        }

        public string GetExportEnginePath() {
            return Path.Combine(GetEngineSearchPath(), Util.Preferences.Default.ExternalExportEngine);
        }
    }
}

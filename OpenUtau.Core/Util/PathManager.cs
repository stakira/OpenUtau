using System;
using System.Globalization;
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
        public const string kPluginPath = "Plugins";

        private static PathManager _inst;

        public PathManager() {
            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            Log.Logger.Information($"Assembly path = {assemblyPath}");
            HomePath = Directory.GetParent(assemblyPath).ToString();
            HomePathIsAscii = true;
            var etor = StringInfo.GetTextElementEnumerator(HomePath);
            while (etor.MoveNext()) {
                string s = etor.GetTextElement();
                if (s.Length != 1 || s[0] >= 128) {
                    HomePathIsAscii = false;
                    break;
                }
            }
            Log.Logger.Information($"Home path = {HomePath}");
        }

        public static PathManager Inst { get { if (_inst == null) { _inst = new PathManager(); } return _inst; } }
        public string HomePath { get; private set; }
        public bool HomePathIsAscii { get; private set; }
        public string InstalledSingersPath => Path.Combine(HomePath, "Content", "Singers");
        public string PluginsPath => Path.Combine(HomePath, kPluginPath);

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

        public string GetPartSavePath(string projectPath, int partNo) {
            var dir = Path.GetDirectoryName(projectPath);
            var filename = Path.GetFileNameWithoutExtension(projectPath);
            return Path.Combine(dir, $"{filename}-{partNo:D2}.ust");
        }

        public string GetExportPath(string projectPath, int trackNo) {
            var dir = Path.Combine(Path.GetDirectoryName(projectPath), kExportPath);
            Directory.CreateDirectory(dir);
            var filename = Path.GetFileNameWithoutExtension(projectPath);
            return Path.Combine(dir, $"{filename}-{trackNo:D2}.wav");
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

namespace OpenUtau {
    public static class PathUtils {
        public static string MakeRelative(string path, string basePath) {
            if (path.StartsWith(basePath)) {
                return path.Replace(basePath, "")
                    .Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            return path;
        }
    }
}

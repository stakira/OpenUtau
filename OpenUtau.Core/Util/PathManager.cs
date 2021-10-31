using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Core {

    public class PathManager {
        private static PathManager _inst;

        public PathManager() {
            if (OS.IsMacOS()) {
                HomePath = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.Personal), "Library", "OpenUtau");
                HomePathIsAscii = true;
            } else {
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
            }
            Log.Logger.Information($"Home path = {HomePath}");
        }

        public static PathManager Inst { get { if (_inst == null) { _inst = new PathManager(); } return _inst; } }
        public string HomePath { get; private set; }
        public bool HomePathIsAscii { get; private set; }
        public string SingersPathOld => Path.Combine(HomePath, "Content", "Singers");
        public string SingersPath => Path.Combine(HomePath, "Singers");
        public string PluginsPath => Path.Combine(HomePath, "Plugins");
        public string TemplatesPath => Path.Combine(HomePath, "Templates");
        public string LogFilePath => Path.Combine(HomePath, "Logs", "log.txt");
        public string PrefsFilePath => Path.Combine(HomePath, "prefs.json");

        public string GetCachePath() {
            string cachepath = Path.Combine(HomePath, "Cache");
            Directory.CreateDirectory(cachepath);
            return cachepath;
        }

        public string GetPartSavePath(string projectPath, int partNo) {
            var dir = Path.GetDirectoryName(projectPath);
            var filename = Path.GetFileNameWithoutExtension(projectPath);
            return Path.Combine(dir, $"{filename}-{partNo:D2}.ust");
        }

        public string GetExportPath(string projectPath, int trackNo) {
            var dir = Path.Combine(Path.GetDirectoryName(projectPath), "Export");
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

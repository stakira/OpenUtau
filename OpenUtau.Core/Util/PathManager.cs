using System;
using System.Diagnostics;
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
            } else if (OS.IsLinux()) {
                HomePath = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.Personal), "OpenUtau");
                HomePathIsAscii = true;
            } else {
                HomePath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
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
        public string AdditionalSingersPath => Preferences.Default.AdditionalSingerPath;
        public string PluginsPath => Path.Combine(HomePath, "Plugins");
        public string TemplatesPath => Path.Combine(HomePath, "Templates");
        public string LogFilePath => Path.Combine(HomePath, "Logs", "log.txt");
        public string PrefsFilePath => Path.Combine(HomePath, "prefs.json");
        public string CachePath => Path.Combine(HomePath, "Cache");

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

        public string ResamplersPath => Path.Combine(HomePath, "Resamplers");

        public string LibsPath {
            get {
                var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                path = Path.Combine(path, "libs");
                if (OS.IsWindows()) {
                    path = Path.Combine(path, Environment.Is64BitProcess ? "win-x64" : "win-x86");
                } else if (OS.IsMacOS()) {
                    path = Path.Combine(path, "osx-x64");
                } else if (OS.IsLinux()) {
                    path = Path.Combine(path, "linux-x64");
                }
                return path;
            }
        }
    }
}

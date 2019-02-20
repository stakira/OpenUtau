using System;
using System.IO;
using System.Linq;

namespace OpenUtau.Core
{
    public class PathManager
    {
        public const string UtauVoicePath = "%VOICE%";
        public const string DefaultSingerPath = "Singers";
        public const string DefaultCachePath = "UCache";

        public readonly string HomePath = AppContext.BaseDirectory;

        static PathManager _inst;
        public static PathManager Inst { get { if (_inst == null) { _inst = new PathManager(); } return _inst; } }

        public string TryMakeRelative(string path)
        {
            if (path.StartsWith(HomePath, StringComparison.Ordinal))
            {
                path = path.Replace(HomePath, "");
                return path.TrimStart(Path.DirectorySeparatorChar);
            }
            return path;
        }

        public string GetSingerAbsPath(string path)
        {
            var singerSearchPaths = GetSingerSearchPaths();
            path = path.Replace(UtauVoicePath, "");
            foreach (string searchPath in singerSearchPaths)
            {
                if (!Directory.Exists(searchPath)) continue;
                string absPath = Path.Combine(searchPath, path);
                if (Directory.Exists(absPath)) return absPath;
            }
            if (Directory.Exists(path)) return path;
            return string.Empty;
        }

        public string[] GetSingerSearchPaths()
        {
            string[] paths = Util.Preferences.Default.SingerSearchPaths;
            return paths;
        }

        public void SetSingerSearchPaths(string[] paths)
        {
            Util.Preferences.Default.SingerSearchPaths = paths;
        }

        public void AddSingerSearchPath(string path)
        {
            path = TryMakeRelative(path);
            var paths = GetSingerSearchPaths().ToList();
            if (!paths.Contains(path))
            {
                paths.Add(path);
                SetSingerSearchPaths(paths.ToArray());
                Util.Preferences.Save();
            }
        }

        public void RemoveSingerSearchPath(string path)
        {
            if (path == DefaultSingerPath) return;
            var paths = GetSingerSearchPaths().ToList();
            if (paths.Contains(path))
            {
                paths.Remove(path);
                SetSingerSearchPaths(paths.ToArray());
                Util.Preferences.Save();
            }
        }

        public string GetCachePath(string filepath)
        {
            string cachepath;
            if (filepath == string.Empty) cachepath = Path.Combine(HomePath, DefaultCachePath);
            else cachepath = Path.Combine(Path.GetDirectoryName(filepath), DefaultCachePath);
            if (!Directory.Exists(cachepath)) Directory.CreateDirectory(cachepath);
            return cachepath;
        }

        public string GetEngineSearchPath()
        {
            return Path.Combine(HomePath, "Resamplers");
        }

        public string GetPreviewEnginePath()
        {
            if (Util.Preferences.Default.InternalEnginePreview) return "TnFndsOU.dll";
            else return Path.Combine(GetEngineSearchPath(), Util.Preferences.Default.ExternalPreviewEngine);
        }

        public string GetExportEnginePath()
        {
            if (Util.Preferences.Default.InternalEngineExport) return "TnFndsOU.dll";
            else return Path.Combine(GetEngineSearchPath(), Util.Preferences.Default.ExternalExportEngine);
        }
    }
}
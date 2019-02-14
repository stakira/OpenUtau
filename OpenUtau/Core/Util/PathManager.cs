using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace OpenUtau.Core
{
    public class PathManager
    {
        public const string UtauVoicePath = "%VOICE%";
        public const string DefaultSingerPath = "Singers";
        public const string DefaultCachePath = "UCache";

        private string _homePath;
        public string HomePath { get { return _homePath; } }
        
        private PathManager()
        {
            _homePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private static PathManager _inst;
        public static PathManager Inst { get { if (_inst == null) { _inst = new PathManager(); } return _inst; } }
        
        public string MakeRelativeToHome(string path)
        {
            if (path.StartsWith(_homePath)) path = path.Replace(_homePath, "");
            while (path.StartsWith(Path.DirectorySeparatorChar.ToString())) path = path.Substring(1);
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
            string[] paths = Core.Util.Preferences.Default.SingerSearchPaths;
            return paths;
        }

        public void SetSingerSearchPaths(string[] paths)
        {
            Core.Util.Preferences.Default.SingerSearchPaths = paths;
        }

        public void AddSingerSearchPath(string path)
        {
            path = MakeRelativeToHome(path);
            var paths = GetSingerSearchPaths().ToList();
            if (!paths.Contains(path))
            {
                paths.Add(path);
                SetSingerSearchPaths(paths.ToArray());
                Core.Util.Preferences.Save();
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
                Core.Util.Preferences.Save();
            }
        }

        public string GetCachePath(string filepath)
        {
            string cachepath;
            if (filepath == string.Empty) cachepath = Path.Combine(_homePath, DefaultCachePath);
            else cachepath = Path.Combine(Path.GetDirectoryName(filepath), DefaultCachePath);
            if (!Directory.Exists(cachepath)) Directory.CreateDirectory(cachepath);
            return cachepath;
        }

        public string GetEngineSearchPath()
        {
            return Path.Combine(_homePath, "Resamplers");
        }

        public string GetPreviewEnginePath()
        {
            if (Core.Util.Preferences.Default.InternalEnginePreview) return "TnFndsOU.dll";
            else return Path.Combine(GetEngineSearchPath(), Core.Util.Preferences.Default.ExternalPreviewEngine);
        }

        public string GetExportEnginePath()
        {
            if (Core.Util.Preferences.Default.InternalEngineExport) return "TnFndsOU.dll";
            else return Path.Combine(GetEngineSearchPath(), Core.Util.Preferences.Default.ExternalExportEngine);
        }
    }
}

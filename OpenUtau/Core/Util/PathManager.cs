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
            if (Properties.Settings.Default.SingerPaths == null)
                Properties.Settings.Default.SingerPaths = new System.Collections.Specialized.StringCollection();
            AddSingerSearchPath(@"Singers");
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
            return "";
        }

        public string[] GetSingerSearchPaths()
        {
            string[] singerSearchPaths = new string[Properties.Settings.Default.SingerPaths.Count];
            Properties.Settings.Default.SingerPaths.CopyTo(singerSearchPaths, 0);
            return singerSearchPaths;
        }

        public void AddSingerSearchPath(string path)
        {
            path = MakeRelativeToHome(path);
            if (!Properties.Settings.Default.SingerPaths.Contains(path))
            {
                Properties.Settings.Default.SingerPaths.Add(path);
                Properties.Settings.Default.Save();
            }
        }

        public void RemoveSingerSearchPath(string path)
        {
            if (path == DefaultSingerPath) return;
            if (Properties.Settings.Default.SingerPaths.Contains(path))
            {
                Properties.Settings.Default.SingerPaths.Remove(path);
                Properties.Settings.Default.Save();
            }
        }

        public string GetCachePath(string filepath)
        {
            string cachepath;
            if (filepath == "") cachepath = Path.Combine(_homePath, DefaultCachePath);
            else cachepath = Path.Combine(Path.GetDirectoryName(filepath), DefaultCachePath);
            if (!Directory.Exists(cachepath)) Directory.CreateDirectory(cachepath);
            return cachepath;
        }

        public string GetTool1Path()
        {
            return Path.Combine(_homePath, "tn_fnds.exe");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using OpenUtau.Core.USTx;
using OpenUtau.Core.Lib;

namespace OpenUtau.Core.Formats
{
    public static class UtauSoundbank
    {
        public static Dictionary<string, USinger> FindAllSingers()
        {
            Dictionary<string, USinger> singers = new Dictionary<string, USinger>();
            var singerSearchPaths = PathManager.Inst.GetSingerSearchPaths();
            foreach (string searchPath in singerSearchPaths)
            {
                if (!Directory.Exists(searchPath)) continue;
                foreach (var dirpath in Directory.EnumerateDirectories(searchPath))
                {
                    if (File.Exists(Path.Combine(dirpath, "character.txt")) &&
                        File.Exists(Path.Combine(dirpath, "oto.ini")))
                    {
                        var singer = LoadSinger(dirpath);
                        singers.Add(singer.Path, singer);
                    }
                }
            }
            return singers;
        }

        public static USinger GetSinger(string path, Encoding ustEncoding, Dictionary<string, USinger> loadedSingers)
        {
            var absPath = DetectSingerPath(path, ustEncoding);
            if (loadedSingers.ContainsKey(absPath)) return loadedSingers[absPath];
            else
            {
                var singer = LoadSinger(absPath);
                loadedSingers.Add(absPath, singer);
                return singer;
            }
        }

        static string DetectSingerPath(string path, Encoding ustEncoding)
        {
            var pathEncoding = DetectSingerPathEncoding(path, ustEncoding);
            return PathManager.Inst.GetSingerAbsPath(EncodingUtil.ConvertEncoding(ustEncoding, pathEncoding, path));
        }

        static USinger LoadSinger(string path)
        {
            if (!Directory.Exists(path) ||
                !File.Exists(Path.Combine(path, "character.txt")) ||
                !File.Exists(Path.Combine(path, "oto.ini"))) return null;
            
            USinger singer = new USinger();
            singer.Path = path;
            singer.FileEncoding = EncodingUtil.DetectFileEncoding(Path.Combine(singer.Path, "oto.ini"));
            singer.PathEncoding = Encoding.Default;
            string[] lines = File.ReadAllLines(Path.Combine(singer.Path, "oto.ini"), singer.FileEncoding);

            int i = 0;
            while (i < 16 && i < lines.Count())
            {
                if (lines[i].Contains("="))
                {
                    string filename = lines[i].Split(new[] { '=' })[0];
                    var detected = DetectPathEncoding(filename, singer.Path, singer.FileEncoding);
                    if (singer.PathEncoding == Encoding.Default) singer.PathEncoding = detected;
                    i++;
                }
            }
            if (singer.PathEncoding == null) return null;
            
            LoadOtos(singer);

            try
            {
                lines = File.ReadAllLines(Path.Combine(singer.Path, "character.txt"), singer.FileEncoding);
            }
            catch { return null; }

            foreach (var line in lines){
                if (line.StartsWith("name=")) singer.Name = line.Trim().Replace("name=", "");
                if (line.StartsWith("image="))
                {
                    string imagePath = line.Trim().Replace("image=", "");
                    Uri imagepath = new Uri(Path.Combine(singer.Path, EncodingUtil.ConvertEncoding(singer.FileEncoding, singer.PathEncoding, imagePath)));
                    singer.Avatar = new System.Windows.Media.Imaging.BitmapImage(imagepath);
                    singer.Avatar.Freeze();
                }
                if (line.StartsWith("author=")) singer.Author = line.Trim().Replace("author=", "");
                if (line.StartsWith("web=")) singer.Website = line.Trim().Replace("web=", "");
            }

            LoadPrefixMap(singer);

            return singer;
        }

        static Encoding DetectSingerPathEncoding(string singerPath, Encoding ustEncoding)
        {
            string[] encodings = new string[] { "shift_jis", "gbk", "utf-8" };
            foreach (string encoding in encodings)
            {
                string path = EncodingUtil.ConvertEncoding(ustEncoding, Encoding.GetEncoding(encoding), singerPath);
                if (PathManager.Inst.GetSingerAbsPath(path) != "") return Encoding.GetEncoding(encoding);
            }
            return null;
        }
        
        static Encoding DetectPathEncoding(string path, string basePath, Encoding encoding)
        {
            string[] encodings = new string[] { "shift_jis", "gbk", "utf-8" };
            foreach (string enc in encodings)
            {
                string absPath = Path.Combine(basePath, EncodingUtil.ConvertEncoding(encoding, Encoding.GetEncoding(enc), path));
                if (File.Exists(absPath) || Directory.Exists(absPath)) return Encoding.GetEncoding(enc);
            }
            return null;
        }

        static void LoadOtos(USinger singer)
        {
            string path = singer.Path;
            if (File.Exists(Path.Combine(path, "oto.ini"))) LoadOto(path, path, singer);
            foreach (var dirpath in Directory.EnumerateDirectories(path))
                if (File.Exists(Path.Combine(dirpath, "oto.ini"))) LoadOto(dirpath, path, singer);
        }

        static void LoadOto(string dirpath, string path, USinger singer)
        {
            string file = Path.Combine(dirpath, "oto.ini");
            string relativeDir = dirpath.Replace(path, "");
            while (relativeDir.StartsWith("\\")) relativeDir = relativeDir.Substring(1);
            string[] lines = File.ReadAllLines(file, singer.FileEncoding);
            foreach (var line in lines)
            {
                var s = line.Split(new[] { '=' });
                if (s.Count() == 2)
                {
                    string wavfile = s[0];
                    var args = s[1].Split(new[] { ',' });
                    if (singer.AliasMap.ContainsKey(args[0])) continue;
                    singer.AliasMap.Add(args[0], new UOto()
                    {
                        File = Path.Combine(relativeDir, wavfile),
                        Alias = args[0],
                        Offset = int.Parse(args[1]),
                        Consonant = int.Parse(args[2]),
                        Cutoff = int.Parse(args[3]),
                        Preutter = int.Parse(args[4]),
                        Overlap = int.Parse(args[5])
                    });
                }
            }
        }

        static void LoadPrefixMap(USinger singer)
        {
            string path = singer.Path;
            if (File.Exists(Path.Combine(path, "prefix.map"))) 
            {
                string[] lines;
                try
                {
                    lines = File.ReadAllLines(Path.Combine(path, "prefix.map"));
                }
                catch
                {
                    throw new Exception("Prefix map exists but cannot be opened for read.");
                }

                foreach (string line in lines)
                {
                    var s = line.Trim().Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                    if (s.Count() == 2)
                    {
                        string source = s[0];
                        string target = s[1];
                        singer.PitchMap.Add(source, target);
                    }
                }
            }
        }
    }
}

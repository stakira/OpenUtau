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
        public static USinger LoadSinger(string path, Encoding ustEncoding)
        {
            USinger singer = new USinger();
            singer.HomePath = @"I:\Utau\voice";
            singer.SoundbankPath = path;
            singer.PathEncoding = GetPathEncoding(ustEncoding, singer.HomePath, singer.SoundbankPath);
            if (singer.PathEncoding == null) return null;
            singer.FileEncoding = ustEncoding;

            if (!Directory.Exists(singer.ActualPath)) return null;
            if (!File.Exists(Path.Combine(singer.ActualPath, "character.txt"))) return null;
            if (!File.Exists(Path.Combine(singer.ActualPath, "oto.ini"))) return null;

            singer.FileEncoding = EncodingUtil.DetectFileEncoding(Path.Combine(singer.ActualPath, "oto.ini"));
            singer.SoundbankPath = EncodingUtil.ConvertEncoding(ustEncoding, singer.FileEncoding, singer.SoundbankPath);
            LoadOtos(singer);

            string[] lines;
            try
            {
                lines = File.ReadAllLines(Path.Combine(singer.ActualPath, "character.txt"), singer.FileEncoding);
            }
            catch { return null; }

            foreach (var line in lines){
                if (line.StartsWith("name=")) singer.Name = line.Trim().Replace("name=", "");
                if (line.StartsWith("image=")) singer.ImagePath = line.Trim().Replace("image=", "");
                if (line.StartsWith("author=")) singer.Author = line.Trim().Replace("author=", "");
                if (line.StartsWith("web=")) singer.Website = line.Trim().Replace("web=", "");
            }

            if (singer.ImagePath != null)
            {
                Uri imagepath = new Uri(Path.Combine(singer.ActualPath, singer.ImagePath));
                singer.Avatar = new System.Windows.Media.Imaging.BitmapImage(imagepath);
            }

            LoadPrefixMap(singer);

            return singer;
        }

        static Encoding GetPathEncoding(Encoding pathEncoding, string homepath, string path)
        {
            if (Directory.Exists(Path.Combine(homepath, EncodingUtil.ConvertEncoding(pathEncoding, Encoding.GetEncoding("shift_jis"), path))))
                return Encoding.GetEncoding("shift_jis");
            else if (Directory.Exists(Path.Combine(homepath, EncodingUtil.ConvertEncoding(pathEncoding, Encoding.GetEncoding("gbk"), path))))
                return Encoding.GetEncoding("gbk");
            else if (Directory.Exists(Path.Combine(homepath, EncodingUtil.ConvertEncoding(pathEncoding, Encoding.UTF8, path))))
                return Encoding.UTF8;
            else
                return null;
        }

        static void LoadOtos(USinger singer)
        {
            string path = singer.ActualPath;
            var otopaths = new List<string>();
            if (File.Exists(Path.Combine(path, "oto.ini"))) otopaths.Add(Path.Combine(path, "oto.ini"));
            foreach (var dirpath in Directory.EnumerateDirectories(path))
                if (File.Exists(Path.Combine(dirpath, "oto.ini"))) otopaths.Add(Path.Combine(dirpath, "oto.ini"));
            foreach (var otopath in otopaths) LoadOto(otopath, singer);
        }
        
        static void LoadOto(string file, USinger singer)
        {
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
                        File = wavfile,
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
            string path = singer.ActualPath;
            if (File.Exists(Path.Combine(path, "prefix.map"))) 
            {
                singer.MultiPitch = true;
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

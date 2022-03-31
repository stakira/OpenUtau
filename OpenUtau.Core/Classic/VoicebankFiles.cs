using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using K4os.Hash.xxHash;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Classic {
    static class VoicebankFiles {

        static object fileAccessLock = new object();

        public static string GetSourceTempPath(string singerId, UOto oto) {
            string ext = Path.GetExtension(oto.File);
            return Path.Combine(PathManager.Inst.CachePath,
                $"src-{HashHex(singerId)}-{HashHex(oto.Set)}-{HashHex(oto.File)}{ext}");
        }

        private static string HashHex(string s) {
            return $"{XXH32.DigestOf(Encoding.UTF8.GetBytes(s)):x8}";
        }

        public static void CopySourceTemp(string source, string temp) {
            CopyOrStamp(source, temp, true);
            var metaFiles = GetMetaFiles(source, temp);
            metaFiles.ForEach(t => CopyOrStamp(t.Item1, t.Item2, false));
        }

        public static void CopyBackMetaFiles(string source, string temp) {
            var metaFiles = GetMetaFiles(source, temp);
            metaFiles.ForEach(t => CopyOrStamp(t.Item2, t.Item1, false));
        }

        private static List<Tuple<string, string>> GetMetaFiles(string source, string sourceTemp) {
            string ext = Path.GetExtension(source);
            string frqExt = ext.Replace('.', '_') + ".frq";
            string noExt = source.Substring(0, source.Length - ext.Length);
            string tempNoExt = sourceTemp.Substring(0, sourceTemp.Length - ext.Length);
            return new List<Tuple<string, string>>() {
                Tuple.Create(noExt + frqExt, tempNoExt + frqExt),
                Tuple.Create(source + ".llsm", sourceTemp + ".llsm"),
                Tuple.Create(source + ".uspec", sourceTemp + ".uspec"),
                Tuple.Create(source + ".dio", sourceTemp + ".dio"),
                Tuple.Create(source + ".star", sourceTemp + ".star"),
                Tuple.Create(source + ".platinum", sourceTemp + ".platinum"),
                Tuple.Create(source + ".frc", sourceTemp + ".frc"),
                Tuple.Create(source + ".pmk", sourceTemp + ".pmk"),
                Tuple.Create(source + ".vs4ufrq", sourceTemp + ".vs4ufrq"),
                Tuple.Create(noExt + ".rudb", tempNoExt + ".rudb"),
            };
        }

        private static void CopyOrStamp(string source, string dest, bool required) {
            lock (fileAccessLock) {
                if (!File.Exists(source)) {
                    if (required) {
                        Log.Error($"Source file {source} not found");
                        throw new FileNotFoundException($"Source file {source} not found");
                    }
                } else if (!File.Exists(dest)) {
                    Log.Verbose($"Copy {source} to {dest}");
                    File.Copy(source, dest);
                }
            }
        }

        public static void ReleaseSourceTemp() {
            lock (fileAccessLock) {
                var expire = DateTime.Now - TimeSpan.FromDays(7);
                string path = PathManager.Inst.CachePath;
                Log.Information($"ReleaseSourceTemp {path}");
                Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(file =>
                        !File.GetAttributes(file).HasFlag(FileAttributes.Directory)
                            && File.GetCreationTime(file) < expire)
                    .ToList()
                    .ForEach(file => File.Delete(file));
            }
        }
    }
}

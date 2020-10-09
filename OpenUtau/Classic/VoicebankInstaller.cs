using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Serilog;

namespace OpenUtau.Classic {

    internal class VoicebankInstaller {

        public static void IndexAllArchive(IEnumerable<string> searchPaths) {
            foreach (var path in searchPaths) {
                if (Directory.Exists(path)) {
                    var zips = Directory.GetFiles(path, "*.zip", SearchOption.AllDirectories);
                    foreach (var zip in zips) {
                        Log.Information($"{zip}");
                        IndexArchive(zip);
                    }
                }
            }
        }

        public static void IndexArchive(string path) {
            using (var stream = new FileStream(path, FileMode.Open)) {
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, false, Encoding.GetEncoding("shift_jis"))) {
                    foreach (var entry in archive.Entries) {
                        if (entry.Name == "character.txt" || entry.Name == "oto.ini") {
                            Log.Information($"{entry.Name} {entry.FullName}");
                        }
                    }
                }
            }
        }
    }
}

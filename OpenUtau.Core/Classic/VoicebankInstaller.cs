using System;
using System.IO;
using System.Linq;
using System.Text;
using K4os.Hash.xxHash;
using Newtonsoft.Json;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace OpenUtau.Classic {

    public class VoicebankInstaller {
        private string basePath;
        readonly Action<double, string> progress;

        public VoicebankInstaller(string basePath, Action<double, string> progress) {
            if (OS.IsWindows()) {
                // Only Windows need to work with exe resamplers.
                if (basePath.Length > 80) {
                    throw new ArgumentException("Path too long. Try to move OpenUtau to a shorter path.");
                }
                foreach (char c in basePath) {
                    if (c > 255) {
                        throw new ArgumentException("Do not place OpenUtau in a non-ASCII path.");
                    }
                }
            }
            Directory.CreateDirectory(basePath);
            this.basePath = basePath;
            this.progress = progress;
        }

        public void LoadArchive(string path) {
            var encoding = Encoding.GetEncoding("shift_jis");
            if (encoding == null) {
                throw new Exception($"Failed to detect encoding of {path}.");
            }
            progress.Invoke(0, "Analyzing archive...");
            var readerOptions = new ReaderOptions {
                ArchiveEncoding = new ArchiveEncoding(encoding, encoding)
            };
            var extractionOptions = new ExtractionOptions {
                Overwrite = true,
            };
            var jsonSeriSettings = new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore
            };
            string[] textFiles = { ".txt", ".ini", ".map" };
            using (var archive = ArchiveFactory.Open(path, readerOptions)) {
                AdjustBasePath(archive);
                int total = archive.Entries.Count();
                int count = 0;
                foreach (var entry in archive.Entries) {
                    var filePath = Path.Combine(basePath, entry.Key);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    if (entry.IsDirectory) {
                    } else if (textFiles.Contains(Path.GetExtension(entry.Key))) {
                        using (var stream = entry.OpenEntryStream()) {
                            using (var reader = new StreamReader(stream, encoding)) {
                                File.WriteAllText(Path.Combine(basePath, entry.Key), reader.ReadToEnd(), Encoding.UTF8);
                            }
                        }
                    } else {
                        entry.WriteToFile(Path.Combine(basePath, entry.Key), extractionOptions);
                    }
                    progress.Invoke(100.0 * ++count / total, entry.Key);
                }
            }
        }

        private void AdjustBasePath(IArchive archive) {
            var characters = archive.Entries.Where(e => Path.GetFileName(e.Key) == "character.txt").ToList();
            if (characters.Count > 0) {
                var entry = characters.FirstOrDefault(e => e.Key == "character.txt");
                if (entry == null) {
                    return;
                }
                var encoding = Encoding.GetEncoding("shift_jis");
                using (var stream = entry.OpenEntryStream()) {
                    using (var reader = new StreamReader(stream, encoding)) {
                        while (!reader.EndOfStream) {
                            var line = reader.ReadLine();
                            if (line.StartsWith("name=")) {
                                var name = line.Replace("name=", "").Trim();
                                basePath = Path.Combine(basePath, name);
                                return;
                            }
                        }
                    }
                }
            }
            basePath = Path.Combine(basePath, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        }

        static string HashPath(string path) {
            string file = Path.GetFileName(path);
            string dir = Path.GetDirectoryName(path);
            file = $"{XXH32.DigestOf(Encoding.UTF8.GetBytes(file)):x8}";
            if (string.IsNullOrEmpty(dir)) {
                return file;
            }
            return Path.Combine(HashPath(dir), file);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Serilog;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using xxHashSharp;

namespace OpenUtau.Classic {

    class VoicebankInstaller {
        readonly string basePath;
        readonly Action<double, string> progress;

        public VoicebankInstaller(string basePath, Action<double, string> progress) {
            if (basePath.Length > 80) {
                throw new ArgumentException("Path too long. Try to move OpenUtau to a shorter path.");
            }
            foreach (char c in basePath) {
                if (c > 255) {
                    throw new ArgumentException("Do not place OpenUtau in a non-ASCII path.");
                }
            }
            Directory.CreateDirectory(basePath);
            this.basePath = basePath;
            this.progress = progress;
        }

        public void LoadArchive(string path) {
            var encoding = Encoding.GetEncoding(932);
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

        static Encoding DetectArchiveFileEncoding(string path) {
            Encoding encoding = Encoding.GetEncoding(1252);
            var options = new ReaderOptions {
                ArchiveEncoding = new ArchiveEncoding(encoding, encoding)
            };
            var detector = new Ude.CharsetDetector();
            using (var archive = ArchiveFactory.Open(path, options)) {
                foreach (var entry in archive.Entries) {
                    byte[] buffer = encoding.GetBytes(entry.Key);
                    detector.Feed(buffer, 0, buffer.Length);
                }
            }
            detector.DataEnd();
            Log.Information($"{path} charset: {detector.Charset} confidence: {detector.Confidence}");
            return Encoding.GetEncoding(detector.Charset);
        }

        static string HashPath(string path) {
            string file = Path.GetFileName(path);
            string dir = Path.GetDirectoryName(path);
            file = $"{xxHash.CalculateHash(Encoding.UTF8.GetBytes(file)):x8}";
            if (string.IsNullOrEmpty(dir)) {
                return file;
            }
            return Path.Combine(HashPath(dir), file);
        }
    }
}

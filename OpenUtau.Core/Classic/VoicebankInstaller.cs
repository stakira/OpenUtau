using System;
using System.Collections.Generic;
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
        const string kCharacterTxt = "character.txt";
        const string kInstallTxt = "install.txt";

        private string basePath;
        private readonly Action<double, string> progress;
        private readonly Encoding archiveEncoding;
        private readonly Encoding textEncoding;

        public VoicebankInstaller(string basePath, Action<double, string> progress, Encoding archiveEncoding = null, Encoding textEncoding = null) {
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
            this.archiveEncoding = archiveEncoding ?? Encoding.GetEncoding("shift_jis");
            this.textEncoding = textEncoding ?? Encoding.GetEncoding("shift_jis");
        }

        public void LoadArchive(string path) {
            progress.Invoke(0, "Analyzing archive...");
            var readerOptions = new ReaderOptions {
                ArchiveEncoding = new ArchiveEncoding(archiveEncoding, archiveEncoding)
            };
            var extractionOptions = new ExtractionOptions {
                Overwrite = true,
            };
            var jsonSeriSettings = new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore
            };
            string[] textFiles = { ".txt", ".ini", ".map" };
            using (var archive = ArchiveFactory.Open(path, readerOptions)) {
                var touches = new List<string>();
                AdjustBasePath(archive, path, touches);
                int total = archive.Entries.Count();
                int count = 0;
                foreach (var entry in archive.Entries) {
                    var filePath = Path.Combine(basePath, entry.Key);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    if (entry.IsDirectory || entry.Key == kInstallTxt) {
                    } else if (textFiles.Contains(Path.GetExtension(entry.Key))) {
                        using (var stream = entry.OpenEntryStream()) {
                            using (var reader = new StreamReader(stream, textEncoding)) {
                                File.WriteAllText(Path.Combine(basePath, entry.Key), reader.ReadToEnd(), Encoding.UTF8);
                            }
                        }
                    } else {
                        entry.WriteToFile(Path.Combine(basePath, entry.Key), extractionOptions);
                    }
                    progress.Invoke(100.0 * ++count / total, entry.Key);
                }
                foreach (var touch in touches) {
                    File.WriteAllText(touch, "\n");
                }
            }
        }

        private void AdjustBasePath(IArchive archive, string archivePath, List<string> touches) {
            var dirsAndFiles = archive.Entries.Select(e => e.Key).ToHashSet();
            var rootDirs = archive.Entries
                .Where(e => e.IsDirectory)
                .Where(e => (e.Key.IndexOf('\\') < 0 || e.Key.IndexOf('\\') == e.Key.Length - 1)
                         && (e.Key.IndexOf('/') < 0 || e.Key.IndexOf('/') == e.Key.Length - 1))
                .ToArray();
            var rootFiles = archive.Entries
                .Where(e => !e.IsDirectory)
                .Where(e => !e.Key.Contains('\\') && !e.Key.Contains('/') && e.Key != kInstallTxt)
                .ToArray();
            if (rootFiles.Count() > 0) {
                // Need to create root folder.
                basePath = Path.Combine(basePath, Path.GetFileNameWithoutExtension(archivePath));
                if (rootFiles.Where(e => e.Key == kCharacterTxt).Count() == 0) {
                    // Need to create character.txt.
                    touches.Add(Path.Combine(basePath, kCharacterTxt));
                }
                return;
            }
            foreach (var rootDir in rootDirs) {
                if (!dirsAndFiles.Contains($"{rootDir.Key}{kCharacterTxt}") &&
                    !dirsAndFiles.Contains($"{rootDir.Key}\\{kCharacterTxt}") &&
                    !dirsAndFiles.Contains($"{rootDir.Key}/{kCharacterTxt}")) {
                    touches.Add(Path.Combine(basePath, rootDir.Key, kCharacterTxt));
                }
            }
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

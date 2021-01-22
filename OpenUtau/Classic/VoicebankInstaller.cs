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
            var copied = new string[] { ".bmp", ".jpg", ".gif" };
            var hashed = new string[] { ".wav", "_wav.frq" };
            string characterPath = null;
            using (var archive = ArchiveFactory.Open(path, readerOptions)) {
                foreach (var entry in archive.Entries) {
                    if (Path.GetFileName(entry.Key) == "character.txt") {
                        characterPath = Path.GetDirectoryName(entry.Key);
                        break;
                    }
                }
            }
            using (var archive = ArchiveFactory.Open(path, readerOptions)) {
                int total = archive.Entries.Count();
                int count = 0;
                foreach (var entry in archive.Entries) {
                    progress.Invoke(++count * 100.0 / total, entry.Key);
                    if (entry.IsDirectory) {
                        continue;
                    }
                    if (Path.GetFileName(entry.Key) == "oto.ini") {
                        OtoSet otoSet;
                        using (var streamReader = new StreamReader(entry.OpenEntryStream(), encoding)) {
                            otoSet = ParseOtoSet(streamReader);
                        }
                        otoSet.OrigFile = entry.Key;
                        string filePath = Path.Combine(HashPath(Path.GetDirectoryName(entry.Key)), "_oto.json");
                        otoSet.File = filePath;
                        otoSet.Name = Path.GetDirectoryName(entry.Key).Replace(characterPath, "").Trim(new char[] { '/', '\\' });
                        filePath = Path.Combine(basePath, filePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                        File.WriteAllText(filePath, JsonConvert.SerializeObject(otoSet, Formatting.Indented, jsonSeriSettings));
                        continue;
                    }
                    if (Path.GetFileName(entry.Key) == "character.txt") {
                        Voicebank voicebank = new Voicebank { OrigFile = entry.Key };
                        using (var streamReader = new StreamReader(entry.OpenEntryStream(), encoding)) {
                            var otherLines = new List<string>();
                            while (!streamReader.EndOfStream) {
                                string line = streamReader.ReadLine().Trim();
                                var s = line.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                                if (s.Length == 2) {
                                    if (s[0] == "name") {
                                        voicebank.Name = s[1];
                                    } else if (s[0] == "image") {
                                        voicebank.Image = s[1];
                                    } else if (s[0] == "author") {
                                        voicebank.Author = s[1];
                                    } else if (s[0] == "web") {
                                        voicebank.Web = s[1];
                                    } else {
                                        otherLines.Add(line);
                                    }
                                } else {
                                    otherLines.Add(line);
                                }
                            }
                            voicebank.OtherInfo = string.Join("\n", otherLines);
                        }
                        if (string.IsNullOrEmpty(voicebank.Name)) {
                            throw new FileFormatException(string.Format("Failed to load character.txt using encoding {0}", encoding));
                        }
                        string filePath = Path.Combine(HashPath(Path.GetDirectoryName(entry.Key)), "_voicebank.json");
                        voicebank.File = filePath;
                        filePath = Path.Combine(basePath, filePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                        File.WriteAllText(filePath, JsonConvert.SerializeObject(voicebank, Formatting.Indented, jsonSeriSettings));
                        continue;
                    }
                    if (Path.GetFileName(entry.Key) == "prefix.map") {
                        var prefixMap = new PrefixMap {
                            OrigFile = entry.Key
                        };
                        using (var streamReader = new StreamReader(entry.OpenEntryStream(), encoding)) {
                            while (!streamReader.EndOfStream) {
                                var s = streamReader.ReadLine().Trim().Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                                if (s.Length == 2) {
                                    string source = s[0];
                                    string target = s[1];
                                    prefixMap.Map[source] = target;
                                }
                            }
                        }
                        string filePath = Path.Combine(HashPath(Path.GetDirectoryName(entry.Key)), "_prefix_map.json");
                        prefixMap.File = filePath;
                        filePath = Path.Combine(basePath, filePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                        File.WriteAllText(filePath, JsonConvert.SerializeObject(prefixMap, Formatting.Indented, jsonSeriSettings));
                        continue;
                    }
                    bool handled = false;
                    foreach (var nameEnd in copied) {
                        if (entry.Key.EndsWith(nameEnd)) {
                            string dir = Path.GetDirectoryName(entry.Key);
                            string fileName = Path.GetFileName(entry.Key);
                            string filePath = Path.Combine(basePath, HashPath(dir), fileName);
                            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                            entry.WriteToFile(filePath, extractionOptions);
                            handled = true;
                            break;
                        }
                    }
                    if (handled) {
                        continue;
                    }
                    foreach (var nameEnd in hashed) {
                        if (entry.Key.EndsWith(nameEnd)) {
                            string filePath = Path.Combine(basePath, HashPath(entry.Key.Substring(0, entry.Key.Length - nameEnd.Length)) + nameEnd);
                            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                            entry.WriteToFile(filePath, extractionOptions);
                            break;
                        }
                    }
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

        static OtoSet ParseOtoSet(StreamReader streamReader) {
            OtoSet otoSet = new OtoSet();
            while (!streamReader.EndOfStream) {
                var line = streamReader.ReadLine();
                Oto oto = ParseOto(line);
                if (oto != null) {
                    otoSet.Otos.Add(oto);
                }
            }
            return otoSet;
        }

        static Oto ParseOto(string line) {
            if (!line.Contains("=")) {
                return null;
            }
            var parts = line.Split('=');
            if (parts.Length != 2) {
                return null;
            }
            var wav = parts[0].Trim();
            parts = parts[1].Split(',');
            if (parts.Length != 6) {
                return null;
            }
            var ext = Path.GetExtension(wav);
            var result = new Oto {
                OrigWav = wav,
                Wav = HashPath(wav.Replace(ext, "")) + ext,
                Name = parts[0].Trim()
            };
            double.TryParse(parts[1], out result.Offset);
            double.TryParse(parts[2], out result.Consonant);
            double.TryParse(parts[3], out result.Cutoff);
            double.TryParse(parts[4], out result.Preutter);
            double.TryParse(parts[5], out result.Overlap);
            return result;
        }
    }
}

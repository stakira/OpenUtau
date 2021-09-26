using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Serilog;

namespace OpenUtau.Classic {
    public class VoicebankLoader {
        class FileLoc {
            public string file;
            public int lineNumber;
            public string line;

            public override string ToString() {
                return $"\"{file}\"\nat line {lineNumber + 1}:\n\"{line}\"";
            }
        }

        readonly string basePath;

        public VoicebankLoader(string basePath) {
            this.basePath = basePath;
        }

        public Dictionary<string, Voicebank> LoadAll() {
            Dictionary<string, Voicebank> result = new Dictionary<string, Voicebank>();
            if (!Directory.Exists(basePath)) {
                return result;
            }
            var banks = Directory.EnumerateFiles(basePath, "character.txt", SearchOption.AllDirectories)
                .Select(filePath => ParseCharacterTxt(filePath, basePath, Encoding.UTF8))
                .OfType<Voicebank>()
                .OrderByDescending(bank => bank.File.Length)
                .ToArray();
            var otoSets = Directory.EnumerateFiles(basePath, "oto.ini", SearchOption.AllDirectories)
                .Select(entry => ParseOtoSet(entry, Encoding.UTF8))
                .OfType<OtoSet>()
                .ToArray();
            var prefixMaps = Directory.EnumerateFiles(basePath, "prefix.map", SearchOption.AllDirectories)
                .Select(entry => ParsePrefixMap(entry, Encoding.UTF8))
                .OfType<PrefixMap>()
                .ToArray();
            var bankDirs = banks
                .Select(bank => Path.GetDirectoryName(bank.File))
                .ToArray();
            foreach (var otoSet in otoSets) {
                var dir = Path.GetDirectoryName(otoSet.File);
                for (int i = 0; i < bankDirs.Length; ++i) {
                    if (dir.StartsWith(bankDirs[i])) {
                        otoSet.Name = Path.GetRelativePath(bankDirs[i], Path.GetDirectoryName(otoSet.File));
                        if (otoSet.Name == ".") {
                            otoSet.Name = string.Empty;
                        }
                        banks[i].OtoSets.Add(otoSet);
                        break;
                    }
                }
            }
            foreach (var prefixMap in prefixMaps) {
                var dir = Path.GetDirectoryName(prefixMap.File);
                for (int i = 0; i < bankDirs.Length; ++i) {
                    if (dir.StartsWith(bankDirs[i])) {
                        banks[i].PrefixMap = prefixMap;
                        break;
                    }
                }
            }
            foreach (var bank in banks) {
                var dir = Path.GetDirectoryName(bank.File);
                var file = Path.Combine(dir, "character.yaml");
                if (File.Exists(file)) {
                    using (var stream = File.OpenRead(file)) {
                        var bankConfig = VoicebankConfig.Load(stream);
                        foreach (var otoSet in bank.OtoSets) {
                            var subbank = bankConfig.Subbanks.FirstOrDefault(b => b.Dir == otoSet.Name);
                            if (subbank != null) {
                                otoSet.Prefix = subbank.Prefix;
                                otoSet.Suffix = subbank.Suffix;
                                otoSet.Flavor = subbank.Flavor;
                                if (!string.IsNullOrEmpty(subbank.Prefix)) {
                                    foreach (var oto in otoSet.Otos) {
                                        string phonetic = oto.Alias;
                                        if (phonetic.StartsWith(subbank.Prefix)) {
                                            phonetic = phonetic.Substring(subbank.Prefix.Length);
                                        }
                                        oto.Phonetic = phonetic;
                                    }
                                }
                                if (!string.IsNullOrEmpty(subbank.Suffix)) {
                                    foreach (var oto in otoSet.Otos) {
                                        string phonetic = oto.Phonetic ?? oto.Alias;
                                        if (phonetic.EndsWith(subbank.Suffix)) {
                                            phonetic = phonetic.Substring(0, phonetic.Length - subbank.Suffix.Length);
                                        }
                                        oto.Phonetic = phonetic;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach (var bank in banks) {
                result.Add(bank.Id, bank);
            }
            return result;
        }

        public static Voicebank ParseCharacterTxt(string filePath, string basePath, Encoding encoding) {
            try {
                using (var stream = File.OpenRead(filePath)) {
                    return ParseCharacterTxt(stream, filePath, basePath, encoding);
                }
            } catch (Exception e) {
                Log.Error(e, $"Failed to load {filePath}");
            }
            return null;
        }

        public static Voicebank ParseCharacterTxt(Stream stream, string filePath, string basePath, Encoding encoding) {
            using (var reader = new StreamReader(stream, encoding)) {
                var voicebank = new Voicebank() {
                    File = filePath,
                };
                var otherLines = new List<string>();
                while (!reader.EndOfStream) {
                    string line = reader.ReadLine().Trim();
                    var s = line.Split(new char[] { '=' });
                    if (s.Length != 2) {
                        s = line.Split(new char[] { ':' });
                    }
                    Array.ForEach(s, temp => temp.Trim());
                    if (s.Length == 2) {
                        s[0] = s[0].ToLowerInvariant();
                        if (s[0] == "name") {
                            voicebank.Name = s[1];
                        } else if (s[0] == "image") {
                            voicebank.Image = s[1];
                        } else if (s[0] == "author" || s[0] == "created by") {
                            voicebank.Author = s[1];
                        } else if (s[0] == "sample") {
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
                voicebank.Id = Path.GetRelativePath(basePath, Path.GetDirectoryName(voicebank.File));
                if (string.IsNullOrEmpty(voicebank.Name)) {
                    voicebank.Name = $"No Name ({voicebank.Id})";
                }
                return voicebank;
            }
        }

        public static PrefixMap ParsePrefixMap(string filePath, Encoding encoding) {
            try {
                using (var stream = File.OpenRead(filePath)) {
                    return ParsePrefixMap(stream, filePath, encoding);
                }
            } catch (Exception e) {
                Log.Error(e, $"Failed to load {filePath}");
            }
            return null;
        }

        public static PrefixMap ParsePrefixMap(Stream stream, string filePath, Encoding encoding) {
            using (var reader = new StreamReader(stream, encoding)) {
                var prefixMap = new PrefixMap {
                    File = filePath,
                };
                while (!reader.EndOfStream) {
                    var s = reader.ReadLine().Split(new char[0]);
                    if (s.Length == 3) {
                        string source = s[0].Trim();
                        string prefix = s[1].Trim();
                        string suffix = s[2].Trim();
                        prefixMap.Map[source] = new Tuple<string, string>(prefix, suffix);
                    }
                }
                return prefixMap;
            }
        }

        public static OtoSet ParseOtoSet(string filePath, Encoding encoding) {
            try {
                using (var stream = File.OpenRead(filePath)) {
                    return ParseOtoSet(stream, filePath, encoding);
                }
            } catch (Exception e) {
                Log.Error(e, $"Failed to load {filePath}");
            }
            return null;
        }

        public static OtoSet ParseOtoSet(Stream stream, string filePath, Encoding encoding) {
            OtoSet otoSet;
            using (var reader = new StreamReader(stream, encoding)) {
                var fileLoc = new FileLoc { file = filePath, lineNumber = 0 };
                otoSet = new OtoSet() {
                    File = filePath,
                };
                while (!reader.EndOfStream) {
                    var line = reader.ReadLine();
                    fileLoc.line = line;
                    try {
                        Oto oto = ParseOto(line);
                        if (oto != null) {
                            otoSet.Otos.Add(oto);
                        }
                    } catch (Exception e) {
                        Log.Error(e, $"Failed to parse\n{fileLoc}");
                        otoSet.Errors.Add($"Oto error:\n{fileLoc}");
                    }
                    fileLoc.line = null;
                    fileLoc.lineNumber++;
                }
            }
            // Use filename as alias if not in oto.
            var knownFiles = otoSet.Otos.Select(oto => oto.Wav).ToHashSet();
            foreach (var wav in Directory.EnumerateFiles(Path.GetDirectoryName(filePath), "*.wav", SearchOption.TopDirectoryOnly)) {
                var file = Path.GetFileName(wav);
                if (!knownFiles.Contains(file)) {
                    var oto = new Oto {
                        Alias = Path.GetFileNameWithoutExtension(file),
                        Wav = file,
                    };
                    oto.Phonetic = oto.Alias;
                    otoSet.Otos.Add(oto);
                }
            }
            return otoSet;
        }

        public static Oto ParseOto(string line) {
            const string format = "<wav>=<alias>,<offset>,<consonant>,<cutoff>,<preutter>,<overlap>";
            if (string.IsNullOrWhiteSpace(line)) {
                return null;
            }
            var parts = line.Split('=');
            if (parts.Length != 2) {
                throw new FileFormatException($"Line does not match format {format}.");
            }
            var wav = parts[0].Trim();
            parts = parts[1].Split(',');
            if (parts.Length != 6) {
                throw new FileFormatException($"Line does not match format {format}.");
            }
            var ext = Path.GetExtension(wav);
            var result = new Oto {
                Wav = wav,
                Alias = parts[0].Trim()
            };
            if (string.IsNullOrEmpty(result.Alias)) {
                result.Alias = wav.Replace(ext, "");
            }
            result.Phonetic = result.Alias;
            if (!ParseDouble(parts[1], out result.Offset)) {
                throw new FileFormatException($"Failed to parse offset. Format is {format}.");
            }
            if (!ParseDouble(parts[2], out result.Consonant)) {
                throw new FileFormatException($"Failed to parse consonant. Format is {format}.");
            }
            if (!ParseDouble(parts[3], out result.Cutoff)) {
                throw new FileFormatException($"Failed to parse cutoff. Format is {format}.");
            }
            if (!ParseDouble(parts[4], out result.Preutter)) {
                throw new FileFormatException($"Failed to parse preutter. Format is {format}.");
            }
            if (!ParseDouble(parts[5], out result.Overlap)) {
                throw new FileFormatException($"Failed to parse overlap. Format is {format}.");
            }
            return result;
        }

        static bool ParseDouble(string s, out double value) {
            if (string.IsNullOrEmpty(s)) {
                value = 0;
                return true;
            }
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}

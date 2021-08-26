using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenUtau.Classic {
    class VoicebankLoader {
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
                .Select(filePath => ParseCharacterTxt(filePath, Encoding.UTF8))
                .OrderByDescending(bank => bank.OrigFile.Length)
                .ToArray();
            var otoSets = Directory.EnumerateFiles(basePath, "oto.ini", SearchOption.AllDirectories)
                .Select(entry => ParseOtoSet(entry, Encoding.UTF8))
                .ToArray();
            var prefixMaps = Directory.EnumerateFiles(basePath, "prefix.map", SearchOption.AllDirectories)
                .Select(entry => ParsePrefixMap(entry, Encoding.UTF8))
                .ToArray();
            var bankDirs = banks
                .Select(bank => Path.GetDirectoryName(bank.OrigFile))
                .ToArray();
            foreach (var otoSet in otoSets) {
                var dir = Path.GetDirectoryName(otoSet.OrigFile);
                for (int i = 0; i < bankDirs.Length; ++i) {
                    if (dir.StartsWith(bankDirs[i])) {
                        otoSet.Name = PathUtils.MakeRelative(Path.GetDirectoryName(otoSet.OrigFile), bankDirs[i]);
                        banks[i].OtoSets.Add(otoSet);
                        break;
                    }
                }
            }
            foreach (var prefixMap in prefixMaps) {
                var dir = Path.GetDirectoryName(prefixMap.OrigFile);
                for (int i = 0; i < bankDirs.Length; ++i) {
                    if (dir.StartsWith(bankDirs[i])) {
                        banks[i].PrefixMap = prefixMap;
                        break;
                    }
                }
            }
            foreach (var bank in banks) {
                result.Add(bank.Id, bank);
            }
            return result;
        }

        Voicebank ParseCharacterTxt(string filePath, Encoding encoding) {
            using (var stream = File.OpenRead(filePath)) {
                using (var reader = new StreamReader(stream, encoding)) {
                    var voicebank = new Voicebank() {
                        File = filePath,
                        OrigFile = filePath,
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
                    if (string.IsNullOrEmpty(voicebank.Name)) {
                        throw new FileFormatException($"Failed to load {filePath} using encoding {encoding.EncodingName}");
                    }
                    voicebank.Id = PathUtils.MakeRelative(Path.GetDirectoryName(voicebank.File), basePath);
                    return voicebank;
                }
            }
        }

        static PrefixMap ParsePrefixMap(string filePath, Encoding encoding) {
            using (var stream = File.OpenRead(filePath)) {
                using (var reader = new StreamReader(stream, encoding)) {
                    var prefixMap = new PrefixMap {
                        File = filePath,
                        OrigFile = filePath,
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
        }

        static OtoSet ParseOtoSet(string filePath, Encoding encoding) {
            using (var stream = File.OpenRead(filePath)) {
                using (var reader = new StreamReader(stream, encoding)) {
                    var fileLoc = new FileLoc { file = filePath, lineNumber = 0 };
                    OtoSet otoSet = new OtoSet() {
                        File = filePath,
                        OrigFile = filePath,
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
                            throw new FileFormatException($"Failed to parse\n{fileLoc}", e);
                        }
                        fileLoc.line = null;
                        fileLoc.lineNumber++;
                    }
                    return otoSet;
                }
            }
        }

        static Oto ParseOto(string line) {
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
                OrigWav = wav,
                Wav = wav,
                Name = parts[0].Trim()
            };
            if (string.IsNullOrEmpty(result.Name)) {
                result.Name = wav.Replace(ext, "");
            }
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

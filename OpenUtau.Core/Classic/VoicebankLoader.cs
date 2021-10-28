using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OpenUtau.Core;
using Serilog;

namespace OpenUtau.Classic {
    public class VoicebankLoader {
        public const string kCharTxt = "character.txt";
        public const string kCharYaml = "character.yaml";
        public const string kOtoIni = "oto.ini";

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
            var banks = Directory.EnumerateFiles(basePath, kCharTxt, SearchOption.AllDirectories)
                .Select(filePath => LoadVoicebank(filePath, basePath))
                .OfType<Voicebank>()
                .ToArray();
            foreach (var bank in banks) {
                result.Add(bank.Id, bank);
            }
            return result;
        }

        public static Voicebank LoadVoicebank(string filePath, string basePath) {
            try {
                var voicebank = new Voicebank();
                ParseCharacterTxt(voicebank, filePath, basePath);
                LoadOtoSets(voicebank, Path.GetDirectoryName(voicebank.File));
                return voicebank;
            } catch (Exception e) {
                Log.Error(e, $"Failed to load {filePath}");
            }
            return null;
        }

        public static void ReloadVoicebank(Voicebank voicebank) {
            var filePath = voicebank.File;
            var basePath = voicebank.BasePath;
            voicebank.Reset();
            ParseCharacterTxt(voicebank, filePath, basePath);
            LoadOtoSets(voicebank, Path.GetDirectoryName(voicebank.File));
        }

        public static void LoadOtoSets(Voicebank voicebank, string dirPath) {
            var otoFile = Path.Combine(dirPath, kOtoIni);
            if (File.Exists(otoFile)) {
                var otoSet = ParseOtoSet(otoFile, voicebank.TextFileEncoding);
                var voicebankDir = Path.GetDirectoryName(voicebank.File);
                otoSet.Name = Path.GetRelativePath(voicebankDir, dirPath);
                if (otoSet.Name == ".") {
                    otoSet.Name = string.Empty;
                }
                voicebank.OtoSets.Add(otoSet);
            }
            var dirs = Directory.GetDirectories(dirPath);
            foreach (var dir in dirs) {
                var charTxt = Path.Combine(dir, kCharTxt);
                if (File.Exists(charTxt)) {
                    continue;
                }
                LoadOtoSets(voicebank, dir);
            }
        }

        public static void ParseCharacterTxt(Voicebank voicebank, string filePath, string basePath) {
            var dir = Path.GetDirectoryName(filePath);
            var yamlFile = Path.Combine(dir, kCharYaml);
            VoicebankConfig bankConfig = null;
            if (File.Exists(yamlFile)) {
                try {
                    using (var stream = File.OpenRead(yamlFile)) {
                        bankConfig = VoicebankConfig.Load(stream);
                    }
                } catch (Exception e) {
                    Log.Error(e, $"Failed to load yaml {yamlFile}");
                }
            }
            Encoding encoding = Encoding.GetEncoding("shift_jis");
            if (!string.IsNullOrEmpty(bankConfig?.TextFileEncoding)) {
                encoding = Encoding.GetEncoding(bankConfig.TextFileEncoding);
            }
            using (var stream = File.OpenRead(filePath)) {
                ParseCharacterTxt(voicebank, stream, filePath, basePath, encoding);
            }
            if (bankConfig != null) {
                ApplyConfig(voicebank, bankConfig);
            }
            if (voicebank.Subbanks.Count == 0) {
                LoadPrefixMap(voicebank);
            }
            if (voicebank.Subbanks.Count == 0) {
                voicebank.Subbanks.Add(new Subbank());
            }
        }

        public static void ParseCharacterTxt(Voicebank voicebank, Stream stream, string filePath, string basePath, Encoding encoding) {
            using (var reader = new StreamReader(stream, encoding)) {
                voicebank.BasePath = basePath;
                voicebank.File = filePath;
                voicebank.TextFileEncoding = encoding;
                var otherLines = new List<string>();
                while (!reader.EndOfStream) {
                    string line = reader.ReadLine().Trim();
                    var s = line.Split(new char[] { '=' });
                    if (s.Length < 2) {
                        s = line.Split(new char[] { ':' });
                    }
                    if (s.Length < 2) {
                        s = line.Split(new char[] { '：' });
                    }
                    Array.ForEach(s, temp => temp.Trim());
                    if (s.Length == 2) {
                        s[0] = s[0].ToLowerInvariant();
                        if (s[0] == "name") {
                            voicebank.Name = s[1];
                        } else if (s[0] == "名前" && string.IsNullOrEmpty(voicebank.Name)) {
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
            }
        }

        public static void ApplyConfig(Voicebank bank, VoicebankConfig bankConfig) {
            if (!string.IsNullOrWhiteSpace(bankConfig.Name)) {
                bank.Name = bankConfig.Name;
            }
            if (!string.IsNullOrWhiteSpace(bankConfig.Image)) {
                bank.Image = bankConfig.Image;
            }
            if (!string.IsNullOrWhiteSpace(bankConfig.Portrait)) {
                bank.Portrait = bankConfig.Portrait;
                bank.PortraitOpacity = bankConfig.PortraitOpacity;
            }
            if (!string.IsNullOrWhiteSpace(bankConfig.Author)) {
                bank.Author = bankConfig.Author;
            }
            if (!string.IsNullOrWhiteSpace(bankConfig.Web)) {
                bank.Web = bankConfig.Web;
            }
            if (bankConfig.Subbanks != null && bankConfig.Subbanks.Length > 0) {
                foreach (var subbank in bankConfig.Subbanks) {
                    subbank.Prefix ??= string.Empty;
                    subbank.Suffix ??= string.Empty;
                }
                bank.Subbanks.AddRange(bankConfig.Subbanks);
            }
        }

        public static void LoadPrefixMap(Voicebank voicebank) {
            var dir = Path.GetDirectoryName(voicebank.File);
            var filePath = Path.Combine(dir, "prefix.map");
            if (!File.Exists(filePath)) {
                return;
            }
            try {
                using (var stream = File.OpenRead(filePath)) {
                    var map = ParsePrefixMap(stream, voicebank.TextFileEncoding);
                    foreach (var kv in map) {
                        var subbank = new Subbank() {
                            Prefix = kv.Key.Item1,
                            Suffix = kv.Key.Item2,
                        };
                        var toneRanges = new List<string>();
                        int? rangeStart = null;
                        int? rangeEnd = null;
                        for (int i = 24; i <= 108; i++) {
                            if (kv.Value.Contains(i) && i < 108) {
                                if (rangeStart == null) {
                                    rangeStart = i;
                                } else {
                                    rangeEnd = i;
                                }
                            } else if (rangeStart != null) {
                                if (rangeEnd != null) {
                                    toneRanges.Add($"{MusicMath.GetToneName(rangeStart.Value)}-{MusicMath.GetToneName(rangeEnd.Value)}");
                                } else {
                                    toneRanges.Add($"{MusicMath.GetToneName(rangeStart.Value)}");
                                }
                                rangeStart = null;
                                rangeEnd = null;
                            }
                        }
                        subbank.ToneRanges = toneRanges.ToArray();
                        voicebank.Subbanks.Add(subbank);
                    }
                }
            } catch (Exception e) {
                Log.Error(e, $"Failed to load {filePath}");
            }
        }

        public static Dictionary<Tuple<string, string>, SortedSet<int>> ParsePrefixMap(Stream stream, Encoding encoding) {
            using (var reader = new StreamReader(stream, encoding)) {
                var result = new Dictionary<Tuple<string, string>, SortedSet<int>>();
                while (!reader.EndOfStream) {
                    var s = reader.ReadLine().Split('\t');
                    if (s.Length == 3) {
                        int tone = MusicMath.NameToTone(s[0]);
                        var key = Tuple.Create(s[1], s[2]);
                        if (!result.TryGetValue(key, out var tones)) {
                            tones = new SortedSet<int>();
                            result[key] = tones;
                        }
                        tones.Add(tone);
                    }
                }
                return result;
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

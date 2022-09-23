using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Core.Enunu {
    public class EnunuSinger : USinger {
        public override string Id => voicebank.Id;
        public override string Name => voicebank.Name;
        public override USingerType SingerType => voicebank.SingerType;
        public override string BasePath => voicebank.BasePath;
        public override string Author => voicebank.Author;
        public override string Voice => voicebank.Voice;
        public override string Location => Path.GetDirectoryName(voicebank.File);
        public override string Web => voicebank.Web;
        public override string Version => voicebank.Version;
        public override string OtherInfo => voicebank.OtherInfo;
        public override IList<string> Errors => errors;
        public override string Avatar => voicebank.Image == null ? null : Path.Combine(Location, voicebank.Image);
        public override byte[] AvatarData => avatarData;
        public override string Portrait => voicebank.Portrait == null ? null : Path.Combine(Location, voicebank.Portrait);
        public override float PortraitOpacity => voicebank.PortraitOpacity;
        public override string DefaultPhonemizer => voicebank.DefaultPhonemizer;
        public override Encoding TextFileEncoding => voicebank.TextFileEncoding;
        public override IList<USubbank> Subbanks => subbanks;

        Voicebank voicebank;
        EnunuConfig enuconfig;
        List<string> errors = new List<string>();
        List<USubbank> subbanks = new List<USubbank>();

        HashSet<string> phonemes = new HashSet<string>();
        HashSet<string> timbres = new HashSet<string>();
        Dictionary<string, string[]> table = new Dictionary<string, string[]>();

        public byte[] avatarData;

        public EnunuSinger(Voicebank voicebank) {
            this.voicebank = voicebank;
            found = true;
        }

        public override void EnsureLoaded() {
            if (Loaded) {
                return;
            }
            Reload();
        }

        public override void Reload() {
            if (!Found) {
                return;
            }
            try {
                voicebank.Reload();
                Load();
                loaded = true;
            } catch (Exception e) {
                Log.Error(e, $"Failed to load {voicebank.File}");
            }
        }

        void Load() {
            enuconfig = EnunuConfig.Load(this);

            phonemes.Clear();
            timbres.Clear();
            table.Clear();
            try {
                var tablePath = Path.Join(Location, enuconfig.tablePath);
                foreach (var line in File.ReadAllLines(tablePath)) {
                    var parts = line.Trim().Split();
                    table[parts[0]] = parts.Skip(1).ToArray();
                    foreach (var phoneme in table[parts[0]]) {
                        //phonemes.Add(phoneme);
                    }
                }
            } catch (Exception e) {
                Log.Error(e, $"Failed to load table for {Name}");
            }
            try {
                var hedPath = Path.Join(Location, enuconfig.questionPath);
                var pattern = new Regex("^\\s*QS\\s+\\\"(.*)\\\"\\s+\\{(.*)}");
                foreach (var line in File.ReadAllLines(hedPath)) {
                    var m = pattern.Match(line);
                    if (!m.Success) {
                        continue;
                    }
                    foreach (var p in m.Groups[2].Value.Split(',')) {
                        var value = p.Trim();
                        if (value.StartsWith("*^") && value.EndsWith("-*") ||
                            value.StartsWith("*-") && value.EndsWith("+*") ||
                            value.StartsWith("*+") && value.EndsWith("=*")) {
                            phonemes.Add(value.Substring(2, value.Length - 4));
                        } else if (value.StartsWith("*^") && value.EndsWith("_*")) {
                            timbres.Add(value.Substring(2, value.Length - 4));
                        }
                    }
                }
            } catch (Exception e) {
                Log.Error(e, $"Failed to load hed for {Name}");
            }

            subbanks.Clear();
            if (voicebank.Subbanks == null || voicebank.Subbanks.Count == 0 ||
                voicebank.Subbanks.Count == 1 && string.IsNullOrEmpty(voicebank.Subbanks[0].Color)) {
                subbanks.Add(new USubbank(new Subbank() {
                    Prefix = string.Empty,
                    Suffix = string.Empty,
                    ToneRanges = new[] { "C1-B7" },
                }));
                subbanks.AddRange(timbres.Select(flag => new USubbank(new Subbank() {
                    Color = flag,
                    Prefix = string.Empty,
                    Suffix = flag,
                    ToneRanges = new[] { "C1-B7" },
                })));
            } else {
                subbanks.AddRange(voicebank.Subbanks
                    .Select(subbank => new USubbank(subbank)));
            }

            if (Avatar != null && File.Exists(Avatar)) {
                try {
                    using (var stream = new FileStream(Avatar, FileMode.Open)) {
                        using (var memoryStream = new MemoryStream()) {
                            stream.CopyTo(memoryStream);
                            avatarData = memoryStream.ToArray();
                        }
                    }
                } catch (Exception e) {
                    avatarData = null;
                    Log.Error(e, "Failed to load avatar data.");
                }
            } else {
                avatarData = null;
                Log.Error("Avatar can't be found");
            }
        }

        public override bool TryGetOto(string phoneme, out UOto oto) {
            var parts = phoneme.Split();
            if (parts.All(p => phonemes.Contains(p))) {
                oto = UOto.OfDummy(phoneme);
                return true;
            }
            oto = null;
            return false;
        }

        public override IEnumerable<UOto> GetSuggestions(string text) {
            if (text != null) {
                text = text.ToLowerInvariant().Replace(" ", "");
            }
            bool all = string.IsNullOrEmpty(text);
            return table.Keys
                .Where(key => all || key.Contains(text))
                .Select(key => UOto.OfDummy(key));
        }

        public override byte[] LoadPortrait() {
            return string.IsNullOrEmpty(Portrait)
                ? null
                : File.ReadAllBytes(Portrait);
        }
    }
}

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
    class EnunuSinger : USinger {
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
        public override Encoding TextFileEncoding => voicebank.TextFileEncoding;
        public override IList<USubbank> Subbanks => subbanks;
        public override Dictionary<string, UOto> Otos => otos;

        Voicebank voicebank;
        EnunuConfig enuconfig;
        List<string> errors = new List<string>();
        List<USubbank> subbanks = new List<USubbank>();
        Dictionary<string, UOto> otos = new Dictionary<string, UOto>();

        HashSet<string> phonemes = new HashSet<string>();
        Dictionary<string, string[]> table = new Dictionary<string, string[]>();

        public byte[] avatarData;

        public EnunuSinger(Voicebank voicebank) {
            this.voicebank = voicebank;
            enuconfig = EnunuConfig.Load(this);

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
                var pattern = new Regex(@"\{(.*)\}");
                foreach (var line in File.ReadAllLines(hedPath)) {
                    if (!line.StartsWith("QS ")) {
                        continue;
                    }
                    var m = pattern.Match(line);
                    if (!m.Success) {
                        continue;
                    }
                    var g = m.Groups[1].Value;
                    foreach (var p in g.Split(',')) {
                        var exp = p.Trim();
                        if (exp.StartsWith("*^") && exp.EndsWith("-*") ||
                            exp.StartsWith("*-") && exp.EndsWith("+*") ||
                            exp.StartsWith("*+") && exp.EndsWith("=*")) {
                            phonemes.Add(exp.Substring(2, exp.Length - 4));
                        }
                    }
                }
            } catch (Exception e) {
                Log.Error(e, $"Failed to load hed for {Name}");
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

            found = true;
            loaded = true;
        }

        public override bool TryGetMappedOto(string phoneme, int tone, out UOto oto) {
            var parts = phoneme.Split();
            if (parts.All(p => phonemes.Contains(p) || table.ContainsKey(p))) {
                oto = new UOto() {
                    Alias = phoneme,
                    Phonetic = phoneme,
                };
                return true;
            }
            oto = null;
            return false;
        }

        public override bool TryGetMappedOto(string phoneme, int tone, string color, out UOto oto) {
            return TryGetMappedOto(phoneme, tone, out oto);
        }

        public override IEnumerable<UOto> GetSuggestions(string text) {
            if (text != null) {
                text = text.ToLowerInvariant().Replace(" ", "");
            }
            bool all = string.IsNullOrEmpty(text);
            return table.Keys
                .Where(key => all || key.Contains(text))
                .Select(key => new UOto() {
                    Alias = key,
                    Phonetic = key,
                });
        }

        public override byte[] LoadPortrait() {
            return string.IsNullOrEmpty(Portrait)
                ? null
                : File.ReadAllBytes(Portrait);
        }
    }
}

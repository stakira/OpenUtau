using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenUtau.Core.Ustx;
using Serilog;
using WanaKanaNet;

namespace OpenUtau.Classic {
    public class ClassicSinger : USinger {
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
        List<string> errors = new List<string>();
        byte[] avatarData;
        List<UOtoSet> OtoSets = new List<UOtoSet>();
        Dictionary<string, UOto> otos = new Dictionary<string, UOto>();
        Dictionary<string, List<UOto>> phonetics;
        List<USubbank> subbanks = new List<USubbank>();

        public ClassicSinger(Voicebank voicebank) {
            this.voicebank = voicebank;
            found = true;
        }

        public override void EnsureLoaded() {
            if (!Found || Loaded) {
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

            subbanks.Clear();
            subbanks.AddRange(voicebank.Subbanks
                .OrderByDescending(subbank => subbank.Prefix.Length + subbank.Suffix.Length)
                .Select(subbank => new USubbank(subbank)));
            var patterns = subbanks.Select(subbank => new Regex($"^{Regex.Escape(subbank.Prefix)}(.*){Regex.Escape(subbank.Suffix)}$"))
                .ToList();

            var dummy = new USubbank(new Subbank());
            OtoSets.Clear();
            otos.Clear();
            errors.Clear();
            foreach (var otoSet in voicebank.OtoSets) {
                var uSet = new UOtoSet(otoSet, voicebank.BasePath);
                OtoSets.Add(uSet);
                foreach (var oto in otoSet.Otos) {
                    UOto? uOto = null;
                    for (var i = 0; i < patterns.Count; i++) {
                        var m = patterns[i].Match(oto.Alias);
                        if (m.Success) {
                            oto.Phonetic = m.Groups[1].Value;
                            uOto = new UOto(oto, uSet, subbanks[i]);
                            break;
                        }
                    }
                    if (uOto == null) {
                        uOto = new UOto(oto, uSet, dummy);
                    }
                    if (!otos.ContainsKey(oto.Alias)) {
                        otos.Add(oto.Alias, uOto);
                    } else {
                        //Errors.Add($"oto conflict {Otos[oto.Alias].Set}/{oto.Alias} and {otoSet.Name}/{oto.Alias}");
                    }
                }
                errors.AddRange(otoSet.Errors);
            }
            phonetics = otos.Values
                .GroupBy(oto => oto.Phonetic, oto => oto)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(oto => oto.Prefix.Length + oto.Suffix.Length).ToList());

            Task.Run(() => {
                otos.Values
                    .ToList()
                    .ForEach(oto => {
                        oto.SearchTerms.Add(oto.Alias.ToLowerInvariant().Replace(" ", ""));
                        oto.SearchTerms.Add(WanaKana.ToRomaji(oto.Alias).ToLowerInvariant().Replace(" ", ""));
                    });
            });
        }

        public override bool TryGetMappedOto(string phoneme, int tone, out UOto oto) {
            oto = default;
            var subbank = subbanks.Find(subbank => string.IsNullOrEmpty(subbank.Color) && subbank.toneSet.Contains(tone));
            if (subbank != null && otos.TryGetValue($"{subbank.Prefix}{phoneme}{subbank.Suffix}", out oto)) {
                return true;
            }
            if (otos.TryGetValue(phoneme, out oto)) {
                return true;
            }
            return false;
        }

        public override bool TryGetMappedOto(string phoneme, int tone, string color, out UOto oto) {
            oto = default;
            var subbank = subbanks.Find(subbank => subbank.Color == color && subbank.toneSet.Contains(tone));
            if (subbank != null && otos.TryGetValue($"{subbank.Prefix}{phoneme}{subbank.Suffix}", out oto)) {
                return true;
            }
            return TryGetMappedOto(phoneme, tone, out oto);
        }

        public override IEnumerable<UOto> GetSuggestions(string text) {
            if (text != null) {
                text = text.ToLowerInvariant().Replace(" ", "");
            }
            bool all = string.IsNullOrEmpty(text);
            return otos.Values
                .Where(oto => all || oto.SearchTerms.Exists(term => term.Contains(text)));
        }

        public override byte[] LoadPortrait() {
            return string.IsNullOrEmpty(Portrait)
                ? null
                : File.ReadAllBytes(Portrait);
        }
    }
}

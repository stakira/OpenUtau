using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenUtau.Classic;
using WanaKanaNet;

namespace OpenUtau.Core.Ustx {
    public struct UOto {
        public string Alias => oto.Alias;
        public string Phonetic => oto.Phonetic;
        public string Set => set.Name;
        public string Prefix => set.Prefix;
        public string Suffix => set.Suffix;
        public string Flavor => set.Flavor;
        public string File { private set; get; }
        public string DisplayFile => oto.Wav;
        public double Offset => oto.Offset;
        public double Consonant => oto.Consonant;
        public double Cutoff => oto.Cutoff;
        public double Preutter => oto.Preutter;
        public double Overlap => oto.Overlap;
        public List<string> SearchTerms { private set; get; }

        private readonly Oto oto;
        private readonly UOtoSet set;

        public UOto(Oto oto, UOtoSet set) {
            this.oto = oto;
            this.set = set;
            File = Path.Combine(set.Location, oto.Wav);
            SearchTerms = new List<string>();
        }

        public override string ToString() => Alias;
    }

    public class UOtoSet {
        public string Name => otoSet.Name;
        public string Prefix => otoSet.Prefix;
        public string Suffix => otoSet.Suffix;
        public string Flavor => otoSet.Flavor;
        public readonly string Location;
        public readonly Dictionary<string, List<UOto>> Otos;
        public readonly List<string> Errors;

        private readonly OtoSet otoSet;

        public UOtoSet(OtoSet otoSet, USinger singer, string singersPath) {
            this.otoSet = otoSet;
            Location = Path.Combine(singersPath, Path.GetDirectoryName(otoSet.File));
            Otos = new Dictionary<string, List<UOto>>();
            foreach (var oto in otoSet.Otos) {
                if (!Otos.ContainsKey(oto.Alias)) {
                    Otos.Add(oto.Alias, new List<UOto>());
                }
                Otos[oto.Alias].Add(new UOto(oto, this));
            }
            Errors = otoSet.Errors;
        }

        public override string ToString() => Name;

        public bool StripPrefixSuffix(string str, out string result) {
            result = str;
            if (!string.IsNullOrEmpty(Prefix) && result.StartsWith(Prefix)) {
                result = result.Substring(Prefix.Length);
            }
            if (!string.IsNullOrEmpty(Suffix) && result.EndsWith(Suffix)) {
                result = result.Substring(0, result.Length - Suffix.Length);
            }
            return result != str;
        }

        public string ApplyPrefixSuffix(string str) {
            if (!string.IsNullOrEmpty(Prefix)) {
                str = $"{Prefix}{str}";
            }
            if (!string.IsNullOrEmpty(Suffix)) {
                str = $"{str}{Suffix}";
            }
            return str;
        }
    }

    public class USinger {
        public readonly string Name;
        public readonly string Location;
        public readonly string Author;
        public readonly string Web;
        public readonly string OtherInfo;
        public readonly string Avatar;
        public readonly Dictionary<string, Tuple<string, string>> PrefixMap;
        public readonly List<UOtoSet> OtoSets;
        public readonly string Id;
        public readonly bool Loaded;

        public string DisplayName { get { return Loaded ? Name : $"{Name}[Unloaded]"; } }

        public USinger(string name) {
            Name = name;
            Loaded = false;
        }

        public USinger(Voicebank voicebank, string singersPath) {
            Name = voicebank.Name;
            Author = voicebank.Author;
            Web = voicebank.Web;
            OtherInfo = voicebank.OtherInfo;
            Location = Path.GetDirectoryName(voicebank.File);
            if (!string.IsNullOrEmpty(voicebank.Image)) {
                Avatar = Path.Combine(Location, voicebank.Image);
            }
            if (voicebank.PrefixMap != null) {
                PrefixMap = voicebank.PrefixMap.Map;
            } else {
                PrefixMap = new Dictionary<string, Tuple<string, string>>();
            }
            OtoSets = new List<UOtoSet>();
            foreach (var otoSet in voicebank.OtoSets) {
                OtoSets.Add(new UOtoSet(otoSet, this, singersPath));
            }
            Id = voicebank.Id;
            Loaded = true;

            Task.Run(() => {
                OtoSets
                    .SelectMany(set => set.Otos.Values)
                    .SelectMany(otos => otos)
                    .ToList()
                    .ForEach(oto => {
                        oto.SearchTerms.Add(oto.Alias.ToLowerInvariant().Replace(" ", ""));
                        oto.SearchTerms.Add(WanaKana.ToRomaji(oto.Alias).ToLowerInvariant().Replace(" ", ""));
                    });
            });
        }

        public bool TryGetOto(string phoneme, out UOto oto) {
            oto = default;
            foreach (var set in OtoSets) {
                if (set.Otos.TryGetValue(phoneme, out var list)) {
                    oto = list[0];
                    return true;
                }
            }
            return false;
        }

        public bool TryGetMappedOto(string phoneme, int tone, out UOto oto) {
            oto = default;
            string toneName = MusicMath.GetToneName(tone);
            if (PrefixMap.TryGetValue(toneName, out var mapped)) {
                string phonemeMapped = mapped.Item1 + phoneme + mapped.Item2;
                if (TryGetOto(phonemeMapped, out oto)) {
                    return true;
                }
            }
            if (TryGetOto(phoneme, out oto)) {
                return true;
            }
            return false;
        }

        public UOto? FindOto(string phoneme) {
            foreach (var set in OtoSets) {
                if (set.Otos.TryGetValue(phoneme, out var list)) {
                    return list[0];
                }
            }
            return null;
        }

        public void GetSuggestions(string text, Action<string> provide) {
            if (text != null) {
                text = text.ToLowerInvariant().Replace(" ", "");
            }
            bool all = string.IsNullOrEmpty(text);
            OtoSets
                .SelectMany(set => set.Otos.Values)
                .SelectMany(list => list)
                .Where(oto => all || oto.SearchTerms.Exists(term => term.Contains(text)))
                .ToList()
                .ForEach(oto => provide(oto.Alias));
        }

        public override string ToString() => Name;
    }
}

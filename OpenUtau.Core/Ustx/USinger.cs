using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenUtau.Classic;
using Serilog;
using WanaKanaNet;

namespace OpenUtau.Core.Ustx {
    public struct UOto {
        public string Alias => oto.Alias;
        public string Phonetic => oto.Phonetic;
        public string Set => set.Name;
        public string Color => subbank.Color;
        public string Prefix => subbank.Prefix;
        public string Suffix => subbank.Suffix;
        public SortedSet<int> ToneSet => subbank.toneSet;
        public string File => Path.Combine(set.Location, oto.Wav);
        public string DisplayFile => oto?.Wav;
        public double Offset => oto.Offset;
        public double Consonant => oto.Consonant;
        public double Cutoff => oto.Cutoff;
        public double Preutter => oto.Preutter;
        public double Overlap => oto.Overlap;
        public List<string> SearchTerms { private set; get; }

        private readonly Oto oto;
        private readonly UOtoSet set;
        private readonly USubbank subbank;

        public UOto(Oto oto, UOtoSet set, USubbank subbank) {
            this.oto = oto;
            this.set = set;
            this.subbank = subbank;
            SearchTerms = new List<string>();
        }

        public override string ToString() => Alias;
    }

    public class UOtoSet {
        public string Name => otoSet.Name;
        public readonly string Location;

        private readonly OtoSet otoSet;

        public UOtoSet(OtoSet otoSet, string singersPath) {
            this.otoSet = otoSet;
            Location = Path.Combine(singersPath, Path.GetDirectoryName(otoSet.File));
        }

        public override string ToString() => Name;
    }

    public class USubbank {
        public string Color {
            get => subbank.Color;
            set => subbank.Color = value;
        }
        public string Prefix {
            get => subbank.Prefix;
            set => subbank.Prefix = value;
        }
        public string Suffix {
            get => subbank.Suffix;
            set => subbank.Suffix = value;
        }
        public string ToneRangesString {
            get => toneRangesString;
            set {
                subbank.ToneRanges = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                toneRangesString = value;
            }
        }

        public readonly SortedSet<int> toneSet;
        public readonly Subbank subbank;

        private string toneRangesString;

        public USubbank(Subbank subbank) {
            this.subbank = subbank;
            toneSet = new SortedSet<int>();
            if (subbank.ToneRanges != null) {
                toneRangesString = string.Join(',', subbank.ToneRanges);
                foreach (var range in subbank.ToneRanges) {
                    AddToneRange(range, toneSet);
                }
            } else {
                toneRangesString = string.Empty;
            }
        }

        private static void AddToneRange(string range, SortedSet<int> set) {
            var parts = range.Split('-');
            if (parts.Length == 1) {
                int tone = MusicMath.NameToTone(parts[0]);
                if (tone > 0) {
                    set.Add(tone);
                }
            } else if (parts.Length == 2) {
                int start = MusicMath.NameToTone(parts[0]);
                int end = MusicMath.NameToTone(parts[1]);
                if (start > 0 && end > 0) {
                    for (int i = start; i <= end; ++i) {
                        set.Add(i);
                    }
                }
            }
        }
    }

    public class USinger {
        public string BasePath => Voicebank.BasePath;
        public readonly string Name;
        public readonly string Location;
        public readonly string Author;
        public readonly string Web;
        public readonly string OtherInfo;
        public readonly string Avatar;
        public readonly byte[] AvatarData;
        public readonly string Portrait;
        public readonly float PortraitOpacity;
        public readonly Encoding TextFileEncoding;
        public readonly List<UOtoSet> OtoSets;
        public readonly string Id;
        public readonly bool Loaded;
        public readonly Voicebank Voicebank;
        public readonly Dictionary<string, UOto> Otos;
        public readonly Dictionary<string, List<UOto>> Phonetics;
        public readonly List<string> Errors;
        public readonly List<USubbank> Subbanks;

        public string DisplayName { get { return Loaded ? Name : $"[Missing] {Name}"; } }

        public USinger(string name) {
            Name = name;
            Loaded = false;
        }

        public USinger(Voicebank voicebank) {
            Voicebank = voicebank;
            Id = voicebank.Id;
            Name = voicebank.Name;
            Author = voicebank.Author;
            Web = voicebank.Web;
            OtherInfo = voicebank.OtherInfo;
            Location = Path.GetDirectoryName(voicebank.File);
            if (!string.IsNullOrEmpty(voicebank.Image)) {
                Avatar = Path.Combine(Location, voicebank.Image);
                if (File.Exists(Avatar)) {
                    try {
                        using (var stream = new FileStream(Avatar, FileMode.Open)) {
                            using (var memoryStream = new MemoryStream()) {
                                stream.CopyTo(memoryStream);
                                AvatarData = memoryStream.ToArray();
                            }
                        }
                    } catch (Exception e) {
                        AvatarData = null;
                        Log.Error(e, "Failed to load avatar data.");
                    }
                } else {
                    AvatarData = null;
                    Log.Error("Avatar can't be found");
                }
            }
            if (!string.IsNullOrEmpty(voicebank.Portrait)) {
                Portrait = Path.Combine(Location, voicebank.Portrait);
                PortraitOpacity = voicebank.PortraitOpacity;
            }
            TextFileEncoding = voicebank.TextFileEncoding;

            Subbanks = new List<Subbank>(voicebank.Subbanks)
                .OrderByDescending(subbank => subbank.Prefix.Length + subbank.Suffix.Length)
                .Select(subbank => new USubbank(subbank))
                .ToList();
            var patterns = Subbanks.Select(subbank => new Regex($"^{Regex.Escape(subbank.Prefix)}(.*){Regex.Escape(subbank.Suffix)}$"))
                .ToList();

            var dummy = new USubbank(new Subbank());
            OtoSets = new List<UOtoSet>();
            Otos = new Dictionary<string, UOto>();
            Errors = new List<string>();
            foreach (var otoSet in voicebank.OtoSets) {
                var uSet = new UOtoSet(otoSet, voicebank.BasePath);
                OtoSets.Add(uSet);
                foreach (var oto in otoSet.Otos) {
                    UOto? uOto = null;
                    for (var i = 0; i < patterns.Count; i++) {
                        var m = patterns[i].Match(oto.Alias);
                        if (m.Success) {
                            oto.Phonetic = m.Groups[1].Value;
                            uOto = new UOto(oto, uSet, Subbanks[i]);
                            break;
                        }
                    }
                    if (uOto == null) {
                        uOto = new UOto(oto, uSet, dummy);
                    }
                    if (!Otos.ContainsKey(oto.Alias)) {
                        Otos.Add(oto.Alias, uOto.Value);
                    } else {
                        //Errors.Add($"oto conflict {Otos[oto.Alias].Set}/{oto.Alias} and {otoSet.Name}/{oto.Alias}");
                    }
                }
                Errors.AddRange(otoSet.Errors);
            }
            Phonetics = Otos.Values
                .GroupBy(oto => oto.Phonetic, oto => oto)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(oto => oto.Prefix.Length + oto.Suffix.Length).ToList());
            Loaded = true;

            Task.Run(() => {
                Otos.Values
                    .ToList()
                    .ForEach(oto => {
                        oto.SearchTerms.Add(oto.Alias.ToLowerInvariant().Replace(" ", ""));
                        oto.SearchTerms.Add(WanaKana.ToRomaji(oto.Alias).ToLowerInvariant().Replace(" ", ""));
                    });
            });
        }

        [Obsolete("Use the overload with color instead.")]
        public bool TryGetMappedOto(string phoneme, int tone, out UOto oto) {
            oto = default;
            var subbank = Subbanks.Find(subbank => subbank.toneSet.Contains(tone) && string.IsNullOrEmpty(subbank.Color));
            if (subbank != null && Otos.TryGetValue($"{subbank.Prefix}{phoneme}{subbank.Suffix}", out oto)) {
                return true;
            }
            if (Otos.TryGetValue(phoneme, out oto)) {
                return true;
            }
            return false;
        }

        public bool TryGetMappedOto(string phoneme, int tone, string color, out UOto oto) {
            oto = default;
            var subbank = Subbanks.Find(subbank => subbank.toneSet.Contains(tone) && color == subbank.Color);
            if (subbank != null && Otos.TryGetValue($"{subbank.Prefix}{phoneme}{subbank.Suffix}", out oto)) {
                return true;
            }
            return TryGetMappedOto(phoneme, tone, out oto);
        }

        public void GetSuggestions(string text, Action<UOto> provide) {
            if (text != null) {
                text = text.ToLowerInvariant().Replace(" ", "");
            }
            bool all = string.IsNullOrEmpty(text);
            Otos.Values
                .Where(oto => all || oto.SearchTerms.Exists(term => term.Contains(text)))
                .ToList()
                .ForEach(oto => provide(oto));
        }

        public override string ToString() => Name;
    }
}

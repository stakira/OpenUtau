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
        public string Id => Voicebank.Id;
        public string Name => Voicebank.Name;
        public string BasePath => Voicebank.BasePath;
        public string Author => Voicebank.Author;
        public string Location => Path.GetDirectoryName(Voicebank.File);
        public string Web => Voicebank.Web;
        public string Version => Voicebank.Version;
        public string OtherInfo => Voicebank.OtherInfo;
        public string Avatar => Voicebank.Image == null ? null : Path.Combine(Location, Voicebank.Image);
        public string Portrait => Voicebank.Portrait == null ? null : Path.Combine(Location, Voicebank.Portrait);
        public float PortraitOpacity => Voicebank.PortraitOpacity;
        public Encoding TextFileEncoding => Voicebank.TextFileEncoding;
        public byte[] AvatarData;
        public List<UOtoSet> OtoSets = new List<UOtoSet>();
        public bool Found;
        public bool Loaded;
        public Voicebank Voicebank;
        public Dictionary<string, UOto> Otos = new Dictionary<string, UOto>();
        public Dictionary<string, List<UOto>> Phonetics;
        public List<string> Errors = new List<string>();
        public List<USubbank> Subbanks = new List<USubbank>();

        public string DisplayName { get { return Found ? Name : $"[Missing] {Name}"; } }

        public USinger(string name) {
            Voicebank = new Voicebank() { Name = name };
            Found = false;
        }

        public USinger(Voicebank voicebank) {
            Voicebank = voicebank;
            Found = true;
        }

        public void EnsureLoaded() {
            if (!Found || Loaded) {
                return;
            }
            try {
                Voicebank.Reload();
                Load();
                Loaded = true;
            } catch (Exception e) {
                Log.Error(e, $"Failed to load {Voicebank.File}");
            }
        }

        public void Reload() {
            if (!Found) {
                return;
            }
            try {
                Voicebank.Reload();
                Load();
                Loaded = true;
            } catch (Exception e) {
                Log.Error(e, $"Failed to load {Voicebank.File}");
            }
        }

        public void Load() {
            if (Avatar != null && File.Exists(Avatar)) {
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

            Subbanks.Clear();
            Subbanks.AddRange(Voicebank.Subbanks
                .OrderByDescending(subbank => subbank.Prefix.Length + subbank.Suffix.Length)
                .Select(subbank => new USubbank(subbank)));
            var patterns = Subbanks.Select(subbank => new Regex($"^{Regex.Escape(subbank.Prefix)}(.*){Regex.Escape(subbank.Suffix)}$"))
                .ToList();

            var dummy = new USubbank(new Subbank());
            OtoSets.Clear();
            Otos.Clear();
            Errors.Clear();
            foreach (var otoSet in Voicebank.OtoSets) {
                var uSet = new UOtoSet(otoSet, Voicebank.BasePath);
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

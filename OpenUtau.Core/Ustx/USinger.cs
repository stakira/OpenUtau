using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenUtau.Classic;

namespace OpenUtau.Core.Ustx {
    public struct UOto {
        public string Alias;
        public string Phonetic;
        public string Set;
        public string Color;
        public string Prefix;
        public string Suffix;
        public SortedSet<int> ToneSet;
        public string File;
        public string DisplayFile;
        public double Offset;
        public double Consonant;
        public double Cutoff;
        public double Preutter;
        public double Overlap;
        public List<string> SearchTerms { private set; get; }

        public UOto(Oto oto, UOtoSet set, USubbank subbank) {
            Alias = oto.Alias;
            Phonetic = oto.Phonetic;
            Set = set.Name;
            Color = subbank?.Color;
            Prefix = subbank?.Prefix;
            Suffix = subbank?.Suffix;
            ToneSet = subbank?.toneSet;
            File = Path.Combine(set.Location, oto.Wav);
            DisplayFile = oto?.Wav;
            Offset = oto.Offset;
            Consonant = oto.Consonant;
            Cutoff = oto.Cutoff;
            Preutter = oto.Preutter;
            Overlap = oto.Overlap;

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

    public enum USingerType { Classic, Enunu, Vogen }

    public class USinger {
        public virtual string Id { get; }
        public virtual string Name => name;
        public virtual USingerType SingerType { get; }
        public virtual string BasePath { get; }
        public virtual string Author { get; }
        public virtual string Location { get; }
        public virtual string Web { get; }
        public virtual string Version { get; }
        public virtual string OtherInfo { get; }
        public virtual IList<string> Errors { get; }
        public virtual string Avatar { get; }
        public virtual byte[] AvatarData { get; }
        public virtual string Portrait { get; }
        public virtual float PortraitOpacity { get; }
        public virtual Encoding TextFileEncoding => Encoding.UTF8;
        public virtual IList<USubbank> Subbanks { get; }
        public virtual Dictionary<string, UOto> Otos { get; }

        public bool Found => found;
        public bool Loaded => found && loaded;

        protected bool found;
        protected bool loaded;

        private string name;

        public string DisplayName { get { return Found ? name : $"[Missing] {name}"; } }

        public virtual void EnsureLoaded() { }
        public virtual void Reload() { }
        public virtual bool TryGetMappedOto(string phoneme, int tone, out UOto oto) {
            oto = default;
            return false;
        }
        public virtual bool TryGetMappedOto(string phoneme, int tone, string color, out UOto oto) {
            oto = default;
            return false;
        }
        public virtual void GetSuggestions(string text, Action<UOto> provide) { }
        public override string ToString() => Name;

        public static USinger CreateMissing(string name) {
            return new USinger() {
                found = false,
                loaded = false,
                name = name,
            };
        }
    }
}

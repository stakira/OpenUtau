using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using OpenUtau.Classic;
using WanaKanaSharp;

namespace OpenUtau.Core.Ustx {
    public struct UOto {
        public string Alias { private set; get; }
        public string Set { private set; get; }
        public string File { private set; get; }
        public double Offset { private set; get; }
        public double Consonant { private set; get; }
        public double Cutoff { private set; get; }
        public double Preutter { private set; get; }
        public double Overlap { private set; get; }
        public List<string> SearchTerms { private set; get; }

        public UOto(Oto oto, UOtoSet set) {
            Alias = oto.Name;
            Set = set.Name;
            File = Path.Combine(set.Location, oto.Wav);
            Offset = oto.Offset;
            Consonant = oto.Consonant;
            Cutoff = oto.Cutoff;
            Preutter = oto.Preutter;
            Overlap = oto.Overlap;
            SearchTerms = new List<string>();
            SearchTerms.Add(Alias.ToLowerInvariant().Replace(" ", ""));
            SearchTerms.Add(WanaKana.ToRomaji(Alias).ToLowerInvariant().Replace(" ", ""));
        }
    }

    public class UOtoSet {
        public readonly string Name;
        public readonly string Location;
        public readonly Dictionary<string, List<UOto>> Otos;

        public UOtoSet(OtoSet otoSet, USinger singer, string singersPath) {
            Name = otoSet.Name;
            Location = Path.Combine(singersPath, Path.GetDirectoryName(otoSet.File));
            Otos = new Dictionary<string, List<UOto>>();
            foreach (var oto in otoSet.Otos) {
                if (!Otos.ContainsKey(oto.Name)) {
                    Otos.Add(oto.Name, new List<UOto>());
                }
                Otos[oto.Name].Add(new UOto(oto, this));
            }
        }
    }

    public class USinger {
        public readonly string Name;
        public readonly string Location;
        public readonly string Author;
        public readonly string Web;
        public readonly BitmapImage Avatar;
        public readonly Dictionary<string, Tuple<string, string>> PitchMap;
        public readonly List<UOtoSet> OtoSets;
        public readonly string VoicebankName;
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
            Location = Path.Combine(singersPath, Path.GetDirectoryName(voicebank.File));
            if (!string.IsNullOrEmpty(voicebank.Image)) {
                var imagePath = Path.Combine(Location, voicebank.Image);
                if (File.Exists(imagePath)) {
                    Avatar = LoadAvatar(imagePath);
                }
            }
            if (voicebank.PrefixMap != null) {
                PitchMap = voicebank.PrefixMap.Map;
            } else {
                PitchMap = new Dictionary<string, Tuple<string, string>>();
            }
            OtoSets = new List<UOtoSet>();
            foreach (var otoSet in voicebank.OtoSets) {
                OtoSets.Add(new UOtoSet(otoSet, this, singersPath));
            }
            VoicebankName = Path.GetDirectoryName(voicebank.OrigFile);
            Loaded = true;
        }

        public bool TryGetOto(string phoneme, int tone, out UOto oto) {
            oto = default;
            string noteString = MusicMath.GetNoteString(tone);
            if (PitchMap.TryGetValue(noteString, out var mapped)) {
                string phonemeMapped = mapped.Item1 + phoneme + mapped.Item2;
                foreach (var set in OtoSets) {
                    if (set.Otos.TryGetValue(phonemeMapped, out var list)) {
                        oto = list[0];
                        return true;
                    }
                }
            }
            foreach (var set in OtoSets) {
                if (set.Otos.TryGetValue(phoneme, out var list)) {
                    oto = list[0];
                    return true;
                }
            }
            return false;
        }

        public void GetSuggestions(string text, Action<UOto> provide) {
            if (text != null) {
                text = text.ToLowerInvariant().Replace(" ", "");
            }
            bool all = string.IsNullOrEmpty(text);
            OtoSets
                .SelectMany(set => set.Otos.Values)
                .SelectMany(list => list)
                .Where(oto => all || oto.SearchTerms.Exists(term => term.Contains(text)))
                .ToList()
                .ForEach(oto => provide(oto));
        }

        private static BitmapImage LoadAvatar(string path) {
            var avatar = new BitmapImage();
            avatar.BeginInit();
            avatar.CacheOption = BitmapCacheOption.OnLoad;
            avatar.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
            avatar.EndInit();
            avatar.Freeze();
            return avatar;
        }
    }
}

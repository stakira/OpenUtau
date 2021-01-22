using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using OpenUtau.Classic;
using WanaKanaSharp;

namespace OpenUtau.Core.USTx {
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
        public readonly Dictionary<string, UOto> Otos;

        public UOtoSet(OtoSet otoSet, USinger singer, string singersPath) {
            Name = otoSet.Name;
            Location = Path.Combine(singersPath, Path.GetDirectoryName(otoSet.File));
            Otos = new Dictionary<string, UOto>();
            foreach (var oto in otoSet.Otos) {
                if (!Otos.ContainsKey(oto.Name)) {
                    Otos.Add(oto.Name, new UOto(oto, this));
                } else {
                    Serilog.Log.Error("{0} {1} {2}", singer.Name, Name, oto.Name);
                }
            }
        }
    }

    public class USinger {
        public readonly string Name;
        public readonly string Location;
        public readonly string Author;
        public readonly string Web;
        public readonly BitmapImage Avatar;
        public readonly Dictionary<string, string> PitchMap;
        public readonly List<UOtoSet> OtoSets;
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
            var imagePath = Path.Combine(Location, voicebank.Image);
            Avatar = LoadAvatar(imagePath);
            Loaded = true;
            if (voicebank.PrefixMap != null) {
                PitchMap = voicebank.PrefixMap.Map;
            } else {
                PitchMap = new Dictionary<string, string>();
            }
            OtoSets = new List<UOtoSet>();
            foreach (var otoSet in voicebank.OtoSets) {
                OtoSets.Add(new UOtoSet(otoSet, this, singersPath));
            }
        }

        public bool TryGetOto(string lyric, out UOto oto) {
            oto = default;
            foreach (var set in OtoSets) {
                if (set.Otos.TryGetValue(lyric, out oto)) {
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
            foreach (var set in OtoSets) {
                foreach (var oto in set.Otos.Values) {
                    if (all || oto.SearchTerms.Exists(term => term.Contains(text))) {
                        provide(oto);
                    }
                }
            }
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

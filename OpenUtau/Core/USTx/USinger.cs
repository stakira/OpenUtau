using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using OpenUtau.Classic;

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

        public UOto(Oto oto, OtoSet otoSet) {
            Alias = oto.Name;
            Set = otoSet.Name;
            File = Path.Combine(Path.GetDirectoryName(otoSet.File), oto.Wav);
            Offset = oto.Offset;
            Consonant = oto.Consonant;
            Cutoff = oto.Cutoff;
            Preutter = oto.Preutter;
            Overlap = oto.Overlap;
        }
    }

    public class USinger {
        public readonly string Name;
        public readonly string Location;
        public readonly string Author;
        public readonly string Web;
        public readonly BitmapImage Avatar;
        public readonly ImmutableDictionary<string, string> PitchMap;
        public readonly ImmutableDictionary<string, UOto> AliasMap;
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
                PitchMap = voicebank.PrefixMap.Map.ToImmutableDictionary();
            } else {
                PitchMap = ImmutableDictionary<string, string>.Empty;
            }
            var aliasMap = new Dictionary<string, UOto>();
            foreach (var otoSet in voicebank.OtoSets) {
                foreach (var oto in otoSet.Otos) {
                    if (!aliasMap.ContainsKey(oto.Name)) {
                        aliasMap.Add(oto.Name, new UOto(oto, otoSet));
                    }
                }
            }
            AliasMap = aliasMap.ToImmutableDictionary();
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

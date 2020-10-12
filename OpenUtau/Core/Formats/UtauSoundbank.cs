using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using OpenUtau.Classic;
using OpenUtau.Core.USTx;

namespace OpenUtau.Core.Formats {
    public static class UtauSoundbank {
        public static Dictionary<string, USinger> FindAllSingers() {
            var loader = new VoicebankLoader(PathManager.Inst.InstalledSingersPath);
            var voicebanks = loader.LoadAll();
            Dictionary<string, USinger> singers = new Dictionary<string, USinger>();
            foreach (var pair in voicebanks) {
                singers[pair.Key] = FromVoicebank(pair.Value);
            }
            return singers;
        }

        static USinger FromVoicebank(Voicebank voicebank) {
            var singer = new USinger();
            singer.Name = voicebank.Name;
            singer.Author = voicebank.Author;
            singer.Website = voicebank.Web;
            singer.Path = Path.Combine(PathManager.Inst.InstalledSingersPath, Path.GetDirectoryName(voicebank.File));
            var imagePath = Path.Combine(singer.Path, voicebank.Image);
            singer.Avatar = LoadAvatar(imagePath);
            singer.Loaded = true;
            singer.PitchMap = voicebank.PrefixMap.Map;
            foreach (var otoSet in voicebank.OtoSets) {
                var otoDir = Path.Combine(Path.GetDirectoryName(otoSet.File));
                foreach (var oto in otoSet.Otos) {
                    if (!singer.AliasMap.ContainsKey(oto.Name)) {
                        singer.AliasMap.Add(oto.Name, new UOto {
                            Alias = oto.Name,
                            File = Path.Combine(otoDir, oto.Wav),
                            Offset = oto.Offset,
                            Consonant = oto.Consonant,
                            Cutoff = oto.Cutoff,
                            Preutter = oto.Preutter,
                            Overlap = oto.Overlap,
                        });
                    }
                }
            }
            return singer;
        }

        static BitmapImage LoadAvatar(string path) {
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

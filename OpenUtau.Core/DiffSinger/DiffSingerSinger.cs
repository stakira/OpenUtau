using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Core.DiffSinger {
    class DiffSingerSinger : USinger {
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
        public override string DefaultPhonemizer => voicebank.DefaultPhonemizer;
        public override Encoding TextFileEncoding => voicebank.TextFileEncoding;
        public override IList<USubbank> Subbanks => subbanks;
        public override Dictionary<string, UOto> Otos => otos;

        Voicebank voicebank;
        List<string> errors = new List<string>();
        List<USubbank> subbanks = new List<USubbank>();
        Dictionary<string, UOto> otos = new Dictionary<string, UOto>();

        HashSet<string> phonemes = new HashSet<string>();
        Dictionary<string, string[]> table = new Dictionary<string, string[]>();

        public byte[] avatarData;

        public Dictionary<string, Tuple<string, string>> phoneDict = new Dictionary<string, Tuple<string, string>>();

        public DiffSingerSinger(Voicebank voicebank) {
            this.voicebank = voicebank;
            //加载头像
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
            //导入拼音转音素字典
            //实现inference\svs\opencpop\map.py
            phoneDict.Clear();
            phonemes.Clear();
            string path = Path.Combine(Location, "pinyin2ph.txt");
            phoneDict.Add("AP", new Tuple<string, string>("", "AP"));
            phoneDict.Add("SP", new Tuple<string, string>("", "SP"));
            foreach (string line in File.ReadLines(path, Encoding.UTF8)) {
                string[] elements = line.Split("|");
                elements[2] = elements[2].Trim();
                if (elements[2].Contains(" ")) {//声母+韵母
                    string[] phones = elements[2].Split(" ");
                    phoneDict.Add(elements[1].Trim(), new Tuple<string, string>(phones[0], phones[1]));
                    phonemes.Add(phones[0]);
                    phonemes.Add(phones[1]);
                } else {//仅韵母
                    phoneDict.Add(elements[1].Trim(), new Tuple<string, string>("", elements[2]));
                    phonemes.Add(elements[2]);
                }
            }
            found = true;
            loaded = true;
        }

        public override bool TryGetMappedOto(string phoneme, int tone, out UOto oto) {
            var parts = phoneme.Split();
            if (parts.All(p => phonemes.Contains(p))) {
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

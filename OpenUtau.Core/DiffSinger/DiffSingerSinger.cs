using Newtonsoft.Json;
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

        Voicebank voicebank;
        List<string> errors = new List<string>();
        List<USubbank> subbanks = new List<USubbank>();
        Dictionary<string, string[]> table = new Dictionary<string, string[]>();
        public byte[] avatarData;
        public Dictionary<string, Tuple<string, string>> phoneDict = new Dictionary<string, Tuple<string, string>>();
        public List<string> phonemes = new List<string>();
        public DiffSingerConfig diffSingerConfig = new DiffSingerConfig();
        public byte[] acousticModel = new byte[0];
        public byte[] vocoderModel = new byte[0];

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
            //导入音源设置
            LoadConfig();
            //导入拼音转音素字典
            phoneDict.Clear();
            HashSet<string> phonemesSet = new HashSet<string> { "SP", "AP" };
            string path = Path.Combine(Location, diffSingerConfig.dictionary);
            phoneDict.Add("AP", new Tuple<string, string>("", "AP"));
            phoneDict.Add("SP", new Tuple<string, string>("", "SP"));
            foreach (string line in File.ReadLines(path, Encoding.UTF8)) {
                string[] elements = line.Split("\t");
                elements[1] = elements[1].Trim();
                if (elements[1].Contains(" ")) {//声母+韵母
                    string[] phones = elements[1].Split(" ");
                    phoneDict.Add(elements[0].Trim(), new Tuple<string, string>(phones[0], phones[1]));
                    phonemesSet.Add(phones[0]);
                    phonemesSet.Add(phones[1]);
                } else {//仅韵母
                    phoneDict.Add(elements[0].Trim(), new Tuple<string, string>("", elements[1]));
                    phonemesSet.Add(elements[1]);
                }
            }
            //有效音素列表
            var phonemesList = phonemesSet.ToList();
            phonemesList.Sort((x, y) => string.CompareOrdinal(x, y));
            //包含padding的有效音素列表
            phonemes = Enumerable.Repeat("", diffSingerConfig.reserved_tokens).ToList();
            phonemes.AddRange(phonemesList);
            found = true;
            loaded = true;
        }

        private void LoadConfig() {
            string path = Path.Combine(Location, "dsconfig.json");
            diffSingerConfig = JsonConvert.DeserializeObject<DiffSingerConfig>(
                File.ReadAllText(path, TextFileEncoding));
        }

        public override bool TryGetOto(string phoneme, out UOto oto) {
            var parts = phoneme.Split();
            if (parts.All(p => phonemes.Contains(p))) {
                oto = UOto.OfDummy(phoneme);
                return true;
            }
            oto = null;
            return false;
        }

        public override IEnumerable<UOto> GetSuggestions(string text) {
            if (text != null) {
                text = text.ToLowerInvariant().Replace(" ", "");
            }
            bool all = string.IsNullOrEmpty(text);
            return table.Keys
                .Where(key => all || key.Contains(text))
                .Select(key => UOto.OfDummy(key));
        }

        public override byte[] LoadPortrait() {
            return string.IsNullOrEmpty(Portrait)
                ? null
                : File.ReadAllBytes(Portrait);
        }

        public byte[] getAcousticModel() {
            if (acousticModel.Length == 0) {
                acousticModel = File.ReadAllBytes(Path.Combine(Location, diffSingerConfig.acoustic));
            }
            return acousticModel;
        }

        public byte[] getVocoderModel() {
            if (vocoderModel.Length == 0) {
                vocoderModel = File.ReadAllBytes(Path.Combine(Location, diffSingerConfig.vocoder));
            }
            return vocoderModel;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;
using Serilog;
using Microsoft.ML.OnnxRuntime;

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
        public List<string> phonemes = new List<string>();
        public DsConfig dsConfig;
        public InferenceSession acousticSession = null;
        public DsVocoder vocoder = null;

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
            string configPath = Path.Combine(Location, "dsconfig.yaml");
            dsConfig = Core.Yaml.DefaultDeserializer.Deserialize<DsConfig>(
                File.ReadAllText(configPath, TextFileEncoding));
            //导入音素列表
            string phonemesPath = Path.Combine(Location, dsConfig.phonemes);
            phonemes = File.ReadLines(phonemesPath,TextFileEncoding).ToList();

            found = true;
            loaded = true;
            
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

        public InferenceSession getAcousticSession() {
            if (acousticSession is null) {
                var acousticModel = File.ReadAllBytes(Path.Combine(Location, dsConfig.acoustic));
                acousticSession = Onnx.getInferenceSession(acousticModel);
            }
            return acousticSession;
        }

        public DsVocoder getVocoder() {
            //获取声码器
            if(vocoder is null) {
                vocoder = new DsVocoder(dsConfig.vocoder);
            }
            return vocoder;
        }
    }

    [Serializable]
    public class DsConfig {
        public string phonemes = "phonemes.txt";
        public string acoustic;
        public string vocoder;
    }
}

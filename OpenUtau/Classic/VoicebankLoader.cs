using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace OpenUtau.Classic {
    class VoicebankLoader {
        readonly string basePath;

        public VoicebankLoader(string basePath) {
            this.basePath = basePath;
        }

        public Dictionary<string, Voicebank> LoadAll() {
            Dictionary<string, Voicebank> result = new Dictionary<string, Voicebank>();
            if (!Directory.Exists(basePath)) {
                return result;
            }
            foreach (var file in Directory.EnumerateFiles(basePath, "_voicebank.json", SearchOption.AllDirectories)) {
                var voicebank = LoadVoicebank(Path.GetDirectoryName(file), file);
                result.Add(Path.GetDirectoryName(voicebank.OrigFile), voicebank);

            }
            return result;
        }

        Voicebank LoadVoicebank(string dirpath, string voicebankFile) {
            var voicebank = JsonConvert.DeserializeObject<Voicebank>(File.ReadAllText(voicebankFile));
            voicebank.OtoSets = new List<OtoSet>();
            foreach (var file in Directory.EnumerateFiles(dirpath, "_oto.json", SearchOption.AllDirectories)) {
                var otoSet = JsonConvert.DeserializeObject<OtoSet>(File.ReadAllText(file));
                voicebank.OtoSets.Add(otoSet);
            }
            var prefixMapPath = Path.Combine(dirpath, "_prefix_map.json");
            if (File.Exists(prefixMapPath)) {
                voicebank.PrefixMap = JsonConvert.DeserializeObject<PrefixMap>(File.ReadAllText(prefixMapPath));
            }
            return voicebank;
        }
    }
}

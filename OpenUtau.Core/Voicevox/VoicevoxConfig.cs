using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Voicevox {
    class VoicevoxConfig {
        public Supported_features supported_features;
        public string name;
        public string speaker_uuid;
        public IList<Styles> styles;
        public string version;
        public string dictxtPath;


        struct SingersResult {
            public string body;
        }

        struct SingersResponse {
            public string error;
            public SingersResult result;
        }

        public static VoicevoxConfig Load(USinger singer) {
            var configPath = Path.Join(singer.Location, "vvconfig.json");
            var config = new List<RawVoicevoxConfig>();
            if (File.Exists(configPath)) {
                var configTxt = File.ReadAllText(configPath);
                config = JsonConvert.DeserializeObject<List<RawVoicevoxConfig>>(configTxt);
            } else {
                var response = VoicevoxClient.Inst.SendRequest<SingersResponse>(new VoicevoxURL() { method ="GET",path= "/singers" });
                if (response.error != null) {
                    throw new Exception(response.error);
                }
                config = JsonConvert.DeserializeObject<List<RawVoicevoxConfig>>(response.result.body);
            }
            return config != null ? new VoicevoxConfig() : config[0].getVVSingers(config, singer);
        }
    }

    public class Style_infos {
        public int id { get; set; }
        public string icon { get; set; }
        public string portrait { get; set; }
        public IList<string> voice_samples { get; set; }

    }
    public class Singer_info {
        public string policy { get; set; }
        public string portrait { get; set; }
        public IList<Style_infos> style_infos { get; set; }

    }

    class Supported_features {
        public string permitted_synthesis_morphing;

    }
    public class Styles {
        public string name;
        public int id;
        public string type;

    }
    class RawVoicevoxConfig {
        public Supported_features supported_features;
        public string name;
        public string speaker_uuid;
        public IList<Styles> styles;
        public string version;

        public VoicevoxConfig getVVSingers(List<RawVoicevoxConfig> singers, USinger singer) {
            VoicevoxConfig vvSinger = new VoicevoxConfig();
            foreach (RawVoicevoxConfig s in singers) {
                if (singer.DisplayName.Equals(s.name)) {
                    vvSinger.name = s.name;
                    vvSinger.version = s.version;
                    vvSinger.speaker_uuid = s.speaker_uuid;
                    vvSinger.styles = s.styles;
                    vvSinger.speaker_uuid = s.speaker_uuid;
                    vvSinger.dictxtPath = Path.Join(singer.Location, "dic.txt");
                    break;
                }
            }
            return vvSinger;
        }
    }
}

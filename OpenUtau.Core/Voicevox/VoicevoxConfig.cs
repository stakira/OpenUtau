using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Core.Voicevox {
    public class VoicevoxConfig {
        public Supported_features supported_features;
        public string name = string.Empty;
        public string speaker_uuid = string.Empty;
        public IList<Styles> styles;
        public string version = string.Empty;
        public string dictxtPath = string.Empty;

        public string policy = string.Empty;
        public string portraitPath = string.Empty;
        public IList<Style_infos> style_infos;

        public string singerlocation;
        public List<(string name, Styles styles)> base_style;
        public string base_name;
        public string base_style_name;
        public string PhonemizerType = string.Empty;
        public bool PhonemizerFlag = true;

        public static VoicevoxConfig Load(USinger singer) {
            var parentDirectory = Directory.GetParent(singer.Location).ToString();
            var configPath = Path.Join(parentDirectory, "vvconfig.json");
            var configs = new List<RawVoicevoxConfig>();
            try {
                if (File.Exists(configPath)) {
                    var configTxt = File.ReadAllText(configPath);
                    configs = JsonConvert.DeserializeObject<List<RawVoicevoxConfig>>(configTxt);
                } else {
                    var ins = VoicevoxClient.Inst;
                    ins.SendRequest(new VoicevoxURL() { method = "GET", path = "/singers" });
                    File.WriteAllText(configPath, JsonConvert.SerializeObject(ins.jObj));
                    configs = ins.jObj.ToObject<List<RawVoicevoxConfig>>();
                }
                foreach (RawVoicevoxConfig rowVoicevoxConfig in configs) {
                    if (rowVoicevoxConfig.name.Equals(singer.Name)) {
                        VoicevoxConfig voicevoxConfig = rowVoicevoxConfig.Convert();
                        voicevoxConfig.singerlocation = singer.Location;
                        voicevoxConfig.dictxtPath = Path.Join(parentDirectory, "dic.txt");
                        return voicevoxConfig;
                    }
                }
            } catch {
                Log.Error("Failed to create a voice base.");
            }
            return new VoicevoxConfig();
        }
        public void LoadInfo(VoicevoxConfig voicevoxConfig) {
            if(voicevoxConfig.style_infos == null) {
                var ins = VoicevoxClient.Inst;
                var queryurl = new VoicevoxURL() { method = "GET", path = "/singer_info", query = new Dictionary<string, string> { { "speaker_uuid", voicevoxConfig.speaker_uuid } } };
                ins.SendRequest(queryurl);
                var rawSinger_Info = ins.jObj.ToObject<RawSinger_info>();
                if (rawSinger_Info != null) {
                    rawSinger_Info.SetInfo(voicevoxConfig);
                }
            }

        }
    }

    public class Style_infos {
        public int id;
        public string icon = string.Empty;
        public string portrait = string.Empty;
        public IList<string> voice_samples = new List<string>();

    }
    class RawSinger_info {
        public string policy = string.Empty;
        public string portrait = string.Empty;
        public IList<Style_infos> style_infos = new List<Style_infos>();

        public void SetInfo(VoicevoxConfig voicevoxConfig) {
            Log.Information($"Begin setup of Voicevox SingerInfo.");
            try {
                if (!string.IsNullOrEmpty(this.policy)) {
                    voicevoxConfig.policy = this.policy;
                    var readmepath = Path.Join(voicevoxConfig.singerlocation, "readme.txt");
                    File.AppendAllText(readmepath, this.policy);
                }
                voicevoxConfig.portraitPath = Path.Join(voicevoxConfig.singerlocation, $"{voicevoxConfig.name}_portrait.png");
                Base64.Base64ToFile(this.portrait, voicevoxConfig.portraitPath);
                if (this.style_infos != null) {
                    voicevoxConfig.style_infos = new List<Style_infos>();
                    for (int i = 0; i < this.style_infos.Count; i++) {
                        voicevoxConfig.style_infos.Add(new Style_infos());
                        for (int a = 0; a < style_infos[i].voice_samples.Count; a++) {
                            voicevoxConfig.style_infos[i].voice_samples.Add(Path.Join(voicevoxConfig.singerlocation, $"{voicevoxConfig.name}_{voicevoxConfig.styles[i].name}_{a}.wav"));
                            checkAndSetFiles(this.style_infos[i].voice_samples[a], voicevoxConfig.style_infos[i].voice_samples[a]);
                        }
                        voicevoxConfig.style_infos[i].icon = Path.Join(voicevoxConfig.singerlocation, $"{voicevoxConfig.name}_{voicevoxConfig.styles[i].name}_icon.png");
                        checkAndSetFiles(this.style_infos[i].icon, voicevoxConfig.style_infos[i].icon);

                        voicevoxConfig.style_infos[i].portrait = Path.Join(voicevoxConfig.singerlocation, $"{voicevoxConfig.name}_{voicevoxConfig.styles[i].name}_portrait.png");
                        checkAndSetFiles(this.portrait, voicevoxConfig.style_infos[i].portrait);

                        voicevoxConfig.style_infos[i].id = this.style_infos[i].id;
                    }
                }
            } catch (Exception e){
                Log.Error($"Could not create character file. : {e}");
            }
            Log.Information($"Voicevox SingerInfo setup complete.");
        }

        public void checkAndSetFiles(string base64str,string filePath) {
            if (!String.IsNullOrEmpty(base64str) && !File.Exists(filePath)) {
                    Base64.Base64ToFile(base64str, filePath);
            }
        }
    }

    public class Supported_features {
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
        public string base_name;
        public string base_style_name;

        public VoicevoxConfig Convert() {
            VoicevoxConfig voicevoxConfig = new VoicevoxConfig();
            voicevoxConfig.name = this.name;
            voicevoxConfig.version = this.version;
            voicevoxConfig.speaker_uuid = this.speaker_uuid;
            voicevoxConfig.styles = this.styles;
            voicevoxConfig.supported_features = this.supported_features;
            voicevoxConfig.base_name = this.base_name;
            voicevoxConfig.base_style_name = this.base_style_name;
            return voicevoxConfig;
        }
    }
}

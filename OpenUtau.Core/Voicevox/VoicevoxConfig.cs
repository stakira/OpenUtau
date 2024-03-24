using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using Serilog;
using static OpenUtau.Api.Phonemizer;

namespace OpenUtau.Core.Voicevox {
    public class VoicevoxConfig {
        //Information that each Singer has
        public Supported_features supported_features;
        public string name = string.Empty;
        public string speaker_uuid = string.Empty;
        public List<Styles> styles;
        public string version = string.Empty;
        public string policy = string.Empty;
        public string portraitPath = string.Empty;
        //So that the renderer can distinguish between phonemizers.
        public string Tag = "DEFAULT";

        public List<Style_infos> style_infos;
        //Prepare for future additions of Teacher Singer.
        public List<(string name, Styles styles)> base_singer_style;
        public string base_singer_name = string.Empty;
        public string base_singer_style_name = string.Empty;

        public static VoicevoxConfig Load(USinger singer) {
            try {
                var response = VoicevoxClient.Inst.SendRequest(new VoicevoxURL() { method = "GET", path = "/singers" });
                var jObj = JObject.Parse(response.Item1);
                if (jObj.ContainsKey("detail")) {
                    Log.Error($"Response was incorrect. : {jObj}");
                }
                var configs = jObj["json"].ToObject<List<RawVoicevoxConfig>>();
                var parentDirectory = Directory.GetParent(singer.Location).ToString();
                List<VoicevoxConfig> vvList = new List<VoicevoxConfig>();
                foreach (RawVoicevoxConfig rowVoicevoxConfig in configs) {
                    VoicevoxConfig voicevoxConfig = rowVoicevoxConfig.Convert();
                    var folderPath = Path.Join(parentDirectory, voicevoxConfig.name);
                    var filePath = Path.Join(folderPath, "character.yaml");
                    if (!File.Exists(filePath)) {
                        Directory.CreateDirectory(folderPath);
                        string typename = string.Empty ;
                        SingerTypeUtils.SingerTypeNames.TryGetValue(USingerType.Voicevox, out typename);
                        var config = new VoicebankConfig() {
                            Name = voicevoxConfig.name,
                            TextFileEncoding = Encoding.UTF8.WebName,
                            SingerType = typename,
                            PortraitHeight = 600,
                            Portrait = $"{voicevoxConfig.name}_portrait.png"
                        };
                        using (var stream = File.Open(filePath, FileMode.Create)) {
                            config.Save(stream);
                        }
                        //Create an empty file to read. May write information in the future?
                        File.WriteAllText(Path.Join(folderPath, "character.txt"), string.Empty);
                    }
                    vvList.Add(voicevoxConfig);
                }
                return vvList.Where(vv => vv.name.Equals(singer.Name)).ToList()[0];
            } catch {
                Log.Error("Could not load VOICEVOX singer.");
            }
            return new VoicevoxConfig();
        }
        public void LoadInfo(VoicevoxConfig voicevoxConfig, string location) {
            if(voicevoxConfig.style_infos == null) {
                var queryurl = new VoicevoxURL() { method = "GET", path = "/singer_info", query = new Dictionary<string, string> { { "speaker_uuid", voicevoxConfig.speaker_uuid } } };
                var response = VoicevoxClient.Inst.SendRequest(queryurl);
                var jObj = JObject.Parse(response.Item1);
                if (jObj.ContainsKey("detail")) {
                    Log.Error($"Response was incorrect. : {jObj}");
                } else {
                    var rawSinger_Info = jObj.ToObject<RawSinger_info>();
                    if (rawSinger_Info != null) {
                        rawSinger_Info.SetInfo(voicevoxConfig, location);
                    }
                }
            }

        }
    }

    public class Phoneme_list {
        public string[] vowels;
        public string[] consonants;
        public string[] kana;
    }

    public class Dictionary_list {
        public Dictionary<string,string> dict = new Dictionary<string, string>();

        public void Loaddic(string location) {
            try {
                var parentDirectory = Directory.GetParent(location).ToString();
                var yamlPath = Path.Join(parentDirectory, "dictionary.yaml");
                if (File.Exists(yamlPath)) {
                    var yamlTxt = File.ReadAllText(yamlPath);
                    var yamlObj = Yaml.DefaultDeserializer.Deserialize<Dictionary<string, List<Dictionary<string, string>>>>(yamlTxt);
                    var list = yamlObj["list"];
                    dict = new Dictionary<string, string>();

                    foreach (var item in list) {
                        foreach (var pair in item) {
                            dict[pair.Key] = pair.Value;
                        }
                    }

                }
            }catch (Exception e) {
                Log.Error($"Failed to read dictionary file. : {e}");
            }
        }

        public string Lyrictodic(Note[][] notes,int index) {
            if (dict.TryGetValue(notes[index][0].lyric, out var lyric_)) {
                if (string.IsNullOrEmpty(lyric_)) {
                    return "";
                }
                return lyric_;
            }
            return notes[index][0].lyric;
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

        public void SetInfo(VoicevoxConfig voicevoxConfig, string location) {
            Log.Information($"Begin setup of Voicevox SingerInfo.");
            try {
                var readmePath = Path.Join(location, "readme.txt");
                if (!string.IsNullOrEmpty(this.policy) && !File.Exists(readmePath)) {
                    voicevoxConfig.policy = this.policy;
                    File.WriteAllText(readmePath, this.policy);
                }
                voicevoxConfig.portraitPath = Path.Join(location, $"{voicevoxConfig.name}_portrait.png");
                Base64.Base64ToFile(this.portrait, voicevoxConfig.portraitPath);
                if (this.style_infos != null) {
                    voicevoxConfig.style_infos = new List<Style_infos>();
                    for (int i = 0; i < this.style_infos.Count; i++) {
                        voicevoxConfig.style_infos.Add(new Style_infos());
                        for (int a = 0; a < style_infos[i].voice_samples.Count; a++) {
                            voicevoxConfig.style_infos[i].voice_samples.Add(Path.Join(location, $"{voicevoxConfig.name}_{voicevoxConfig.styles[i].name}_{a}.wav"));
                            checkAndSetFiles(this.style_infos[i].voice_samples[a], voicevoxConfig.style_infos[i].voice_samples[a]);
                        }
                        voicevoxConfig.style_infos[i].icon = Path.Join(location, $"{voicevoxConfig.name}_{voicevoxConfig.styles[i].name}_icon.png");
                        checkAndSetFiles(this.style_infos[i].icon, voicevoxConfig.style_infos[i].icon);

                        voicevoxConfig.style_infos[i].portrait = Path.Join(location, $"{voicevoxConfig.name}_{voicevoxConfig.styles[i].name}_portrait.png");
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
        public List<Styles> styles;
        public string version;
        public string base_singer_name;
        public string base_singer_style_name;

        public VoicevoxConfig Convert() {
            VoicevoxConfig voicevoxConfig = new VoicevoxConfig();
            voicevoxConfig.name = this.name.Replace("/", "_");
            voicevoxConfig.version = this.version;
            voicevoxConfig.speaker_uuid = this.speaker_uuid;
            voicevoxConfig.styles = this.styles;
            voicevoxConfig.supported_features = this.supported_features;
            voicevoxConfig.base_singer_name = this.base_singer_name;
            voicevoxConfig.base_singer_style_name = this.base_singer_style_name;
            return voicevoxConfig;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenUtau.Core.Render;
using Serilog;
using static OpenUtau.Api.Phonemizer;

/*
 * This source code is partially based on the VOICEVOX engine.
 * https://github.com/VOICEVOX/voicevox_engine/blob/master/LGPL_LICENSE
 */

namespace OpenUtau.Core.Voicevox {
    public class Phonemes {
        public string phoneme;
        public int frame_length;

    }
    public class VoicevoxNote {
        public List<double> f0 = new List<double>();
        public List<double> volume = new List<double>();
        public List<Phonemes> phonemes = new List<Phonemes>();
        public int volumeScale;
        public int outputSamplingRate;
        public bool outputStereo;

    }

    public class VoicevoxQueryNotes {
        public int? key;
        public int frame_length;
        public string lyric;
        public int vqnindex;

    }

    public class VoicevoxQueryMain {
        public List<VoicevoxQueryNotes> notes = new List<VoicevoxQueryNotes>();
    }
    public class Phoneme_list {
        public string[] vowels = "a i u e o A I U E O N pau cl".Split();
        public string[] consonants = "b by ch d dy f g gw gy h hy j k kw ky m my n ny p py r ry s sh t ts ty v w y z".Split();
        public Dictionary<string, string> kanas = new Dictionary<string, string>();
        public Dictionary<string, string> paus = new Dictionary<string, string>();
        public Phoneme_list() {
            var kanaGroups = new List<string[]> {
                "あ ば びゃ ちゃ だ でゃ ふぁ が ぐゎ ぎゃ は ひゃ じゃ か くゎ きゃ ま みゃ な にゃ ぱ ぴゃ ら りゃ さ しゃ た つぁ てゃ ゔぁ わ や ざ".Split(),
                "い び  ち ぢ でぃ ふぃ ぎ   ひ  じ き   み  に  ぴ  り  すぃ し てぃ つぃ  ゔぃ うぃ  ずぃ".Split(),
                "う ぶ びゅ ちゅ どぅ でゅ ふ ぐ  ぎゅ  ひゅ じゅ く  きゅ む みゅ ぬ にゅ ぷ ぴゅ る りゅ す しゅ つ つ てゅ ゔ  ゆ ず".Split(),
                "え べ びぇ ちぇ で でぇ ふぇ げ  ぎぇ へ ひぇ じぇ け  きぇ め みぇ ね にぇ ぺ ぴぇ れ りぇ せ しぇ て つぇ  ゔぇ うぇ いぇ ぜ".Split(),
                "お ぼ びょ ちょ ど でょ ふぉ ご  ぎょ ほ ひょ じょ こ  きょ も みょ の にょ ぽ ぴょ ろ りょ そ しょ と つぉ てょ ゔぉ を よ ぞ".Split(),
                "ん ン".Split(),
                "っ ッ".Split()
            };

            foreach (var group in kanaGroups) {
                foreach (var kana in group) {
                    if (!kanas.ContainsKey(kana)) {
                        kanas.Add(kana.Normalize(), group[0].Normalize());
                    }
                }
            }
            string[] pauseGroups = "R pau AP SP".Split();

            foreach (string group in pauseGroups) {
                if (!paus.ContainsKey(group)) {
                    paus.Add(group.Normalize(), pauseGroups[0].Normalize());
                }
            }
        }
    }

    public class Dictionary_list {
        public Dictionary<string, string> dict = new Dictionary<string, string>();

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
            } catch (Exception e) {
                Log.Error($"Failed to read dictionary file. : {e}");
            }
        }
        public string Notetodic(Note[][] notes, int index) {
            if (dict.TryGetValue(notes[index][0].lyric, out var lyric_)) {
                if (string.IsNullOrEmpty(lyric_)) {
                    return "";
                }
                return lyric_;
            }
            return notes[index][0].lyric;
        }

        public string Lyrictodic(string lyric) {
            if (dict.TryGetValue(lyric, out var lyric_)) {
                if (string.IsNullOrEmpty(lyric_)) {
                    return "";
                }
                return lyric_;
            }
            return lyric;
        }

        public bool IsDic(string lyric) {
            return dict.ContainsKey(lyric);
        }
    }


    public static class VoicevoxUtils {
        public const string VOLC = "volc";
        public const int headS = 1;
        public const int tailS = 1;
        public const double fps = 93.75;
        public const string defaultID = "6000";
        public static Dictionary_list dic = new Dictionary_list();
        public static Phoneme_list phoneme_List = new Phoneme_list();

        public static VoicevoxNote VoicevoxVoiceBase(VoicevoxQueryMain qNotes, string id) {
            var queryurl = new VoicevoxURL() { method = "POST", path = "/sing_frame_audio_query", query = new Dictionary<string, string> { { "speaker", id } }, body = JsonConvert.SerializeObject(qNotes) };
            var response = VoicevoxClient.Inst.SendRequest(queryurl);
            VoicevoxNote configs;
            var jObj = JObject.Parse(response.Item1);
            if (jObj.ContainsKey("detail")) {
                Log.Error($"Response was incorrect. : {jObj}");
            } else {
                configs = jObj.ToObject<VoicevoxNote>();
                return configs;
            }
            return new VoicevoxNote();
        }

        public static void Loaddic(VoicevoxSinger singer) {
            dic.Loaddic(singer.Location);
        }

        public static VoicevoxQueryMain NoteGroupsToVoicevox(Note[][] notes, TimeAxis timeAxis) {
            VoicevoxQueryMain qnotes = new VoicevoxQueryMain();
            int index = 0;
            int duration = 0;
            try {
                qnotes.notes.Add(new VoicevoxQueryNotes() {
                    lyric = "",
                    frame_length = (int)Math.Round((headS * fps), MidpointRounding.AwayFromZero),
                    key = null,
                    vqnindex = -1
                });
                duration = notes[index][0].position + notes[index][0].duration;
                while (index < notes.Length) {
                    string lyric = dic.Notetodic(notes, index);
                    int length = (int)Math.Round(((timeAxis.TickPosToMsPos(notes[index].Sum(n => n.duration)) / 1000f) * VoicevoxUtils.fps), MidpointRounding.AwayFromZero);
                    //Avoid synthesis without at least two frames.
                    if (length < 2) {
                        length = 2;
                    }
                    int? tone = null;
                    if (!string.IsNullOrEmpty(lyric)) {
                        if (notes[index][0].phonemeAttributes != null) {
                            if (notes[index][0].phonemeAttributes.Length > 0) {
                                tone = notes[index][0].tone + notes[index][0].phonemeAttributes[0].toneShift;
                            } else {
                                tone = notes[index][0].tone;
                            }
                        } else {
                            tone = notes[index][0].tone;
                        }
                    } else {
                        lyric = "";
                    }
                    qnotes.notes.Add(new VoicevoxQueryNotes {
                        lyric = lyric,
                        frame_length = length,
                        key = tone,
                        vqnindex = index
                    });
                    duration += notes[index][0].duration;
                    index++;
                }
                qnotes.notes.Add(new VoicevoxQueryNotes {
                    lyric = "",
                    frame_length = (int)(tailS * fps),
                    key = null,
                    vqnindex = index
                });

            } catch (Exception e) {
                Log.Error($"VoicevoxQueryNotes setup error.");
            }
            return qnotes;
        }

        public static double[] SampleCurve(RenderPhrase phrase, float[] curve, double defaultValue, double frameMs, int length, int headFrames, int tailFrames, double offset, Func<double, double> convert) {
            const int interval = 5;
            var result = new double[length];
            try {
                if (curve == null) {
                    Array.Fill(result, defaultValue);
                    return result;
                }

                for (int i = 0; i < length - headFrames - tailFrames; i++) {
                    double posMs = phrase.positionMs - phrase.leadingMs + (i * frameMs) + offset;
                    int ticks = phrase.timeAxis.MsPosToTickPos(posMs) - (phrase.position - phrase.leading);
                    int index = Math.Max(0, (int)((double)ticks / interval));
                    if (index < curve.Length) {
                        result[i + headFrames] = convert(curve[index]);
                    }
                }
                //Fill head and tail
                Array.Fill(result, convert(curve[0]), 0, headFrames);
                Array.Fill(result, convert(curve[^1]), length - tailFrames, tailFrames);
            } catch (Exception e) {
                Log.Error($"SampleCurve:{e}");
            }
            return result;
        }

        public static bool IsPau(string s) {
            return phoneme_List.paus.ContainsKey(s);
        }

        public static bool TryGetPau(string s, out string str) {
            phoneme_List.paus.TryGetValue(s, out str);
            return phoneme_List.paus.ContainsKey(s);
        }

        public static string getBaseSingerID(VoicevoxSinger singer) {
            if (singer.voicevoxConfig.base_singer_style != null) {
                foreach (var s in singer.voicevoxConfig.base_singer_style) {
                    if (s.name.Equals(singer.voicevoxConfig.base_singer_name)) {
                        if (s.styles.name.Equals(singer.voicevoxConfig.base_singer_style_name)) {
                            return s.styles.id.ToString();
                        }
                    }
                }
            }
            return defaultID;
        }
    }
}

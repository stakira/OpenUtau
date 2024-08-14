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
        public string[] consonants = "k s sh t ts ch n h f my r w z j d b p w v ky ny hy my ry gy by py ty dy".Split();
        public Dictionary<string, string> kanas = new Dictionary<string, string>();
        public Dictionary<string, string> paus = new Dictionary<string, string>();
        public Phoneme_list() {
            var kanaGroups = new List<string[]> {
                "あ か が さ ざ た だ な は ば ぱ ま や ら わ きゃ ぎゃ しゃ じゃ ちゃ てゃ でゃ にゃ ひゃ びゃ ぴゃ ふぁ みゃ りゃ ぁ ゃ ア カ ガ サ ザ タ ダ ナ ハ バ パ マ ヤ ラ ワ キャ ギャ シャ ジャ チャ テャ デャ ニャ ヒャ ビャ ピャ ファ ミャ リャ ァ ャ a".Split(),
                "い き ぎ し じ ち ぢ に ひ び ぴ み り ゐ いぇ ヴぁ うぃ ヴぃ すぃ ずぃ つぃ てぃ でぃ ふぃ ぃ イ キ ギ シ ジ チ ヂ ニ ヒ ビ ピ ミ リ ヰ イェ ヴァ ウィ ヴィ スィ ズィ ツィ ティ ディ フィ ィ i".Split(),
                "う く ぐ す ず つ づ ぬ ふ ぶ ぷ む ゆ る きゅ ぎゅ しゅ じゅ ちゅ てゅ でゅ とぅ どぅ にゅ ひゅ びゅ ぴゅ みゅ りゅ ぅ ゅ ウ ク グ ス ズ ツ ヅ ヌ フ ブ プ ム ユ ル ヴ キュ ギュ シュ ジュ チュ テュ デュ トゥ ドゥ ニュ ヒュ ビュ ピュ ミュ リュ ゥ ュ u".Split(),
                "え け げ せ ぜ て で ね へ べ ぺ め れ ゑ うぇ ヴぇ きぇ ぎぇ しぇ じぇ ちぇ つぇ にぇ ひぇ びぇ ぴぇ ふぇ みぇ りぇ ぇ エ ケ ゲ セ ゼ テ デ ネ ヘ ベ ペ メ レ ヱ ウェ ヴェ キェ ギェ シェ ジェ チェ ツェ ニェ ヒェ ビェ ピェ フェ ミェ リェ ェ e".Split(),
                "お こ ご そ ぞ と ど の ほ ぼ ぽ も よ ろ を うぉ ヴぉ きょ ぎょ しょ じょ ちょ つぉ てょ でょ にょ ひょ びょ ぴょ ふぉ みょ りょ ぉ ょ オ コ ゴ ソ ゾ ト ド ノ ホ ボ ポ モ ヨ ロ ヲ ウォ ヴォ キョ ギョ ショ ジョ チョ ツォ テョ デョ ニョ ヒョ ビョ ピョ フォ ミョ リョ ォ ョ o".Split(),
                "ん ン n ng".Split(),
                "っ ッ cl".Split()
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

        public static double[] SampleCurve(RenderPhrase phrase, float[] curve, double defaultValue, double frameMs, int length, int headFrames, int tailFrames, Func<double, double> convert) {
            const int interval = 5;
            var result = new double[length]; 
            try {
                if (curve == null) {
                    Array.Fill(result, defaultValue);
                    return result;
                }

                for (int i = 0; i < length - headFrames - tailFrames; i++) {
                    double posMs = phrase.positionMs - phrase.leadingMs + i * frameMs;
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

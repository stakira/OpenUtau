using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenUtau.Core.Render;
using Serilog;
using static OpenUtau.Api.Phonemizer;

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


    internal static class VoicevoxUtils {
        public const string VOLC = "volc";
        public const int headS = 1;
        public const int tailS = 1;
        public const double fps = 93.75;
        public const string defaultID = "6000";

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

        public static VoicevoxQueryMain NoteGroupsToVoicevox(Note[][] notes, TimeAxis timeAxis, VoicevoxSinger singer) {
            if (!VoicevoxUtils.IsHiraKana(notes[0][0].lyric) || !VoicevoxUtils.IsPau(notes[0][0].lyric)) {
                BaseChinesePhonemizer.RomanizeNotes(notes);
            }
            VoicevoxQueryMain qnotes = new VoicevoxQueryMain();
            Dictionary_list dic = new Dictionary_list();
            dic.Loaddic(singer.Location);
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
                    string lyric = dic.Lyrictodic(notes, index);
                    int length = (int)Math.Round(((timeAxis.TickPosToMsPos(notes[index].Sum(n => n.duration)) / 1000f) * VoicevoxUtils.fps), MidpointRounding.AwayFromZero);
                    //Avoid synthesis without at least two frames.
                    if (length < 2 ) {
                        length = 2;
                    }
                    int? tone = null;
                    if (!string.IsNullOrEmpty(lyric) || VoicevoxUtils.IsPau(lyric)) {
                        if (notes[index][0].phonemeAttributes != null) {
                            if (notes[index][0].phonemeAttributes.Length > 0) {
                                tone = notes[index][0].tone + notes[index][0].phonemeAttributes[0].toneShift;
                            } else {
                                tone = notes[index][0].tone;
                            }
                        } else {
                            tone = notes[index][0].tone;
                        }
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
            if (curve == null) {
                Array.Fill(result, defaultValue);
                return result;
            }

            for (int i = 0; i < length; i++) {
                double posMs = phrase.positionMs - phrase.leadingMs + i * frameMs;
                int ticks = phrase.timeAxis.MsPosToTickPos(posMs) - (phrase.position - phrase.leading);
                int index = Math.Max(0, (int)((double)ticks / interval));
                if (index < curve.Length) {
                    result[i] = convert(curve[index]);
                }
            }
            //Fill head and tail
            Array.Fill(result, convert(curve[0]), 0, headFrames);
            Array.Fill(result, convert(curve[^1]), length - tailFrames, tailFrames);
            return result;
        }


        public static bool IsHiraKana(string s) {
            foreach(char c in s.ToCharArray()) {
                if (!('\u3041' <= c && c <= '\u309F') || ('\u30A0' <= c && c <= '\u30FF') || c == '\u30FC' || c == '\u30A0') {
                    return false;
                }
            }
            return true;
        }

        public static bool IsPau(string s) {
            if (s.EndsWith("R") || s.ToLower().EndsWith("pau") || s.EndsWith("AP") || s.EndsWith("SP")) {
                return true;
            }
            return false;
        }
    }
}

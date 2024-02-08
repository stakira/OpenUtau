using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using OpenUtau.Core.Ustx;
using Serilog;
using static OpenUtau.Api.Phonemizer;

namespace OpenUtau.Core.Voicevox {
    public class Phonemes {
        public string phoneme;
        public int frame_length;

    }
    public class VoicevoxNote {
        public IList<float> f0;
        public IList<float> volume;
        public IList<Phonemes> phonemes;
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
        public static VoicevoxQueryMain NoteGroupsToVoicevox(Note[][] notes, TimeAxis timeAxis) {
            BaseChinesePhonemizer.RomanizeNotes(notes);
            VoicevoxQueryMain qnotes = new VoicevoxQueryMain();
            int index = 0;
            int position = 0;
            while (index < notes.Length) {
                if (position < notes[index][0].position) {
                    qnotes.notes.Add(new VoicevoxQueryNotes() {
                        lyric = "",
                        frame_length = notes[index][0].position - position/10,
                        key = null,
                        vqnindex = -1
                    });
                    position = notes[index][0].position;
                } else {
                    qnotes.notes.Add(new VoicevoxQueryNotes {
                        lyric = notes[index][0].lyric,
                        frame_length = (int)timeAxis.TickPosToMsPos(notes[index].Sum(n => n.duration))/60,
                        key = notes[index][0].tone,
                        vqnindex = index
                    });
                    position += qnotes.notes.Last().frame_length;
                    index++;
                }
            }
            return qnotes;
        }

        public static VoicevoxNote VoicevoxVoiceBase(VoicevoxQueryMain qNotes, string id) {
            var ins = VoicevoxClient.Inst;
            try {
                var queryurl = new VoicevoxURL() { method = "POST", path = "/sing_frame_audio_query", query = new Dictionary<string, string> { { "speaker", id } }, body = JsonConvert.SerializeObject(qNotes) };
                ins.SendRequest(queryurl);
                var configs = ins.jObj.ToObject<VoicevoxNote>();
                return configs;
            } catch {
                Log.Error($"Failed to create a voice base. : {ins.jObj}");
            }
            return new VoicevoxNote();
        }
    }
}

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
        public List<float> f0;
        public List<float> volume;
        public List<Phonemes> phonemes;
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

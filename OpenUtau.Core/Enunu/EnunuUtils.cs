using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Core.Enunu {
    public struct VersionResult {
        public string name;
        public string version;
        public string author;
    }

    public struct VersionResponse {
        public string error;
        public VersionResult result;
    }

    public struct EnunuNote {
        public string lyric;
        public int length;
        public int noteNum;
        public int noteIndex;
        public int style_shift;
        public string timbre;
        public int velocity;
    }

    internal static class EnunuUtils {
        static readonly Encoding ShiftJIS = Encoding.GetEncoding("shift_jis");
        static readonly Encoding UTF8 = Encoding.UTF8;

        internal static void WriteUst(IList<EnunuNote> notes, double tempo, USinger singer, string ustPath) {
            WriteUst(notes, tempo, singer, ustPath, ShiftJIS);
        }

        internal static void WriteUst(IList<EnunuNote> notes, double tempo, USinger singer, string ustPath, Encoding encoding) {
            using (var writer = new StreamWriter(ustPath, false, encoding)) {
                writer.WriteLine("[#SETTING]");
                writer.WriteLine($"Tempo={tempo}");
                writer.WriteLine("Tracks=1");
                writer.WriteLine($"Project={ustPath}");
                writer.WriteLine($"VoiceDir={singer.Location}");
                writer.WriteLine($"CacheDir={PathManager.Inst.CachePath}");
                writer.WriteLine("Mode2=True");
                for (int i = 0; i < notes.Count; ++i) {
                    writer.WriteLine($"[#{i}]");
                    writer.WriteLine($"Lyric={notes[i].lyric}");
                    writer.WriteLine($"Length={notes[i].length}");
                    writer.WriteLine($"NoteNum={notes[i].noteNum}");
                    writer.WriteLine($"Velocity={notes[i].velocity}");
                    if (!string.IsNullOrEmpty(notes[i].timbre)) {
                        writer.WriteLine($"Flags={notes[i].timbre}S{notes[i].style_shift}");
                    }
                }
                writer.WriteLine("[#TRACKEND]");
            }
        }

        internal static string SetPortNum() {
             var ver_response = EnunuClient.Inst.SendRequest<VersionResponse>(new string[] { "ver_check" }, "15556", 1);
             if (ver_response.error != null) {
                 Log.Error(ver_response.error);
             } else if (ver_response.result.name != null) {
                 return "15556";
             }
            return "15555";
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Enunu {
    public struct VersionResult {
        public string name;
        public string version;
        public string author;
        /// <summary>Added by ENUNUServer 1.0. Null on older servers.</summary>
        public EnunuServerFeatures? features;
    }

    public class EnunuServerFeatures {
        public string[] commands;
        public bool style_shift;
        public bool pitch_n_frames;
        public Dictionary<string, EnunuDiffusionSetting>? diffusion;

        public bool Has(string command) => commands != null && commands.Contains(command);
        public bool SupportsPitch => Has(EnunuCommand.Pitch) && Has(EnunuCommand.AcousticF0);
    }

    public class EnunuDiffusionSetting {
        public string method;
        public int steps;
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
                    string flags = "Flags=";
                    if (!string.IsNullOrEmpty(notes[i].timbre)) {
                        flags += notes[i].timbre;
                    }
                    flags += notes[i].style_shift != 0 ? $"S{notes[i].style_shift}" : "";
                    writer.WriteLine(flags);
                }
                writer.WriteLine("[#TRACKEND]");
            }
        }
    }
}

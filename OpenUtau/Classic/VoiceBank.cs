using System.Collections.Generic;
using System.IO;
using System.Text;
using Serilog;

namespace OpenUtau.Classic {

    /// <summary>
    /// A sound bank. Corresponds to a single oto.ini file.
    /// </summary>
    public class Voicebank {

        private Voicebank() {
        }

        public string BasePath { private set; get; }
        public string OtoFile { private set; get; }
        public List<Oto> OtoList { private set; get; }
        public Encoding OtoEncoding { private set; get; }
        public Encoding PathEncoding { private set; get; }

        public class Builder {
            private readonly Voicebank voicebank;
            private readonly byte[] otoBytes;
            private int audioFilesFound;

            public Builder(string otoFile) {
                otoBytes = File.ReadAllBytes(otoFile);
                voicebank = new Voicebank() {
                    OtoFile = otoFile,
                    BasePath = Path.GetDirectoryName(otoFile),
                    PathEncoding = Encoding.Default,
                };
            }

            public string TestOtoEncoding(Encoding encoding) {
                return encoding.GetString(otoBytes);
            }

            public Builder SetOtoEncoding(Encoding encoding) {
                voicebank.OtoEncoding = encoding;
                var lines = new List<string>();
                using (var stream = new StreamReader(new MemoryStream(otoBytes), encoding)) {
                    while (!stream.EndOfStream) {
                        lines.Add(stream.ReadLine());
                    }
                }
                var otoList = new List<Oto>();
                foreach (var line in lines) {
                    var oto = Oto.Parse(line);
                    if (oto != null) {
                        otoList.Add(oto);
                    }
                }
                voicebank.OtoList = otoList;
                return this;
            }

            public double TestPathEncoding(Encoding encoding) {
                if (voicebank.OtoList == null) {
                    return 0;
                }
                if (voicebank.OtoList.Count == 0) {
                    return 1;
                }
                audioFilesFound = 0;
                foreach (var oto in voicebank.OtoList) {
                    try {
                        var file = encoding.GetString(voicebank.OtoEncoding.GetBytes(oto.AudioFile));
                        if (File.Exists(Path.Combine(voicebank.BasePath, file))) {
                            audioFilesFound++;
                        }
                    } catch { }
                }
                return (double)audioFilesFound / voicebank.OtoList.Count;
            }

            public Builder SetPathEncoding(Encoding encoding) {
                voicebank.PathEncoding = encoding;
                return this;
            }

            public Voicebank Build() {
                var score = TestPathEncoding(voicebank.PathEncoding);
                Log.Information("Loaded {0} otos and {1:F2}% audio files from {2}",
                    voicebank.OtoList.Count, score * 100, voicebank.OtoFile);
                return voicebank;
            }
        }
    }
}

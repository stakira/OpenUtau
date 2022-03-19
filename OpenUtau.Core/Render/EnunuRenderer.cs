using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using OpenUtau.Core.Format;
using Serilog;

namespace OpenUtau.Core.Render {
    class EnunuRenderer : IRenderer {
        static readonly Encoding ShiftJIS = Encoding.GetEncoding("shift_jis");
        static readonly object lockObj = new object();

        static string workDir = null;
        static string python = null;
        static string script = null;

        static void Init() {
            if (string.IsNullOrEmpty(python) || string.IsNullOrEmpty(script)) {
                var plugin = DocManager.Inst.Plugins.First(plugin => plugin.Name.ToLowerInvariant().Contains("enunu"));
                if (!File.Exists(plugin.Executable)) {
                    return;
                }
                var lines = File.ReadAllLines(plugin.Executable);
                var line = lines.First(line => line.Contains("python"));
                var parts = line.Split();
                workDir = Path.GetDirectoryName(plugin.Executable);
                python = Path.Join(workDir, parts[0]);
                script = parts[1];
            }
        }

        struct EnunuNote {
            public string lyric;
            public int length;
            public int noteNum;
        }

        public RenderResult Layout(RenderPhrase phrase) {
            var firstPhone = phrase.phones.First();
            var lastPhone = phrase.phones.Last();
            return new RenderResult() {
                leadingMs = 240 * phrase.tickToMs,
                positionMs = (phrase.position + firstPhone.position) * phrase.tickToMs,
                estimatedLengthMs = (lastPhone.duration + lastPhone.position - firstPhone.position + 480) * phrase.tickToMs,
            };
        }

        public Task<RenderResult> Render(RenderPhrase phrase, Progress progress, CancellationTokenSource cancellation, bool isPreRender) {
            var task = Task.Run(() => {
                lock (lockObj) {
                    if (cancellation.IsCancellationRequested) {
                        return new RenderResult();
                    }
                    Init();
                    var ustPath = Path.Join(PathManager.Inst.CachePath, $"enu-{phrase.hash}.tmp");
                    var wavPath = ustPath.Substring(0, ustPath.Length - 4) + ".wav";
                    if (!File.Exists(wavPath)) {
                        InvokeEnunu(phrase, ustPath, wavPath, isPreRender);
                    }
                    foreach (var phone in phrase.phones) {
                        progress.CompleteOne(phone.phoneme);
                    }
                    var result = Layout(phrase);
                    if (File.Exists(wavPath)) {
                        using (var waveStream = Wave.OpenFile(wavPath)) {
                            result.samples = Wave.GetSamples(waveStream.ToSampleProvider().ToMono(1, 0));
                        }
                        if (result.samples != null) {
                            ApplyDynamics(phrase, result.samples);
                        }
                    } else {
                        result.samples = new float[0];
                    }
                    return result;
                }
            });
            return task;
        }

        private void WriteUst(RenderPhrase phrase, string ustPath) {
            var notes = new List<EnunuNote>();
            notes.Add(new EnunuNote {
                lyric = "R",
                length = 240,
                noteNum = 60,
            });
            foreach (var phone in phrase.phones) {
                notes.Add(new EnunuNote {
                    lyric = phone.phoneme,
                    length = phone.duration,
                    noteNum = phone.tone,
                });
            }
            notes.Add(new EnunuNote {
                lyric = "R",
                length = 240,
                noteNum = 60,
            });
            using (var writer = new StreamWriter(ustPath, false, ShiftJIS)) {
                writer.WriteLine("[#SETTING]");
                writer.WriteLine($"Tempo={phrase.tempo}");
                writer.WriteLine("Tracks=1");
                writer.WriteLine($"VoiceDir={phrase.singer.Location}");
                writer.WriteLine($"CacheDir={PathManager.Inst.CachePath}");
                writer.WriteLine("Mode2=True");
                for (int i = 0; i < notes.Count; ++i) {
                    writer.WriteLine($"[#{i}]");
                    writer.WriteLine($"Lyric={notes[i].lyric}");
                    writer.WriteLine($"Length={notes[i].length}");
                    writer.WriteLine($"NoteNum={notes[i].noteNum}");
                }
                writer.WriteLine("[#TRACKEND]");
            }
        }

        private void InvokeEnunu(RenderPhrase phrase, string ustPath, string wavPath, bool createNoWindow) {
            WriteUst(phrase, ustPath);
            var startInfo = new ProcessStartInfo() {
                FileName = python,
                Arguments = $"{script} {ustPath} {wavPath}",
                WorkingDirectory = workDir,
                CreateNoWindow = !Util.Preferences.Default.ResamplerLogging,
            };
            try {
                using (var process = Process.Start(startInfo)) {
                    process.WaitForExit();
                }
            } catch (Exception e) {
                Log.Error(e, "Failed to run Enunu");
            }
        }

        void ApplyDynamics(RenderPhrase phrase, float[] samples) {
            const int interval = 5;
            if (phrase.dynamics == null) {
                return;
            }
            int pos = 0;
            int offset = (int)(240 * phrase.tickToMs / 1000 * 44100);
            for (int i = 0; i < phrase.dynamics.Length; ++i) {
                int endPos = (int)((i + 1) * interval * phrase.tickToMs / 1000 * 44100);
                float a = phrase.dynamics[i];
                float b = (i + 1) == phrase.dynamics.Length ? phrase.dynamics[i] : phrase.dynamics[i + 1];
                for (int j = pos; j < endPos; ++j) {
                    samples[offset + j] *= a + (b - a) * (j - pos) / (endPos - pos);
                }
                pos = endPos;
            }
        }
    }
}

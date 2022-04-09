using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using K4os.Hash.xxHash;
using NAudio.Wave;
using NumSharp;
using OpenUtau.Core.Format;
using OpenUtau.Core.Render;
using OpenUtau.Core.SignalChain;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Core.Enunu {
    class EnunuRenderer : IRenderer {
        const int headTicks = 240;
        const int tailTicks = 240;

        static readonly HashSet<string> supportedExp = new HashSet<string>(){
            Format.Ustx.DYN,
            Format.Ustx.PITD,
            Format.Ustx.GENC,
            Format.Ustx.BREC,
            Format.Ustx.TENC,
            Format.Ustx.VOIC,
        };

        static readonly Encoding ShiftJIS = Encoding.GetEncoding("shift_jis");
        static readonly object lockObj = new object();

        public bool SupportsRenderPitch => true;

        public bool SupportsExpression(UExpressionDescriptor descriptor) {
            return supportedExp.Contains(descriptor.abbr);
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
                leadingMs = headTicks * phrase.tickToMs,
                positionMs = (phrase.position + firstPhone.position) * phrase.tickToMs,
                estimatedLengthMs = (lastPhone.duration + lastPhone.position - firstPhone.position + headTicks + tailTicks) * phrase.tickToMs,
            };
        }

        public Task<RenderResult> Render(RenderPhrase phrase, Progress progress, CancellationTokenSource cancellation, bool isPreRender) {
            var task = Task.Run(() => {
                lock (lockObj) {
                    if (cancellation.IsCancellationRequested) {
                        return new RenderResult();
                    }
                    EnunuInit.Init();
                    ulong preEffectHash = PreEffectsHash(phrase);
                    var tmpPath = Path.Join(PathManager.Inst.CachePath, $"enu-{preEffectHash:x16}");
                    var ustPath = tmpPath + ".tmp";
                    var wavPath = Path.Join(PathManager.Inst.CachePath, $"enu-{phrase.hash:x16}.wav");
                    var result = Layout(phrase);
                    if (!File.Exists(wavPath)) {
                        var f0Path = Path.Join(tmpPath, "acoustic-f0.npy");
                        var spPath = Path.Join(tmpPath, "acoustic-sp.npy");
                        var apPath = Path.Join(tmpPath, "acoustic-ap.npy");
                        if (!File.Exists(f0Path) || !File.Exists(spPath) || !File.Exists(apPath)) {
                            InvokeEnunu(phrase, "all", ustPath);
                        }
                        if (cancellation.IsCancellationRequested) {
                            return new RenderResult();
                        }
                        var config = EnunuConfig.Load(phrase.singer);
                        var f0 = np.Load<double[]>(f0Path);
                        var sp = np.Load<double[,]>(spPath);
                        var ap = np.Load<double[,]>(apPath);
                        int totalFrames = f0.Length;
                        int headFrames = (int)Math.Round(headTicks * phrase.tickToMs / config.framePeriod);
                        int tailFrames = (int)Math.Round(tailTicks * phrase.tickToMs / config.framePeriod);
                        var editorF0 = DownSampleCurve(phrase.pitches, 0, config.framePeriod, totalFrames, headFrames, tailFrames, phrase.tickToMs, x => MusicMath.ToneToFreq(x * 0.01));
                        var gender = DownSampleCurve(phrase.gender, 0.5, config.framePeriod, totalFrames, headFrames, tailFrames, phrase.tickToMs, x => 0.5 + 0.005 * x);
                        var tension = DownSampleCurve(phrase.tension, 0.5, config.framePeriod, totalFrames, headFrames, tailFrames, phrase.tickToMs, x => 0.5 + 0.005 * x);
                        var breathiness = DownSampleCurve(phrase.breathiness, 0.5, config.framePeriod, totalFrames, headFrames, tailFrames, phrase.tickToMs, x => 0.5 + 0.005 * x);
                        var voicing = DownSampleCurve(phrase.voicing, 1.0, config.framePeriod, totalFrames, headFrames, tailFrames, phrase.tickToMs, x => 0.01 * x);
                        int fftSize = (sp.GetLength(1) - 1) * 2;
                        for (int i = 0; i < f0.Length; i++) {
                            if (f0[i] < 50) {
                                editorF0[i] = 0;
                            }
                        }
                        var samples = Worldline.WorldSynthesis(
                            editorF0,
                            sp, false, sp.GetLength(1),
                            ap, false, fftSize,
                            config.framePeriod, config.sampleRate,
                            gender, tension, breathiness, voicing);
                        result.samples = samples.Select(d => (float)d).ToArray();
                        Wave.CorrectSampleScale(result.samples);
                        var source = new WaveSource(0, 0, 0, 1);
                        source.SetSamples(result.samples);
                        WaveFileWriter.CreateWaveFile16(wavPath, new ExportAdapter(source).ToMono(1, 0));
                    }
                    string joined = string.Join(" ", phrase.phones.Select(p => p.phoneme));
                    foreach (var phone in phrase.phones) {
                        progress.CompleteOne(joined);
                    }
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

        public RenderPitchResult LoadRenderedPitch(RenderPhrase phrase) {
            EnunuInit.Init();
            ulong preEffectHash = PreEffectsHash(phrase);
            var tmpPath = Path.Join(PathManager.Inst.CachePath, $"enu-{preEffectHash:x16}");
            var ustPath = tmpPath + ".tmp";
            var wavPath = Path.Join(PathManager.Inst.CachePath, $"enu-{phrase.hash:x16}.wav");
            var f0Path = Path.Join(tmpPath, "acoustic-f0.npy");
            var layout = Layout(phrase);
            if (!File.Exists(f0Path)) {
                return null;
            }
            var config = EnunuConfig.Load(phrase.singer);
            var f0 = np.Load<double[]>(f0Path);
            var result = new RenderPitchResult() {
                tones = f0.Select(f => (float)MusicMath.FreqToTone(f)).ToArray(),
            };
            result.ticks = new float[result.tones.Length];
            var t = layout.positionMs - layout.leadingMs;
            for (int i = 0; i < result.tones.Length; i++) {
                t += config.framePeriod;
                result.ticks[i] = (float)(t / phrase.tickToMs) - phrase.position;
            }
            return result;
        }

        private ulong PreEffectsHash(RenderPhrase phrase) {
            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {
                    writer.Write(phrase.singerId);
                    writer.Write(phrase.tempo);
                    writer.Write(phrase.tickToMs);
                    foreach (var phone in phrase.phones) {
                        writer.Write(phone.hash);
                    }
                    return XXH64.DigestOf(stream.ToArray());
                }
            }
        }

        private void WriteUst(RenderPhrase phrase, string ustPath) {
            var notes = new List<EnunuNote>();
            notes.Add(new EnunuNote {
                lyric = "R",
                length = headTicks,
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
                length = tailTicks,
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

        private void InvokeEnunu(RenderPhrase phrase, string phase, string ustPath) {
            Log.Information($"Starting enunu {phase} \"{ustPath}\"");
            WriteUst(phrase, ustPath);
            string args = $"{EnunuInit.Script} {phase} \"{ustPath}\"";
            Util.ProcessRunner.Run(EnunuInit.Python, args, Log.Logger, workDir: EnunuInit.WorkDir, timeoutMs: 0);
        }

        double[] DownSampleCurve(float[] curve, double defaultValue, double frameMs, int length, int headFrames, int tailFrames, double tickToMs, Func<double, double> convert) {
            const int interval = 5;
            var result = new double[length];
            if (curve == null) {
                Array.Fill(result, defaultValue);
                return result;
            }
            for (int i = 0; i < length - headFrames - tailFrames; i++) {
                int index = (int)(i * frameMs / tickToMs / interval);
                if (index < curve.Length) {
                    result[i + headFrames] = convert(curve[index]);
                }
            }
            Array.Fill(result, defaultValue, 0, headFrames);
            Array.Fill(result, defaultValue, length - tailFrames, tailFrames);
            return result;
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

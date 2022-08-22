using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NumSharp;
using OpenUtau.Core.Format;
using OpenUtau.Core.Render;
using OpenUtau.Core.SignalChain;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Core.Enunu {
    public class EnunuRenderer : IRenderer {
        public const int headTicks = 240;
        public const int tailTicks = 240;

        static readonly HashSet<string> supportedExp = new HashSet<string>(){
            Format.Ustx.DYN,
            Format.Ustx.CLR,
            Format.Ustx.PITD,
            Format.Ustx.GENC,
            Format.Ustx.BREC,
            Format.Ustx.TENC,
            Format.Ustx.VOIC,
        };

        struct AcousticResult {
            public string path_acoustic;
            public string path_f0;
            public string path_spectrogram;
            public string path_aperiodicity;
        }

        struct AcousticResponse {
            public string error;
            public AcousticResult result;
        }

        static readonly object lockObj = new object();

        public USingerType SingerType => USingerType.Enunu;

        public bool SupportsRenderPitch => true;

        public bool SupportsExpression(UExpressionDescriptor descriptor) {
            return supportedExp.Contains(descriptor.abbr);
        }

        public RenderResult Layout(RenderPhrase phrase) {
            var firstPhone = phrase.phones.First();
            var lastPhone = phrase.phones.Last();
            var headMs = firstPhone.positionMs - phrase.timeAxis.TickPosToMsPos(firstPhone.position - headTicks);
            var tailMs = phrase.timeAxis.TickPosToMsPos(lastPhone.position + lastPhone.duration + tailTicks) - lastPhone.endMs;
            return new RenderResult() {
                leadingMs = headMs,
                positionMs = firstPhone.positionMs,
                estimatedLengthMs = headMs + (lastPhone.positionMs - firstPhone.positionMs) + tailMs,
            };
        }

        public Task<RenderResult> Render(RenderPhrase phrase, Progress progress, CancellationTokenSource cancellation, bool isPreRender) {
            var task = Task.Run(() => {
                lock (lockObj) {
                    if (cancellation.IsCancellationRequested) {
                        return new RenderResult();
                    }
                    string progressInfo = string.Join(" ", phrase.phones.Select(p => p.phoneme));
                    progress.Complete(0, progressInfo);
                    var tmpPath = Path.Join(PathManager.Inst.CachePath, $"enu-{phrase.preEffectHash:x16}");
                    var ustPath = tmpPath + ".tmp";
                    var enutmpPath = tmpPath + "_enutemp";
                    var wavPath = Path.Join(PathManager.Inst.CachePath, $"enu-{phrase.hash:x16}.wav");
                    var result = Layout(phrase);
                    if (!File.Exists(wavPath)) {
                        var f0Path = Path.Join(enutmpPath, "f0.npy");
                        var spPath = Path.Join(enutmpPath, "spectrogram.npy");
                        var apPath = Path.Join(enutmpPath, "aperiodicity.npy");
                        if (!File.Exists(f0Path) || !File.Exists(spPath) || !File.Exists(apPath)) {
                            Log.Information($"Starting enunu acoustic \"{ustPath}\"");
                            var enunuNotes = PhraseToEnunuNotes(phrase);
                            // TODO: using first note tempo as ust tempo.
                            EnunuUtils.WriteUst(enunuNotes, phrase.phones.First().tempo, phrase.singer, ustPath);
                            var response = EnunuClient.Inst.SendRequest<AcousticResponse>(new string[] { "acoustic", ustPath });
                            if (response.error != null) {
                                throw new Exception(response.error);
                            }
                        }
                        if (cancellation.IsCancellationRequested) {
                            return new RenderResult();
                        }
                        var config = EnunuConfig.Load(phrase.singer);
                        var f0 = np.Load<double[]>(f0Path);
                        var sp = np.Load<double[,]>(spPath);
                        var ap = np.Load<double[,]>(apPath);
                        int totalFrames = f0.Length;
                        var firstPhone = phrase.phones.First();
                        var lastPhone = phrase.phones.Last();
                        var headMs = firstPhone.positionMs - phrase.timeAxis.TickPosToMsPos(firstPhone.position - headTicks);
                        var tailMs = phrase.timeAxis.TickPosToMsPos(lastPhone.position + lastPhone.duration + tailTicks) - lastPhone.endMs;
                        int headFrames = (int)Math.Round(headMs / config.framePeriod);
                        int tailFrames = (int)Math.Round(tailMs / config.framePeriod);
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
                        if (config.sampleRate != 44100) {
                            var signal = new NWaves.Signals.DiscreteSignal(config.sampleRate, result.samples);
                            signal = NWaves.Operations.Operation.Resample(signal, 44100);
                            result.samples = signal.Samples;
                        }
                        var source = new WaveSource(0, 0, 0, 1);
                        source.SetSamples(result.samples);
                        WaveFileWriter.CreateWaveFile16(wavPath, new ExportAdapter(source).ToMono(1, 0));
                    }
                    progress.Complete(phrase.phones.Length, progressInfo);
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
            var tmpPath = Path.Join(PathManager.Inst.CachePath, $"enu-{phrase.preEffectHash:x16}");
            var enutmpPath = tmpPath + "_enutemp";
            var f0Path = Path.Join(enutmpPath, "f0.npy");
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

        static EnunuNote[] PhraseToEnunuNotes(RenderPhrase phrase) {
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
                    timbre = phone.suffix,
                });
            }
            notes.Add(new EnunuNote {
                lyric = "R",
                length = tailTicks,
                noteNum = 60,
            });
            return notes.ToArray();
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

        public override string ToString() => "ENUNU";
    }
}

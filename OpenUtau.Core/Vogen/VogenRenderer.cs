using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NAudio.Wave;
using NumSharp;
using OpenUtau.Core.Format;
using OpenUtau.Core.Render;
using OpenUtau.Core.SignalChain;
using OpenUtau.Core.Ustx;
using Serilog;
using VocalShaper;
using VocalShaper.World;

namespace OpenUtau.Core.Vogen {
    public class VogenRenderer : IRenderer {
        const int fs = 44100;
        const int fftSize = 2048;
        const float headMs = 500;
        const float tailMs = 500;
        const float frameMs = 10;

        static readonly object lockObj = new object();

        static readonly HashSet<string> supportedExp = new HashSet<string>(){
            Format.Ustx.DYN,
            Format.Ustx.PITD,
            Format.Ustx.GENC,
            Format.Ustx.BREC,
            Format.Ustx.SHFC,
            Format.Ustx.TENC,
            Format.Ustx.VOIC,
        };

        static readonly VSVocoder vocoder = new VSVocoder(fs, frameMs);

        public USingerType SingerType => USingerType.Vogen;

        public bool SupportsRenderPitch => true;

        public bool SupportsExpression(UExpressionDescriptor descriptor) {
            return supportedExp.Contains(descriptor.abbr);
        }

        public RenderResult Layout(RenderPhrase phrase) {
            var firstPhone = phrase.phones.First();
            var lastPhone = phrase.phones.Last();
            return new RenderResult() {
                leadingMs = headMs,
                positionMs = firstPhone.positionMs,
                estimatedLengthMs = headMs + (lastPhone.positionMs - firstPhone.positionMs) + tailMs,
            };
        }

        public Task<RenderResult> Render(RenderPhrase phrase, Progress progress, CancellationTokenSource cancellation, bool isPreRender = false) {
            var task = Task.Run(() => {
                lock (lockObj) {
                    if (cancellation.IsCancellationRequested) {
                        return new RenderResult();
                    }
                    var result = Layout(phrase);
                    var wavPath = Path.Join(PathManager.Inst.CachePath, $"vog-{phrase.hash:x16}.wav");
                    string progressInfo = string.Join(" ", phrase.phones.Select(p => p.phoneme));
                    progress.Complete(0, progressInfo);
                    if (File.Exists(wavPath)) {
                        try {
                            using (var waveStream = Wave.OpenFile(wavPath)) {
                                result.samples = Wave.GetSamples(waveStream.ToSampleProvider().ToMono(1, 0));
                            }
                        } catch (Exception e) {
                            Log.Error(e, "Failed to render.");
                        }
                    }
                    if (result.samples == null) {
                        result.samples = InvokeVogen(phrase);
                        var source = new WaveSource(0, 0, 0, 1);
                        source.SetSamples(result.samples);
                        WaveFileWriter.CreateWaveFile16(wavPath, new ExportAdapter(source).ToMono(1, 0));
                    }
                    if (result.samples != null) {
                        ApplyDynamics(phrase, result.samples);
                    }
                    progress.Complete(phrase.phones.Length, progressInfo);
                    return result;
                }
            });
            return task;
        }

        float[] InvokeVogen(RenderPhrase phrase) {
            const int pitchInterval = 5;
            int headFrames = (int)(headMs / frameMs);
            int tailFrames = (int)(tailMs / frameMs);
            var result = Layout(phrase);
            var singer = phrase.singer as VogenSinger;
            var notePitches = phrase.notes
                .Select(n => (float)n.tone)
                .Prepend(0)
                .Append(0)
                .ToList();
            var noteDurs = phrase.notes
                .Select(n => (long)Math.Round(n.duration * phrase.tickToMs / frameMs))
                .Prepend(headFrames)
                .Append(tailFrames)
                .ToList();
            var noteToCharIndex = Enumerable.Range(0, noteDurs.Count)
                .Select(i => (long)i)
                .ToList();
            var phonemes = phrase.phones
                .Select(p => p.phoneme)
                .Prepend("")
                .Append("")
                .ToList();
            var phDurs = new List<long>();
            phDurs.Add(headFrames);
            int startTick = phrase.phones.First().position;
            double lastEndMs = 0;
            foreach (var phone in phrase.phones) {
                double endMs = (phone.position + phone.duration - startTick) * phrase.tickToMs;
                phDurs.Add((int)Math.Round((endMs - lastEndMs) / frameMs));
                lastEndMs = endMs;
            }
            phDurs.Add(tailFrames);
            var totalFrames = (int)phDurs.Sum();
            var f0 = DownSampleCurve(phrase.pitches, 0, totalFrames, headFrames, tailFrames, phrase.tickToMs, x => MusicMath.ToneToFreq(x * 0.01));
            float[] f0Shifted = f0.Select(f => (float)f).ToArray();
            if (phrase.toneShift != null) {
                for (int i = 0; i < f0.Length - headFrames - tailFrames; i++) {
                    int index = (int)(i * frameMs / phrase.tickToMs / pitchInterval);
                    if (index < phrase.pitches.Length) {
                        f0Shifted[i + headFrames] = (float)MusicMath.ToneToFreq((phrase.pitches[index] + phrase.toneShift[index]) * 0.01);
                    }
                }
            }
            noteDurs[0] += phDurs.Sum() - noteDurs.Sum();
            var breAmp = new float[f0.Length];
            Array.Fill(breAmp, 0);
            var inputs = new List<NamedOnnxValue>();
            inputs.Add(NamedOnnxValue.CreateFromTensor("notePitches",
                new DenseTensor<float>(notePitches.ToArray(), new int[] { notePitches.Count })));
            inputs.Add(NamedOnnxValue.CreateFromTensor("noteDurs",
                new DenseTensor<long>(noteDurs.ToArray(), new int[] { noteDurs.Count })));
            inputs.Add(NamedOnnxValue.CreateFromTensor("noteToCharIndex",
                new DenseTensor<long>(noteToCharIndex.ToArray(), new int[] { noteToCharIndex.Count })));
            inputs.Add(NamedOnnxValue.CreateFromTensor("phs",
                new DenseTensor<string>(phonemes.ToArray(), new int[] { phonemes.Count })));
            inputs.Add(NamedOnnxValue.CreateFromTensor("phDurs",
                new DenseTensor<long>(phDurs.ToArray(), new int[] { phonemes.Count })));
            using (var session = new InferenceSession(Data.VogenRes.f0_man)) {
                using var outputs = session.Run(inputs);
                var f0Out = outputs.First().AsTensor<float>();
                var f0Path = Path.Join(PathManager.Inst.CachePath, $"vog-{phrase.hash:x16}-f0.npy");
                var f0Array = new float[f0.Length];
                for (int i = 0; i < f0.Length; ++i) {
                    f0Array[i] = f0Out[i];
                    if (f0Out[i] < float.Epsilon) {
                        f0[i] = 0;
                    }
                }
                np.Save(f0Array, f0Path);
            }
            inputs.Clear();
            inputs.Add(NamedOnnxValue.CreateFromTensor("phs",
                new DenseTensor<string>(phonemes.ToArray(), new int[] { 1, phonemes.Count })));
            inputs.Add(NamedOnnxValue.CreateFromTensor("phDurs",
                new DenseTensor<long>(phDurs.ToArray(), new int[] { 1, phonemes.Count })));
            inputs.Add(NamedOnnxValue.CreateFromTensor("f0",
                new DenseTensor<float>(f0Shifted, new int[] { 1, f0.Length })));
            inputs.Add(NamedOnnxValue.CreateFromTensor("breAmp",
                new DenseTensor<float>(breAmp, new int[] { 1, f0.Length })));
            double[,] sp;
            double[,] ap;
            using (var session = new InferenceSession(singer.model)) {
                using var outputs = session.Run(inputs);
                var mgc = outputs.First().AsTensor<float>().Select(f => (double)f).ToArray();
                var bap = outputs.Last().AsTensor<float>().Select(f => (double)f).ToArray();
                sp = Worldline.DecodeMgc(f0.Length, mgc, fftSize, fs);
                ap = Worldline.DecodeBap(f0.Length, bap, fftSize, fs);
            }
            var gender = DownSampleCurve(phrase.gender, 0.5, totalFrames, headFrames, tailFrames, phrase.tickToMs, x => 0.5 + 0.005 * x);
            var tension = DownSampleCurve(phrase.tension, 0.5, totalFrames, headFrames, tailFrames, phrase.tickToMs, x => 0.5 + 0.005 * x);
            var breathiness = DownSampleCurve(phrase.breathiness, 0.5, totalFrames, headFrames, tailFrames, phrase.tickToMs, x => 0.5 + 0.005 * x);
            var voicing = DownSampleCurve(phrase.voicing, 1.0, totalFrames, headFrames, tailFrames, phrase.tickToMs, x => 0.01 * x);
            var samples = Worldline.WorldSynthesis(
                f0,
                sp, false, sp.GetLength(1),
                ap, false, fftSize,
                frameMs, 44100,
                gender, tension, breathiness, voicing);
            return samples.Select(f => (float)f).ToArray();
        }

        double[] DownSampleCurve(float[] curve, double defaultValue, int length, int headFrames, int tailFrames, double tickToMs, Func<double, double> convert) {
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
            int offset = (int)(headMs / 1000 * 44100);
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

        public RenderPitchResult LoadRenderedPitch(RenderPhrase phrase) {
            var f0Path = Path.Join(PathManager.Inst.CachePath, $"vog-{phrase.hash:x16}-f0.npy");
            if (!File.Exists(f0Path)) {
                return null;
            }
            var result = new RenderPitchResult() {
                tones = np.Load<float[]>(f0Path).Select(f => (float)MusicMath.FreqToTone(f)).ToArray(),
            };
            result.ticks = new float[result.tones.Length];
            var layout = Layout(phrase);
            var t = layout.positionMs - layout.leadingMs;
            for (int i = 0; i < result.tones.Length; i++) {
                t += frameMs;
                result.ticks[i] = (float)(t / phrase.tickToMs) - phrase.position;
            }
            return result;
        }

        public override string ToString() => "VOGEN";
    }
}

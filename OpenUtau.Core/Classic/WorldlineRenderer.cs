using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using OpenUtau.Core;
using OpenUtau.Core.Format;
using OpenUtau.Core.Render;
using OpenUtau.Core.SignalChain;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Classic {
    public class WorldlineRenderer : IRenderer {
        const float frameMs = 10;

        static readonly HashSet<string> supportedExp = new HashSet<string>(){
            Ustx.DYN,
            Ustx.PITD,
            Ustx.CLR,
            Ustx.SHFT,
            Ustx.VEL,
            Ustx.VOL,
            Ustx.MOD,
            Ustx.ALT,
            Ustx.GENC,
            Ustx.BREC,
            Ustx.TENC,
            Ustx.VOIC,
        };

        public USingerType SingerType => USingerType.Classic;

        public bool SupportsRenderPitch => false;

        public bool SupportsExpression(UExpressionDescriptor descriptor) {
            return supportedExp.Contains(descriptor.abbr);
        }

        public RenderResult Layout(RenderPhrase phrase) {
            var firstPhone = phrase.phones.First();
            var lastPhone = phrase.phones.Last();
            return new RenderResult() {
                leadingMs = firstPhone.preutterMs,
                positionMs = (phrase.position + firstPhone.position) * phrase.tickToMs,
                estimatedLengthMs = (lastPhone.duration + lastPhone.position - firstPhone.position) * phrase.tickToMs + firstPhone.preutterMs,
            };
        }

        public Task<RenderResult> Render(RenderPhrase phrase, Progress progress, CancellationTokenSource cancellation, bool isPreRender) {
            var resamplerItems = new List<ResamplerItem>();
            foreach (var phone in phrase.phones) {
                resamplerItems.Add(new ResamplerItem(phrase, phone));
            }
            var task = Task.Run(() => {
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
                    using var phraseSynth = new Worldline.PhraseSynth();
                    double posOffsetMs = resamplerItems[0].phone.position * phrase.tickToMs - resamplerItems[0].phone.preutterMs;
                    foreach (var item in resamplerItems) {
                        if (cancellation.IsCancellationRequested) {
                            return result;
                        }
                        double posMs = item.phone.position * item.phrase.tickToMs - item.phone.preutterMs - posOffsetMs;
                        double skipMs = item.skipOver;
                        double lengthMs = item.phone.envelope[4].X - item.phone.envelope[0].X;
                        double fadeInMs = item.phone.envelope[1].X - item.phone.envelope[0].X;
                        double fadeOutMs = item.phone.envelope[4].X - item.phone.envelope[3].X;
                        phraseSynth.AddRequest(item, posMs, skipMs, lengthMs, fadeInMs, fadeOutMs);
                    }
                    int frames = (int)Math.Ceiling(result.estimatedLengthMs / frameMs);
                    var f0 = DownSampleCurve(phrase.pitches, 0, frames, phrase.tickToMs, x => MusicMath.ToneToFreq(x * 0.01));
                    var gender = DownSampleCurve(phrase.gender, 0.5, frames, phrase.tickToMs, x => 0.5 + 0.005 * x);
                    var tension = DownSampleCurve(phrase.tension, 0.5, frames, phrase.tickToMs, x => 0.5 + 0.005 * x);
                    var breathiness = DownSampleCurve(phrase.breathiness, 0.5, frames, phrase.tickToMs, x => 0.5 + 0.005 * x);
                    var voicing = DownSampleCurve(phrase.voicing, 1.0, frames, phrase.tickToMs, x => 0.01 * x);
                    phraseSynth.SetCurves(f0, gender, tension, breathiness, voicing);
                    result.samples = phraseSynth.Synth();
                    var source = new WaveSource(0, 0, 0, 1);
                    source.SetSamples(result.samples);
                    WaveFileWriter.CreateWaveFile16(wavPath, new ExportAdapter(source).ToMono(1, 0));
                }
                progress.Complete(phrase.phones.Length, progressInfo);
                if (result.samples != null) {
                    ApplyDynamics(phrase, result.samples);
                }
                return result;
            });
            return task;
        }

        double[] DownSampleCurve(float[] curve, double defaultValue, int length, double tickToMs, Func<double, double> convert) {
            const int interval = 5;
            var result = new double[length];
            if (curve == null) {
                Array.Fill(result, defaultValue);
                return result;
            }
            for (int i = 0; i < length; i++) {
                int index = (int)(i * frameMs / tickToMs / interval);
                if (index < curve.Length) {
                    result[i] = convert(curve[index]);
                }
            }
            return result;
        }

        void ApplyDynamics(RenderPhrase phrase, float[] samples) {
            const int interval = 5;
            if (phrase.dynamics == null) {
                return;
            }
            int pos = 0;
            for (int i = 0; i < phrase.dynamics.Length; ++i) {
                int endPos = (int)((i + 1) * interval * phrase.tickToMs / 1000 * 44100);
                endPos = Math.Min(endPos, samples.Length);
                float a = phrase.dynamics[i];
                float b = (i + 1) == phrase.dynamics.Length ? phrase.dynamics[i] : phrase.dynamics[i + 1];
                for (int j = pos; j < endPos; ++j) {
                    samples[j] *= a + (b - a) * (j - pos) / (endPos - pos);
                }
                pos = endPos;
            }
        }

        public RenderPitchResult LoadRenderedPitch(RenderPhrase phrase) {
            return null;
        }

        public override string ToString() => "WORLDLINER";
    }
}

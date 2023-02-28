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
            return new RenderResult() {
                leadingMs = phrase.leadingMs,
                positionMs = phrase.positionMs,
                estimatedLengthMs = phrase.durationMs + phrase.leadingMs,
            };
        }

        public Task<RenderResult> Render(RenderPhrase phrase, Progress progress, CancellationTokenSource cancellation, bool isPreRender) {
            var resamplerItems = new List<ResamplerItem>();
            foreach (var phone in phrase.phones) {
                resamplerItems.Add(new ResamplerItem(phrase, phone));
            }
            var task = Task.Run(() => {
                var result = Layout(phrase);
                var wavPath = Path.Join(PathManager.Inst.CachePath, $"wdl-{phrase.hash:x16}.wav");
                string progressInfo = $"{this} {string.Join(" ", phrase.phones.Select(p => p.phoneme))}";
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
                    double posOffsetMs = phrase.positionMs - phrase.leadingMs;
                    foreach (var item in resamplerItems) {
                        if (cancellation.IsCancellationRequested) {
                            return result;
                        }
                        double posMs = item.phone.positionMs - item.phone.leadingMs - (phrase.positionMs - phrase.leadingMs);
                        double skipMs = item.skipOver;
                        double lengthMs = item.phone.envelope[4].X - item.phone.envelope[0].X;
                        double fadeInMs = item.phone.envelope[1].X - item.phone.envelope[0].X;
                        double fadeOutMs = item.phone.envelope[4].X - item.phone.envelope[3].X;
                        phraseSynth.AddRequest(item, posMs, skipMs, lengthMs, fadeInMs, fadeOutMs);
                    }
                    int frames = (int)Math.Ceiling(result.estimatedLengthMs / frameMs);
                    var f0 = SampleCurve(phrase, phrase.pitches, 0, frames, x => MusicMath.ToneToFreq(x * 0.01));
                    var gender = SampleCurve(phrase, phrase.gender, 0.5, frames, x => 0.5 + 0.005 * x);
                    var tension = SampleCurve(phrase, phrase.tension, 0.5, frames, x => 0.5 + 0.005 * x);
                    var breathiness = SampleCurve(phrase, phrase.breathiness, 0.5, frames, x => 0.5 + 0.005 * x);
                    var voicing = SampleCurve(phrase, phrase.voicing, 1.0, frames, x => 0.01 * x);
                    phraseSynth.SetCurves(f0, gender, tension, breathiness, voicing);
                    result.samples = phraseSynth.Synth();
                    var source = new WaveSource(0, 0, 0, 1);
                    source.SetSamples(result.samples);
                    WaveFileWriter.CreateWaveFile16(wavPath, new ExportAdapter(source).ToMono(1, 0));
                }
                progress.Complete(phrase.phones.Length, progressInfo);
                if (result.samples != null) {
                    Renderers.ApplyDynamics(phrase, result);
                }
                return result;
            });
            return task;
        }

        double[] SampleCurve(RenderPhrase phrase, float[] curve, double defaultValue, int length, Func<double, double> convert) {
            const int interval = 5;
            var result = new double[length];
            if (curve == null) {
                Array.Fill(result, defaultValue);
                return result;
            }
            for (int i = 0; i < length; i++) {
                double posMs = phrase.positionMs - phrase.leadingMs + i * frameMs;
                int ticks = phrase.timeAxis.MsPosToTickPos(posMs) - (phrase.position - phrase.leading);
                int index = Math.Max(0, (int)((double)ticks / interval));
                if (index < curve.Length) {
                    result[i] = convert(curve[index]);
                }
            }
            return result;
        }

        public RenderPitchResult LoadRenderedPitch(RenderPhrase phrase) {
            return null;
        }

        public UExpressionDescriptor[] GetSuggestedExpressions(USinger singer, URenderSettings renderSettings) {
            return new UExpressionDescriptor[] { };
        }

        public override string ToString() => Renderers.WORLDLINER;
    }
}

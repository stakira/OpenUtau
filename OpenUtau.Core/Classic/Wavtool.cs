using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using NAudio.Wave;
using OpenUtau.Core.Formats;
using OpenUtau.Core.SignalChain;

namespace OpenUtau.Classic {
    interface IWavtool {
        // <output file> <input file> <STP> <note length>
        // [<p1> <p2> <p3> <v1> <v2> <v3> [<v4> <overlap> <p4> [<p5> <v5>]]]
        float[] Concatenate(List<ResamplerItem> resamplerItems, CancellationTokenSource cancellation);
    }

    class SharpWavtool : IWavtool {
        public float[] Concatenate(List<ResamplerItem> resamplerItems, CancellationTokenSource cancellation) {
            if (cancellation.IsCancellationRequested) {
                return null;
            }
            var phrase = resamplerItems[0].phrase;
            double posOffset = resamplerItems[0].phone.position * phrase.tickToMs - resamplerItems[0].phone.preutterMs;
            var mix = new WaveMix(resamplerItems.Select(item => {
                double posMs = item.phone.position * item.phrase.tickToMs - item.phone.preutterMs - posOffset;
                var source = new WaveSource(posMs, item.requiredLength, item.skipOver, 1);
                if (File.Exists(item.outputFile)) {
                    using (var waveStream = Wave.OpenFile(item.outputFile)) {
                        var samples = Wave.GetSamples(waveStream.ToSampleProvider().ToMono(1, 0));
                        int skipSamples = (int)(item.skipOver * 44100 / 1000);
                        var envelope = EnvelopeMsToSamples(item.phone.envelope, skipSamples);
                        ApplyEnvelope(samples, envelope);
                        source.SetSamples(samples);
                    }
                } else {
                    source.SetSamples(new float[0]);
                }
                return source;
            }));
            var export = new ExportAdapter(mix).ToMono(1, 0);
            var samples = new List<float>();
            var buffer = new float[44100];
            int n;
            while ((n = export.Read(buffer, 0, buffer.Length)) > 0) {
                samples.AddRange(buffer.Take(n));
            }
            return samples.ToArray();
        }

        private static void ApplyEnvelope(float[] data, IList<Vector2> envelope) {
            int nextPoint = 0;
            for (int i = 0; i < data.Length; ++i) {
                while (nextPoint < envelope.Count && i > envelope[nextPoint].X) {
                    nextPoint++;
                }
                float gain;
                if (nextPoint == 0) {
                    gain = envelope.First().Y;
                } else if (nextPoint >= envelope.Count) {
                    gain = envelope.Last().Y;
                } else {
                    var p0 = envelope[nextPoint - 1];
                    var p1 = envelope[nextPoint];
                    if (p0.X >= p1.X) {
                        gain = p0.Y;
                    } else {
                        gain = p0.Y + (p1.Y - p0.Y) * (i - p0.X) / (p1.X - p0.X);
                    }
                }
                data[i] *= gain;
            }
        }

        private static IList<Vector2> EnvelopeMsToSamples(IList<Vector2> envelope, int skipOverSamples) {
            envelope = new List<Vector2>(envelope);
            double shift = -envelope[0].X;
            for (var i = 0; i < envelope.Count; i++) {
                var point = envelope[i];
                point.X = (float)((point.X + shift) * 44100 / 1000) + skipOverSamples;
                point.Y /= 100;
                envelope[i] = point;
            }
            return envelope;
        }
    }
}

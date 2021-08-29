using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using NAudio.Wave;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Render {
    class EnvelopeSampleProvider : ISampleProvider {
        private readonly object lockObject = new object();
        private readonly ISampleProvider source;
        private readonly List<Vector2> envelope = new List<Vector2>();
        private int samplePosition = 0;

        public EnvelopeSampleProvider(ISampleProvider source, List<Vector2> envelope, double skipOver) {
            this.source = source;
            this.envelope.AddRange(envelope);
            int skipOverSamples = (int)(skipOver * WaveFormat.SampleRate / 1000);
            ConvertEnvelope(skipOverSamples);
        }

        public int Read(float[] buffer, int offset, int count) {
            int sourceSamplesRead = source.Read(buffer, offset, count);
            lock (lockObject) {
                ApplyEnvelope(buffer, offset, sourceSamplesRead);
            }
            return sourceSamplesRead;
        }

        private void ApplyEnvelope(float[] buffer, int offset, int sourceSamplesRead) {
            int sample = 0;
            while (sample < sourceSamplesRead) {
                // TODO: optimization, skip between unit volume points
                float multiplier = GetGain();
                for (int ch = 0; ch < WaveFormat.Channels; ch++) {
                    buffer[offset + sample++] *= multiplier;
                }
                samplePosition++;
            }
        }

        public WaveFormat WaveFormat {
            get { return source.WaveFormat; }
        }

        private int x0, x1;
        private float y0, y1;
        private int nextPoint = 0;

        private float GetGain() {
            while (nextPoint < envelope.Count() && samplePosition >= envelope[nextPoint].X) {
                nextPoint++;
                if (nextPoint > 0 && nextPoint < envelope.Count()) {
                    x0 = (int)envelope[nextPoint - 1].X;
                    x1 = (int)envelope[nextPoint].X;
                    y0 = envelope[nextPoint - 1].Y;
                    y1 = envelope[nextPoint].Y;
                }
            }
            if (nextPoint == 0) return envelope[0].Y;
            else if (nextPoint == envelope.Count()) return envelope.Last().Y;
            else return y0 + (y1 - y0) * (samplePosition - x0) / (x1 - x0);
        }

        private void ConvertEnvelope(int skipOverSamples) {
            double shift = -envelope[0].X;
            for (var i = 0; i < envelope.Count; i++) {
                var point = envelope[i];
                point.X = (int)((point.X + shift) * WaveFormat.SampleRate / 1000) + skipOverSamples;
                point.Y /= 100;
                envelope[i] = point;
            }
        }
    }
}

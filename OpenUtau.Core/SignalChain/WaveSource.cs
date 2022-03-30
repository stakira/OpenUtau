using System;

namespace OpenUtau.Core.SignalChain {
    public class WaveSource : ISignalSource {
        public readonly int offset;
        public readonly int estimatedLength;
        public readonly int channels;

        private readonly object lockObj = new object();
        private float[] data;

        public WaveSource(double offsetMs, double estimatedLengthMs, double skipOverMs, int channels) {
            this.channels = channels;
            offset = (int)((offsetMs - skipOverMs) * 44100 / 1000) * channels;
            estimatedLength = (int)(estimatedLengthMs * 44100 / 1000) * channels;
        }

        public void SetSamples(float[] samples) {
            lock (lockObj) {
                data = samples;
            }
        }

        public bool IsReady(int position, int count) {
            int copies = 2 / channels;
            return position + count <= offset * copies
                || offset * copies + estimatedLength * copies <= position
                || data != null;
        }

        public int Mix(int position, float[] buffer, int index, int count) {
            int copies = 2 / channels;
            if (data == null) {
                if (position + count <= offset * copies) {
                    return position + count;
                }
                return position;
            }
            int start = Math.Max(position, offset * copies);
            int end = Math.Min(position + count, offset * copies + data.Length * copies);
            for (int i = start; i < end; ++i) {
                buffer[index + i - position] += data[i / copies - offset];
            }
            return end;
        }
    }
}

using System;
using NAudio.Wave;

namespace OpenUtau.Core.Metronome {
    class SampleSourceProvider : ISampleProvider
    {
        private readonly SampleSource sampleSource;
        private long Position { get; set; }

        public SampleSourceProvider(SampleSource samples)
        {
            this.sampleSource = samples;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                int bytesRead = ReadSample(buffer, offset + totalBytesRead, count - totalBytesRead);
                if (bytesRead == 0)
                {
                    Position = 0;
                }
                totalBytesRead += bytesRead;
            }
            return totalBytesRead;
        }

        private int ReadSample(float[] buffer, int offset, int count)
        {
            var availableSamples = sampleSource.Length - Position;
            var samplesToCopy = Math.Min(availableSamples, count);
            Array.Copy(sampleSource.AudioData, Position, buffer, offset, samplesToCopy);
            Position += samplesToCopy;
            return (int)samplesToCopy;
        }

        public WaveFormat WaveFormat { get { return sampleSource.WaveFormat; } }
    }
}

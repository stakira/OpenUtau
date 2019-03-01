using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Serilog;

namespace OpenUtau.Core.Render {

    internal class MemorySampleProvider : ISampleProvider {
        private float[] data;
        private int position;
        public WaveFormat WaveFormat { private set; get; }

        public static MemorySampleProvider FromStream(Stream stream) {
            using (var waveProvider = new WaveFileReader(stream)) {
                ISampleProvider sampleProvider = null;
                switch (waveProvider.WaveFormat.BitsPerSample) {
                    case 8:
                        sampleProvider = new Pcm8BitToSampleProvider(waveProvider);
                        break;
                    case 16:
                        sampleProvider = new Pcm16BitToSampleProvider(waveProvider);
                        break;
                    case 24:
                        sampleProvider = new Pcm24BitToSampleProvider(waveProvider);
                        break;
                    case 32:
                        sampleProvider = new Pcm32BitToSampleProvider(waveProvider);
                        break;
                    default:
                        Log.Error($"Unknown PCM bits per sample {waveProvider.WaveFormat.BitsPerSample}");
                        return null;
                }

                var format = sampleProvider.WaveFormat;
                var samples = new List<float>();
                var buffer = new float[format.SampleRate];
                var n = 0;
                while ((n = sampleProvider.Read(buffer, 0, buffer.Length)) > 0) {
                    samples.AddRange(buffer.Take(n));
                }
                var data = samples.ToArray();
                return new MemorySampleProvider() {
                    WaveFormat = format,
                    data = data,
                };
            }
        }

        public int Read(float[] buffer, int offset, int count) {
            var left = data.Length - position;
            count = Math.Min(left, count);
            Array.Copy(data, position, buffer, offset, count);
            position += count;
            return count;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using OpenUtau.Core;

namespace OpenUtau.Audio {
    class NAudioFileUtils : IAudioFileUtils {
        public void GetAudioFileInfo(string file, out WaveFormat waveFormat, out TimeSpan duration) {
            var reader = new AudioFileReader(file);
            waveFormat = reader.WaveFormat;
            duration = reader.TotalTime;
        }

        public WaveStream OpenAudioFileAsWaveStream(string file) {
            return new AudioFileReader(file);
        }

        public float[] GetAudioSamples(string file) {
            using (var reader = new AudioFileReader(file)) {
                var provider = new MediaFoundationResampler(reader, WaveFormat.CreateIeeeFloatWaveFormat(44100, 1)).ToSampleProvider();
                List<float> samples = new List<float>();
                float[] buffer = new float[64 * 1024];
                int n;
                while ((n = provider.Read(buffer, 0, buffer.Length)) > 0) {
                    samples.AddRange(buffer.Take(n));
                }
                return samples.ToArray();
            }
        }
    }
}

using System;
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

        public ISampleProvider OpenAudioFileAsSampleProvider(string file) {
            return new AudioFileReader(file);
        }
    }
}

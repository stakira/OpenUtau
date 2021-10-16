using System;

namespace OpenUtau.Audio {
    public readonly struct AudioStreamInfo {
        public AudioStreamInfo(int channels, int sampleRate, TimeSpan duration) {
            Channels = channels;
            SampleRate = sampleRate;
            Duration = duration;
        }
        public int Channels { get; }
        public int SampleRate { get; }
        public TimeSpan Duration { get; }
    }
}

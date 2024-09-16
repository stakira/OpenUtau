using System;
using NAudio.Wave;


namespace OpenUtau.Core.Metronome {
    class SampleSource
    {
        public float[] AudioData { get; private set; }       // Audio samples
        public WaveFormat WaveFormat { get; private set; }   // Information about format of sample rate, channes, etc
        public long Length { get; private set; }             // Length of stream in bytes
        public double Duration { get; private set; }         // Length of stream in seconds

        public SampleSource(string audioFileName)
        {
            AudioFileReader reader = new AudioFileReader(audioFileName);
            using (reader)
            {
                WaveFormat = reader.WaveFormat;
                Length = reader.Length; 
                Duration = (double)Length / (WaveFormat.SampleRate * WaveFormat.Channels * (WaveFormat.BitsPerSample / 8)); 
                AudioData = new float[Length];
                reader.Read(AudioData, 0, (int)Length);
            }
        }

        public SampleSource(float[] audioData, WaveFormat waveFormat)
        {
            WaveFormat = waveFormat;
            Length = audioData.Length;
            Duration = (double)Length / (WaveFormat.SampleRate * WaveFormat.Channels * (WaveFormat.BitsPerSample / 8));
            AudioData = audioData;
        }
    }
}

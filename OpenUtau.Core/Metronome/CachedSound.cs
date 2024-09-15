using System;
using System.Collections.Generic;
using NAudio.Wave;
using System.Linq;

namespace OpenUtau.Core.Metronome
{
    public class CachedSound : ISampleProvider
    {
        public float[] Data { get; private set; }
        public WaveFormat WaveFormat { get; private set; }
        public int Position { get; set; }

        private CachedSound(float[] data, WaveFormat waveFormat) 
        {
            Data = data;
            WaveFormat = waveFormat;
        }

        public static CachedSound FromSampleProvider(ISampleProvider sampleProvider)
        {
            var readBuffer = new float[sampleProvider.WaveFormat.SampleRate * sampleProvider.WaveFormat.Channels];
            var data = new List<float>(readBuffer.Length / 4);
            
            int samplesRead;

            while ((samplesRead = sampleProvider.Read(readBuffer, 0, readBuffer.Length)) > 0)
            {
                data.AddRange(readBuffer.Take(samplesRead));
            }

            return new CachedSound(data.ToArray(), sampleProvider.WaveFormat);
        }
        /*
        public static CachedSound FromFileName(string fileName)
        {
            using var audioFileReader = new AudioFileReader(fileName);

            var file = new List<float>((int)(audioFileReader.Length / 4));
            var readBuffer = new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];

            int samplesRead;

            while ((samplesRead = audioFileReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
            {
            file.AddRange(readBuffer.Take(samplesRead));
            }

            return new CachedSound(file.ToArray(), audioFileReader.WaveFormat);
        }
        */
        public int Read(float[] buffer, int offset, int count)
        {
            var availableSamples = Data.Length - Position;
            var samplesToCopy = Math.Min(availableSamples, count);

            Array.Copy(Data, Position, buffer, offset, samplesToCopy);

            Position += samplesToCopy;

            return samplesToCopy;
        }
    }
}

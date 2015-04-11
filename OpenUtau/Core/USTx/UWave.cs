using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;

namespace OpenUtau.Core.USTx
{
    class UWave : UPart
    {
        private const long MaxPeakCount = 32 * 1024;

        public WaveStream Stream;
        public WaveFormat Format;
        public int Channels;
        public int VisualChannels { get { return Math.Min(Channels, 2); } }

        int DownSampleRatio = 64;
        public List<double> LeftPeaks = new List<double>();
        public List<double> RightPeaks = new List<double>();

        public string FilePath;

        public UWave() { }

        public void Resample(WaveStream stream)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch(); sw.Start();
            int outRate = 2000;
            int outDepth = 8;
            var outFormat = new WaveFormat(outRate, outDepth, stream.WaveFormat.Channels);
            using (var resampler = new MediaFoundationResampler(stream, outFormat))
            {
                resampler.ResamplerQuality = 1;
                WaveFileWriter.CreateWaveFile(@"E:\Vsqx\test.wav", resampler);
            }
            sw.Stop(); System.Diagnostics.Debug.WriteLine(sw.Elapsed.TotalMilliseconds.ToString());
        }

        public void BuildPeaks(WaveStream stream)
        {
            //Resample(stream);

            LeftPeaks.Clear();
            RightPeaks.Clear();

            Stream = stream;
            Format = stream.WaveFormat;

            Channels = Format.Channels;
            int byteDepth = Format.BitsPerSample / 8;

            long lengthInSample = Stream.Length / byteDepth / Channels;
            while (lengthInSample > MaxPeakCount * DownSampleRatio) DownSampleRatio *= 2;

            int reader = 0;
            int bufferSize = byteDepth * Channels;
            byte[] buffer = new byte[bufferSize];
            int samples = 0;
            long sumLeft = 0;
            long sumRight = 0;

            double normalization = Math.Pow(256, byteDepth - 1);

            while ((reader = stream.Read(buffer, 0, bufferSize)) != 0)
            {
                for (int i = 0; i < Channels; i++)
                {
                    int data;
                    if (byteDepth == 1) data = buffer[i];
                    else if (byteDepth == 2) data = BitConverter.ToInt16(buffer, i * 2);
                    else if (byteDepth == 4) data = BitConverter.ToInt32(buffer, i * 4);
                    else data = BitConverter.ToInt32(buffer, i * 3);

                    if (i == 0) sumLeft += data * data;
                    else if (i == 1) sumRight += data * data;
                }
                    samples ++;

                if (samples == DownSampleRatio || stream.Position == stream.Length)
                {
                    LeftPeaks.Add(Math.Sqrt(sumLeft / samples) / normalization);
                    //LeftPeaks.Add(Math.Log(Math.Max(1, Math.Sqrt(sumLeft / samples) / normalization)) * 10);
                    RightPeaks.Add(Math.Sqrt(sumRight / samples) / normalization);
                    //RightPeaks.Add(Math.Log(Math.Max(1, Math.Sqrt(sumRight / samples) / normalization)) * 10);

                    samples = 0;
                    sumLeft = 0;
                    sumRight = 0;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using NAudio;
using NAudio.Wave;

using OpenUtau.Core.USTx;

namespace OpenUtau.Core.Formats
{
    static class Sound
    {
        public static UWavePart CreateUWavePart(string filepath)
        {
            UWavePart uwavepart = new UWavePart();
            uwavepart.FilePath = filepath;
            uwavepart.Name = Path.GetFileName(filepath);
            uwavepart.PosTick = 0;
            LoadUWavePart(uwavepart);
            return uwavepart;
        }

        public static void LoadUWavePart(UWavePart uwavepart)
        {
            WaveStream stream;
            try
            {
                stream = new AudioFileReader(uwavepart.FilePath);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.GetType().ToString() + "\n" + e.Message);
                return;
            }

            uwavepart.PeaksPath = uwavepart.FilePath + ".oupeaks";
            uwavepart.Stream = stream;
            GetPeaks(uwavepart);
        }

        private static void GetPeaks(UWavePart uwavepart)
        {
            if (!File.Exists(uwavepart.FilePath)) return;

            WaveStream stream;
            try { stream = new WaveFileReader(uwavepart.PeaksPath); }
            catch { stream = null; }

            if (stream != null)
            {
                if (stream.TotalTime.TotalSeconds - uwavepart.Stream.TotalTime.TotalSeconds > 0.1)
                {
                    stream.Dispose();
                    stream = null;
                }
                else
                {
                    uwavepart.Peaks = stream;
                    return;
                }
            }

            BuildPeaks(uwavepart);
        }

        private static void BuildPeaks(UWavePart uwavepart)
        {
            if (File.Exists(uwavepart.PeaksPath)) File.Delete(uwavepart.PeaksPath);

            WaveFormat outFormat = new WaveFormat(2000, 8, 1/*Math.Min(2, uwavepart.Stream.WaveFormat.Channels)*/);
            using (var resampler = new MediaFoundationResampler(uwavepart.Stream, outFormat))
            {
                WaveFileWriter.CreateWaveFile(uwavepart.PeaksPath, resampler);
            }

            uwavepart.Peaks = new WaveFileReader(uwavepart.PeaksPath);
        }

        public static byte[] StreamToArray(WaveStream stream)
        {
            byte[] buffer = new byte[4096];
            int reader = 0;
            System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
            while ((reader = stream.Read(buffer, 0, buffer.Length)) != 0)
                memoryStream.Write(buffer, 0, reader);
            return memoryStream.ToArray();
        }
    }
}

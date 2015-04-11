using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio;
using NAudio.Wave;

using OpenUtau.Core.USTx;

namespace OpenUtau.Core.Formats
{
    static class Sound
    {
        public static UWave Load(string file)
        {
            UWave uwave = new UWave();
            WaveStream stream;

            try
            {
                stream = new WaveFileReader(file);
            }
            catch (Exception e)
            {
                return null;
            }

            if (stream.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
            {
                stream = WaveFormatConversionStream.CreatePcmStream(stream);
                stream = new BlockAlignReductionStream(stream);
            }
            uwave.Name = System.IO.Path.GetFileName(file);
            uwave.FilePath = file;
            uwave.BuildPeaks(stream);

            return uwave;
        }

    }
}

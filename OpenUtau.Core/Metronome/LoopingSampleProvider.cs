using NAudio.Wave;

namespace OpenUtau.Core.Metronome
{
    public class LoopingSampleProvider : ISampleProvider
    {
        public WaveFormat WaveFormat => source.WaveFormat;

        private CachedSound source;

        public LoopingSampleProvider(CachedSound source)
        {
            this.source = source;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                int bytesRead = source.Read(buffer, offset + totalBytesRead, count - totalBytesRead);

                if (bytesRead == 0)
                    source.Position = 0;

                totalBytesRead += bytesRead;
            }
            return totalBytesRead;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenUtau.Core.USTx;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace OpenUtau.Core.Render
{
    class EnvelopeSampleProvider : ISampleProvider
    {
        private readonly object lockObject = new object();
        private readonly OffsetSampleProvider sourceSkipped;
        private readonly List<ExpPoint> envelope = new List<ExpPoint>();
        private int samplePosition = 0;

        public EnvelopeSampleProvider(ISampleProvider source, List<ExpPoint> envelope, double skipOver)
        {
            foreach (var pt in envelope) this.envelope.Add(pt.Clone());
            sourceSkipped = new OffsetSampleProvider(source);
            sourceSkipped.SkipOverSamples = (int)(skipOver * source.WaveFormat.SampleRate / 1000);
            ConvertEnvelope();
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int sourceSamplesRead = sourceSkipped.Read(buffer, offset, count);
            lock (lockObject)
            {
                ApplyEnvelope(buffer, offset, sourceSamplesRead);
            }
            return sourceSamplesRead;
        }

        private void ApplyEnvelope(float[] buffer, int offset, int sourceSamplesRead)
        {
            int sample = 0;
            while (sample < sourceSamplesRead)
            {
                // TODO: optimization, skip between unit volume points
                float multiplier = GetGain();
                for (int ch = 0; ch < sourceSkipped.WaveFormat.Channels; ch++)
                {
                    buffer[offset + sample++] *= multiplier;
                }
                samplePosition++;
            }
        }

        public WaveFormat WaveFormat
        {
            get { return sourceSkipped.WaveFormat; }
        }

        private int x0, x1;
        private float y0, y1;
        private int nextPoint = 0;

        private float GetGain()
        {
            while (nextPoint < envelope.Count() && samplePosition >= envelope[nextPoint].X)
            {
                nextPoint++;
                if (nextPoint > 0 && nextPoint < envelope.Count())
                {
                    x0 = (int)envelope[nextPoint - 1].X;
                    x1 = (int)envelope[nextPoint].X;
                    y0 = (float)envelope[nextPoint - 1].Y;
                    y1 = (float)envelope[nextPoint].Y;
                }
            }
            if (nextPoint == 0) return (float)envelope[0].Y;
            else if (nextPoint == envelope.Count()) return (float)envelope.Last().Y;
            else return y0 + (y1 - y0) * (samplePosition - x0) / (x1 - x0);
        }

        private void ConvertEnvelope()
        {
            double shift = -envelope[0].X;
            foreach (var point in envelope)
            {
                point.X = (int)((point.X + shift) * sourceSkipped.WaveFormat.SampleRate / 1000);
                point.Y /= 100;
            }
        }
    }

    class WaveConnector
    {
        public List<RenderItem> RenderItems = new List<RenderItem>();

        public WaveConnector() { }

        public void WriteToFile(string file)
        {
            WaveFileWriter.CreateWaveFile16(file, GetMixingSampleProvider());
        }

        public MixingSampleProvider GetMixingSampleProvider()
        {
            List<ISampleProvider> segmentProviders = new List<ISampleProvider>();

            foreach (var item in RenderItems)
            {
                var segmentSource = new CachedSoundSampleProvider(item.Sound);
                segmentProviders.Add(new OffsetSampleProvider(new EnvelopeSampleProvider(segmentSource, item.Envelope, item.SkipOver))
                {
                    DelayBySamples = (int)(item.PosMs * segmentSource.WaveFormat.SampleRate / 1000),
                    TakeSamples = (int)(item.DurMs * segmentSource.WaveFormat.SampleRate / 1000)
                });
            }

            return new MixingSampleProvider(segmentProviders);
        }
    }
}

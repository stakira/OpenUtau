using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace OpenUtau.Core.Render
{
    class RenderItemSampleProvider : ISampleProvider
    {
        private int firstSample;
        private int lastSample;
        private ISampleProvider signalChain;

        public RenderItemSampleProvider(RenderItem renderItem)
        {
            this.RenderItem = renderItem;
            var cachedSampleProvider = new CachedSoundSampleProvider(RenderItem.Sound);
            var offsetSampleProvider = new OffsetSampleProvider(new EnvelopeSampleProvider(cachedSampleProvider, RenderItem.Envelope, RenderItem.SkipOver))
            {
                DelayBySamples = (int)(RenderItem.PosMs * cachedSampleProvider.WaveFormat.SampleRate / 1000),
                TakeSamples = (int)(RenderItem.DurMs * cachedSampleProvider.WaveFormat.SampleRate / 1000),
                SkipOverSamples = (int)(RenderItem.SkipOver * cachedSampleProvider.WaveFormat.SampleRate / 1000)
            };
            this.signalChain = offsetSampleProvider;
            this.firstSample = offsetSampleProvider.DelayBySamples + offsetSampleProvider.SkipOverSamples;
            this.lastSample = this.firstSample + offsetSampleProvider.TakeSamples;
        }

        /// <summary>
        /// Position of first sample
        /// </summary>
        public int FirstSample { get { return firstSample; } }

        /// <summary>
        /// Position of last sample (not included)
        /// </summary>
        public int LastSample { get { return lastSample; } }

        public RenderItem RenderItem { set; get; }

        public int Read(float[] buffer, int offset, int count)
        {
            return signalChain.Read(buffer, offset, count);
        }

        public WaveFormat WaveFormat
        {
            get { return signalChain.WaveFormat; }
        }
    }
}

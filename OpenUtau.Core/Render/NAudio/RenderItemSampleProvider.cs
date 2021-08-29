using System;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace OpenUtau.Core.Render {

    internal class RenderItemSampleProvider : ISampleProvider {
        private readonly ISampleProvider signalChain;

        public RenderItemSampleProvider(RenderItem renderItem, TimeSpan skip) {
            RenderItem = renderItem;
            var cachedSampleProvider = MemorySampleProvider.FromStream(new MemoryStream(RenderItem.Data));
            int delayBy = (int)((RenderItem.PosMs - skip.TotalMilliseconds) * cachedSampleProvider.WaveFormat.SampleRate / 1000);
            var offsetSampleProvider = new OffsetSampleProvider(new EnvelopeSampleProvider(cachedSampleProvider, RenderItem.Envelope, RenderItem.SkipOver)) {
                DelayBySamples = delayBy > 0 ? delayBy : 0,
                TakeSamples = (int)(RenderItem.DurMs * cachedSampleProvider.WaveFormat.SampleRate / 1000),
                SkipOverSamples = (int)(RenderItem.SkipOver * cachedSampleProvider.WaveFormat.SampleRate / 1000) + (delayBy < 0 ? -delayBy : 0),
            };
            signalChain = offsetSampleProvider;
            FirstSample = offsetSampleProvider.DelayBySamples + offsetSampleProvider.SkipOverSamples;
            LastSample = FirstSample + offsetSampleProvider.TakeSamples;
        }

        public int FirstSample { get; }
        public int LastSample { get; }
        public RenderItem RenderItem { set; get; }
        public WaveFormat WaveFormat => signalChain.WaveFormat;

        public int Read(float[] buffer, int offset, int count) {
            return signalChain.Read(buffer, offset, count);
        }
    }
}

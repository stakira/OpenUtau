using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace OpenUtau.Core.Render {

    internal class RenderItemSampleProvider : ISampleProvider {
        private readonly ISampleProvider signalChain;

        public RenderItemSampleProvider(RenderItem renderItem) {
            RenderItem = renderItem;
            var cachedSampleProvider = MemorySampleProvider.FromStream(new MemoryStream(RenderItem.Data));
            var offsetSampleProvider = new OffsetSampleProvider(new EnvelopeSampleProvider(cachedSampleProvider, RenderItem.Envelope, RenderItem.SkipOver)) {
                DelayBySamples = (int)(RenderItem.PosMs * cachedSampleProvider.WaveFormat.SampleRate / 1000),
                TakeSamples = (int)(RenderItem.DurMs * cachedSampleProvider.WaveFormat.SampleRate / 1000),
                SkipOverSamples = (int)(RenderItem.SkipOver * cachedSampleProvider.WaveFormat.SampleRate / 1000)
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

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
    class RenderDispatcher
    {
        public List<RenderItem> RenderItems = new List<RenderItem>();

        public RenderDispatcher() { }

        public void WriteToFile(string file)
        {
            WaveFileWriter.CreateWaveFile16(file, GetMixingSampleProvider());
        }

        public SequencingSampleProvider GetMixingSampleProvider()
        {
            List<RenderItemSampleProvider> segmentProviders = new List<RenderItemSampleProvider>();
            foreach (var item in RenderItems) segmentProviders.Add(new RenderItemSampleProvider(item));
            return new SequencingSampleProvider(segmentProviders);
        }
    }
}

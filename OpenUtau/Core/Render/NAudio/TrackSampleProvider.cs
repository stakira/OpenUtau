using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace OpenUtau.Core.Render
{
    class TrackSampleProvider : ISampleProvider
    {
        private PanningSampleProvider pan;
        private VolumeSampleProvider volume;
        private MixingSampleProvider mix;

        /// <summary>
        /// Pan. -1f (left) to 1f (right).
        /// </summary>
        public float Pan { set { pan.Pan = value; } get { return pan.Pan; } }

        /// <summary>
        /// Volume. 0f to 1f.
        /// </summary>
        public float Volume { set { volume.Volume = value; } get { return volume.Volume; } }

        public TrackSampleProvider()
        {
            mix = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
            //pan = new PanningSampleProvider(mix);
            volume = new VolumeSampleProvider(mix);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            return volume.Read(buffer, offset, count);
        }

        public WaveFormat WaveFormat
        {
            get { return volume.WaveFormat; }
        }

        public void AddSource(ISampleProvider source, TimeSpan delayBy)
        {
            ISampleProvider _source;
            if (source == null || source.WaveFormat == null) {
                return;
            }
            if (source.WaveFormat.Channels == 1) _source = new MonoToStereoSampleProvider(source);
            else if (source.WaveFormat.Channels == 2) _source = source;
            else return;
            mix.AddMixerInput(new OffsetSampleProvider(_source) { DelayBy = delayBy });
        }
    }
}

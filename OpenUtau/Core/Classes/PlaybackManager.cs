using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OpenUtau.Core.Render;

namespace OpenUtau.Core
{
    class PlaybackManager : ICmdSubscriber
    {
        private WaveOut outDevice;
        private MasterBusSampleProvider masterBus;

        private PlaybackManager() {
            this.Subscribe(DocManager.Inst);
            masterBus = (new MasterBusSampleProvider());
        }

        private static PlaybackManager _s;
        public static PlaybackManager Inst { get { if (_s == null) { _s = new PlaybackManager(); } return _s; } }

        MixingSampleProvider mix;
        public void Play(SequencingSampleProvider source)
        {
            if (outDevice != null && outDevice.PlaybackState == PlaybackState.Playing) return;
            if (outDevice != null) outDevice.Dispose();
            var stereo = new MonoToStereoSampleProvider(source);
            mix = new MixingSampleProvider(stereo.WaveFormat);
            mix.AddMixerInput(stereo);
            /*foreach (var part in DocManager.Inst.Project.Parts)
            {
                if (part is OpenUtau.Core.USTx.UWavePart)
                {
                    var _part = part as OpenUtau.Core.USTx.UWavePart;
                    _part.Stream.Position = 0;
                    var _offset = new OffsetSampleProvider(new WaveToSampleProvider(_part.Stream));
                    _offset.DelayBy = TimeSpan.FromMilliseconds(DocManager.Inst.Project.TickToMillisecond(_part.PosTick));
                    mix.AddMixerInput(_offset);
                }
            }*/
            outDevice = new WaveOut();
            outDevice.Init(mix);
            outDevice.Play();
        }

        public void UpdatePlayPos()
        {
            if (outDevice != null && outDevice.PlaybackState == PlaybackState.Playing)
            {
                double ms = outDevice.GetPosition() * 1000.0 / mix.WaveFormat.BitsPerSample /mix.WaveFormat.Channels * 8 / mix.WaveFormat.SampleRate;
                int tick = DocManager.Inst.Project.MillisecondToTick(ms);
                DocManager.Inst.ExecuteCmd(new SetPlayPosTickNotification(tick), true);
            }
        }

        # region ICmdSubscriber

        public void Subscribe(ICmdPublisher publisher) { if (publisher != null) publisher.Subscribe(this); }

        public void OnNext(UCommand cmd, bool isUndo)
        {
            if (cmd is SeekPlayPosTickNotification)
            {
                if (outDevice != null) outDevice.Stop();
                int tick = ((SeekPlayPosTickNotification)cmd).playPosTick;
                DocManager.Inst.ExecuteCmd(new SetPlayPosTickNotification(tick));
            }
        }

        # endregion
    }
}

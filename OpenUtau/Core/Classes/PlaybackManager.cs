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

        SequencingSampleProvider bus;
        public void Play(SequencingSampleProvider source)
        {
            if (outDevice != null && outDevice.PlaybackState == PlaybackState.Playing) return;
            if (outDevice != null) outDevice.Dispose();
            bus = source;
            outDevice = new WaveOut();
            outDevice.Init(bus);
            outDevice.Play();
            outDevice.PlaybackStopped += (o, e) => {
                System.Diagnostics.Debug.WriteLine(bus.LastSample);
            };
        }

        public void UpdatePlayPos()
        {
            if (outDevice != null && outDevice.PlaybackState == PlaybackState.Playing)
            {
                double ms = outDevice.GetPosition() * 1000.0 / bus.WaveFormat.BitsPerSample * 8 / bus.WaveFormat.SampleRate;
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

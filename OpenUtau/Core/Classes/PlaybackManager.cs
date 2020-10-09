using System;
using System.Collections.Generic;
using System.IO;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OpenUtau.Core.USTx;
using OpenUtau.Core.Render;
using OpenUtau.Core.ResamplerDriver;
using Serilog;

namespace OpenUtau.Core
{
    class PlaybackManager : ICmdSubscriber
    {
        private WaveOut outDevice;

        private PlaybackManager() { this.Subscribe(DocManager.Inst); }

        private static PlaybackManager _s;
        public static PlaybackManager Inst { get { if (_s == null) { _s = new PlaybackManager(); } return _s; } }

        MixingSampleProvider masterMix;
        List<TrackSampleProvider> trackSources;

        public bool Playing => pendingParts > 0 || outDevice != null && outDevice.PlaybackState == PlaybackState.Playing;

        public bool CheckResampler() {
            var path = PathManager.Inst.GetPreviewEnginePath();
            Directory.CreateDirectory(PathManager.Inst.GetEngineSearchPath());
            return File.Exists(path); // TODO: validate exe / dll
        }

        public void Play(UProject project)
        {
            if (pendingParts > 0) return; // Rendering
            else if (outDevice != null)
            {
                if (outDevice.PlaybackState == PlaybackState.Playing) return;
                else if (outDevice.PlaybackState == PlaybackState.Paused) { outDevice.Play(); return; }
                else outDevice.Dispose();
            }
            BuildAudio(project);
        }

        public void StopPlayback()
        {
            if (outDevice != null) outDevice.Stop();
        }

        public void PausePlayback()
        {
            if (outDevice != null) outDevice.Pause();
        }

        private void StartPlayback()
        {
            masterMix = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
            foreach (var source in trackSources)
                masterMix.AddMixerInput(source);
            outDevice = new WaveOut();
            outDevice.Init(masterMix);
            outDevice.Play();
        }

        private ISampleProvider BuildWavePartAudio(UWavePart part, UProject project)
        {
            AudioFileReader stream;
            try { stream = new AudioFileReader(part.FilePath); }
            catch { return null; }
            return new WaveToSampleProvider(stream);
        }

        private void BuildVoicePartAudio(UVoicePart part, UProject project,IResamplerDriver engine)
        {
            ResamplerInterface ri = new ResamplerInterface();
            ri.ResamplePart(part, project, engine, (o) => { this.BuildVoicePartDone(o, part, project); });
        }

        private void BuildVoicePartDone(SequencingSampleProvider source, UPart part, UProject project) {
            lock (lockObject) {
                if (source != null) {
                    trackSources[part.TrackNo].AddSource(
                        source,
                        TimeSpan.FromMilliseconds(project.TickToMillisecond(part.PosTick))
                    );
                }
                pendingParts--;
            }
            if (pendingParts == 0) StartPlayback();
        }

        int pendingParts = 0;
        private readonly object lockObject = new object();

        private void BuildAudio(UProject project) {
            trackSources = new List<TrackSampleProvider>();
            foreach (UTrack track in project.Tracks) {
                trackSources.Add(new TrackSampleProvider() { Volume = DecibelToVolume(track.Volume) });
            }
            pendingParts = project.Parts.Count;
            foreach (UPart part in project.Parts) {
                if (part is UWavePart) {
                    lock (lockObject) {
                        trackSources[part.TrackNo].AddSource(
                            BuildWavePartAudio(part as UWavePart, project),
                            TimeSpan.FromMilliseconds(project.TickToMillisecond(part.PosTick))
                        );
                        pendingParts--;
                    }
                } else {
                    var singer = project.Tracks[part.TrackNo].Singer;
                    if (singer != null && singer.Loaded) {
                        FileInfo ResamplerFile = new FileInfo(PathManager.Inst.GetPreviewEnginePath());
                        IResamplerDriver driver = ResamplerDriver.ResamplerDriver.Load(ResamplerFile.FullName);
                        if (driver != null) {
                            Log.Information($"ResamplerDriver loaded ({driver.GetType()}).");
                            BuildVoicePartAudio(part as UVoicePart, project, driver);
                        } else {
                            Log.Error("ResamplerDriver failed to load.");
                        }
                    } else lock (lockObject) { pendingParts--; }
                }
            }

            if (pendingParts == 0) StartPlayback();
        }

        public void UpdatePlayPos()
        {
            if (outDevice != null && outDevice.PlaybackState == PlaybackState.Playing)
            {
                double ms = outDevice.GetPosition() * 1000.0 / masterMix.WaveFormat.BitsPerSample /masterMix.WaveFormat.Channels * 8 / masterMix.WaveFormat.SampleRate;
                int tick = DocManager.Inst.Project.MillisecondToTick(ms);
                DocManager.Inst.ExecuteCmd(new SetPlayPosTickNotification(tick), true);
            }
        }

        private float DecibelToVolume(double db)
        {
            return (db == -24) ? 0 : (float)((db < -16) ? MusicMath.DecibelToLinear(db * 2 + 16) : MusicMath.DecibelToLinear(db));
        }

        # region ICmdSubscriber

        public void Subscribe(ICmdPublisher publisher) { if (publisher != null) publisher.Subscribe(this); }

        public void OnNext(UCommand cmd, bool isUndo)
        {
            if (cmd is SeekPlayPosTickNotification)
            {
                StopPlayback();
                int tick = ((SeekPlayPosTickNotification)cmd).playPosTick;
                DocManager.Inst.ExecuteCmd(new SetPlayPosTickNotification(tick));
            }
            else if (cmd is VolumeChangeNotification)
            {
                var _cmd = cmd as VolumeChangeNotification;
                if (trackSources != null && trackSources.Count > _cmd.TrackNo)
                {
                    trackSources[_cmd.TrackNo].Volume = DecibelToVolume(_cmd.Volume);
                }
            }
        }

        # endregion
    }
}

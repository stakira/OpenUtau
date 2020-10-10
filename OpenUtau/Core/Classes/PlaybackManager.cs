using System;
using System.Collections.Generic;
using System.IO;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OpenUtau.Core.USTx;
using OpenUtau.Core.Render;
using OpenUtau.Core.ResamplerDriver;
using Serilog;
using System.Threading.Tasks;

namespace OpenUtau.Core {
    class PlaybackManager : ICmdSubscriber {
        private WaveOutEvent outDevice;

        private PlaybackManager() { this.Subscribe(DocManager.Inst); }

        private static PlaybackManager _s;
        public static PlaybackManager Inst { get { if (_s == null) { _s = new PlaybackManager(); } return _s; } }

        MixingSampleProvider masterMix;
        List<TrackSampleProvider> trackSources;
        Task<List<TrackSampleProvider>> renderTask;

        public bool Playing => renderTask != null || outDevice != null && outDevice.PlaybackState == PlaybackState.Playing;

        public bool CheckResampler() {
            var path = PathManager.Inst.GetPreviewEnginePath();
            Directory.CreateDirectory(PathManager.Inst.GetEngineSearchPath());
            return File.Exists(path); // TODO: validate exe / dll
        }

        public void Play(UProject project) {
            if (renderTask != null) {
                if (renderTask.IsCompleted) {
                    renderTask = null;
                } else {
                    return;
                }
            }
            if (outDevice != null) {
                if (outDevice.PlaybackState == PlaybackState.Playing) {
                    return;
                }
                if (outDevice.PlaybackState == PlaybackState.Paused) {
                    outDevice.Play();
                    return;
                }
                outDevice.Dispose();
            }
            Render(project);
        }

        public void StopPlayback() {
            if (outDevice != null) outDevice.Stop();
        }

        public void PausePlayback() {
            if (outDevice != null) outDevice.Pause();
        }

        private void StartPlayback() {
            masterMix = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
            foreach (var source in trackSources)
                masterMix.AddMixerInput(source);
            outDevice = new WaveOutEvent();
            outDevice.Init(masterMix);
            outDevice.Play();
        }

        private void Render(UProject project) {
            FileInfo ResamplerFile = new FileInfo(PathManager.Inst.GetPreviewEnginePath());
            IResamplerDriver driver = ResamplerDriver.ResamplerDriver.Load(ResamplerFile.FullName);
            if (driver == null) {
                return;
            }
            Task.Run(() => {
                var task = Task.Run(async () => {
                    RenderEngine engine = new RenderEngine(project, driver);
                    renderTask = engine.RenderAsync();
                    trackSources = await renderTask;
                    StartPlayback();
                    renderTask = null;
                });
                try {
                    task.Wait();
                } catch (AggregateException ae) {
                    foreach (var e in ae.Flatten().InnerExceptions) {
                        Log.Error(e, "Failed to render.");
                    }
                }
            });
        }

        public void UpdatePlayPos() {
            if (outDevice != null && outDevice.PlaybackState == PlaybackState.Playing) {
                double ms = outDevice.GetPosition() * 1000.0 / masterMix.WaveFormat.BitsPerSample / masterMix.WaveFormat.Channels * 8 / masterMix.WaveFormat.SampleRate;
                int tick = DocManager.Inst.Project.MillisecondToTick(ms);
                DocManager.Inst.ExecuteCmd(new SetPlayPosTickNotification(tick), true);
            }
        }

        public static float DecibelToVolume(double db) {
            return (db == -24) ? 0 : (float)((db < -16) ? MusicMath.DecibelToLinear(db * 2 + 16) : MusicMath.DecibelToLinear(db));
        }

        # region ICmdSubscriber

        public void Subscribe(ICmdPublisher publisher) { if (publisher != null) publisher.Subscribe(this); }

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is SeekPlayPosTickNotification) {
                StopPlayback();
                int tick = ((SeekPlayPosTickNotification)cmd).playPosTick;
                DocManager.Inst.ExecuteCmd(new SetPlayPosTickNotification(tick));
            } else if (cmd is VolumeChangeNotification) {
                var _cmd = cmd as VolumeChangeNotification;
                if (trackSources != null && trackSources.Count > _cmd.TrackNo) {
                    trackSources[_cmd.TrackNo].Volume = DecibelToVolume(_cmd.Volume);
                }
            }
        }

        # endregion
    }
}

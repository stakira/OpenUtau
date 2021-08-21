using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Render;
using OpenUtau.Core.ResamplerDriver;
using Serilog;

namespace OpenUtau.Core {
    class PlaybackManager : ICmdSubscriber {
        private WaveOutEvent outDevice;
        private WaveOutEvent testDevice;

        private PlaybackManager() {
            DocManager.Inst.AddSubscriber(this);
            if (Guid.TryParse(Util.Preferences.Default.PlaybackDevice, out var guid)) {
                SelectOutputDevice(guid, Util.Preferences.Default.PlaybackDeviceNumber);
            } else {
                SelectOutputDevice(new Guid(), 0);
            }
        }

        private static PlaybackManager _s;
        public static PlaybackManager Inst { get { if (_s == null) { _s = new PlaybackManager(); } return _s; } }

        ISampleProvider masterMix;
        double startMs;
        List<TrackSampleProvider> trackSources;
        Task<List<TrackSampleProvider>> renderTask;
        readonly RenderCache cache = new RenderCache(2048);

        object previewDriverLockObj = new object();
        string resamplerSelected;
        IResamplerDriver previewDriver;
        CancellationTokenSource previewCancellationTokenSource;

        public int PlaybackDeviceNumber { get; private set; }
        public bool Playing => renderTask != null || outDevice != null && outDevice.PlaybackState == PlaybackState.Playing;

        public List<WaveOutCapabilities> GetOutputDevices() {
            var outDevices = new List<WaveOutCapabilities>();
            for (int i = 0; i < WaveOut.DeviceCount; ++i) {
                outDevices.Add(WaveOut.GetCapabilities(i));
            }
            return outDevices;
        }

        public void SelectOutputDevice(Guid productGuid, int deviceNumber) {
            // Product guid may not be unique. Use device number first.
            if (deviceNumber < WaveOut.DeviceCount && WaveOut.GetCapabilities(deviceNumber).ProductGuid == productGuid) {
                PlaybackDeviceNumber = deviceNumber;
                return;
            }
            // If guid does not match, device number may have changed. Search guid instead.
            PlaybackDeviceNumber = 0;
            for (int i = 0; i < WaveOut.DeviceCount; ++i) {
                if (WaveOut.GetCapabilities(i).ProductGuid == productGuid) {
                    PlaybackDeviceNumber = i;
                    break;
                }
            }
        }

        public bool CheckResampler() {
            var path = PathManager.Inst.GetPreviewEnginePath();
            Directory.CreateDirectory(PathManager.Inst.GetEngineSearchPath());
            return File.Exists(path); // TODO: validate exe / dll
        }

        public void PlayTestSound() {
            if (testDevice != null) {
                testDevice.Dispose();
            }
            testDevice = new WaveOutEvent() { DeviceNumber = PlaybackDeviceNumber };
            testDevice.Init(new SignalGenerator().Take(TimeSpan.FromSeconds(1)));
            testDevice.Play();
        }

        public void Play(UProject project, int tick) {
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
            Render(project, tick);
        }

        public void StopPlayback() {
            if (outDevice != null) outDevice.Stop();
        }

        public void PausePlayback() {
            if (outDevice != null) outDevice.Pause();
        }

        private void StartPlayback(double startMs) {
            this.startMs = startMs;
            var start = TimeSpan.FromMilliseconds(startMs);
            Log.Information($"StartPlayback at {start}");
            var mix = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
            foreach (var source in trackSources) {
                mix.AddMixerInput(source);
            }
            masterMix = mix;
            outDevice = new WaveOutEvent() { DeviceNumber = PlaybackDeviceNumber };
            outDevice.Init(masterMix);
            outDevice.Play();
        }

        private IResamplerDriver GetPreviewDriver() {
            lock (previewDriverLockObj) {
                var resamplerPath = PathManager.Inst.GetPreviewEnginePath();
                if (resamplerPath == resamplerSelected) {
                    return previewDriver;
                }
                FileInfo resamplerFile = new FileInfo(resamplerPath);
                previewDriver = ResamplerDriver.ResamplerDriver.Load(resamplerFile.FullName);
                resamplerSelected = resamplerPath;
                return previewDriver;
            }
        }

        private void Render(UProject project, int tick) {
            IResamplerDriver driver = GetPreviewDriver();
            if (driver == null) {
                return;
            }
            StopPreRender();
            Task.Run(() => {
                var task = Task.Run(async () => {
                    RenderEngine engine = new RenderEngine(project, driver, cache, tick);
                    renderTask = engine.RenderAsync();
                    trackSources = await renderTask;
                    StartPlayback(project.TickToMillisecond(tick));
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
                int tick = DocManager.Inst.Project.MillisecondToTick(startMs + ms);
                DocManager.Inst.ExecuteCmd(new SetPlayPosTickNotification(tick));
            }
        }

        public static float DecibelToVolume(double db) {
            return (db == -24) ? 0 : (float)MusicMath.DecibelToLinear((db < -16) ? db * 2 + 16 : db);
        }

        public void RenderToFiles(UProject project) {
            FileInfo ResamplerFile = new FileInfo(PathManager.Inst.GetExportEnginePath());
            IResamplerDriver driver = ResamplerDriver.ResamplerDriver.Load(ResamplerFile.FullName);
            if (driver == null) {
                return;
            }
            StopPreRender();
            Task.Run(() => {
                var task = Task.Run(async () => {
                    RenderEngine engine = new RenderEngine(project, driver, cache);
                    renderTask = engine.RenderAsync();
                    trackSources = await renderTask;
                    for (int i = 0; i < trackSources.Count; ++i) {
                        var file = PathManager.Inst.GetExportPath(project.FilePath, i + 1);
                        DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, $"Exporting to {file}."));
                        WaveFileWriter.CreateWaveFile16(file, trackSources[i]);
                        DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, $"Exported to {file}."));
                    }
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

        void SchedulePreRender() {
            var driver = GetPreviewDriver();
            if (driver == null) {
                return;
            }
            Log.Information("SchedulePreRender");
            var engine = new RenderEngine(DocManager.Inst.Project, driver, cache);
            var source = engine.PreRenderProject();
            source = Interlocked.Exchange(ref previewCancellationTokenSource, source);
            if (source != null) {
                source.Cancel();
            }
        }

        void StopPreRender() {
            var source = Interlocked.Exchange(ref previewCancellationTokenSource, null);
            if (source != null) {
                source.Cancel();
            }
        }

        #region ICmdSubscriber

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
            if (!(cmd is UNotification) || cmd is LoadProjectNotification) {
                SchedulePreRender();
            }
        }

        #endregion
    }
}

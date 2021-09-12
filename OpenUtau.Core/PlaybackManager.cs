using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OpenUtau.Core.Render;
using OpenUtau.Core.ResamplerDriver;
using OpenUtau.Core.SignalChain;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Core {
    public interface IAudioOutput {
        PlaybackState PlaybackState { get; }
        int DeviceNumber { get; }

        void SelectDevice(Guid productGuid, int deviceNumber);
        void Init(ISampleProvider waveProvider);
        void Pause();
        void Play();
        void Stop();
        long GetPosition();
    }

    public interface IAudioFileUtils {
        void GetAudioFileInfo(string file, out WaveFormat waveFormat, out TimeSpan duration);
        WaveStream OpenAudioFileAsWaveStream(string file);
        ISampleProvider OpenAudioFileAsSampleProvider(string file);
    }

    public static class AudioFileUtilsProvider {
        public static IAudioFileUtils Utils { get; set; }
    }

    public class PlaybackManager : ICmdSubscriber {
        private PlaybackManager() {
            DocManager.Inst.AddSubscriber(this);
        }

        private static PlaybackManager _s;
        public static PlaybackManager Inst { get { if (_s == null) { _s = new PlaybackManager(); } return _s; } }

        readonly RenderCache cache = new RenderCache(2048);
        List<Fader> faders;
        MasterAdapter masterMix;
        double startMs;

        object previewDriverLockObj = new object();
        string resamplerSelected;
        IResamplerDriver previewDriver;
        CancellationTokenSource previewCancellationTokenSource;

        public IAudioOutput AudioOutput { get; set; }
        public bool Playing => AudioOutput.PlaybackState == PlaybackState.Playing;

        public bool CheckResampler() {
            var path = PathManager.Inst.GetPreviewEnginePath();
            Directory.CreateDirectory(PathManager.Inst.GetEngineSearchPath());
            return File.Exists(path); // TODO: validate exe / dll
        }

        public void PlayTestSound() {
            AudioOutput.Stop();
            AudioOutput.Init(new SignalGenerator().Take(TimeSpan.FromSeconds(1)));
            AudioOutput.Play();
        }

        public void Play(UProject project, int tick) {
            if (AudioOutput.PlaybackState == PlaybackState.Paused) {
                AudioOutput.Play();
                return;
            }
            AudioOutput.Stop();
            Render(project, tick);
        }

        public void StopPlayback() {
            AudioOutput.Stop();
        }

        public void PausePlayback() {
            AudioOutput.Pause();
        }

        private void StartPlayback(double startMs, MasterAdapter masterAdapter) {
            this.startMs = startMs;
            var start = TimeSpan.FromMilliseconds(startMs);
            Log.Information($"StartPlayback at {start}");
            masterMix = masterAdapter;
            AudioOutput.Stop();
            AudioOutput.Init(masterMix);
            AudioOutput.Play();
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
            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            Task.Run(() => {
                RenderEngine engine = new RenderEngine(project, driver, cache, tick);
                var result = engine.RenderProject(tick);
                faders = result.Item2;
                StartPlayback(project.TickToMillisecond(tick), result.Item1);
            }).ContinueWith((task) => {
                if (task.IsFaulted) {
                    Log.Information($"{task.Exception}");
                    DocManager.Inst.ExecuteCmd(new UserMessageNotification(task.Exception.ToString()));
                    throw task.Exception;
                }
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, scheduler);
        }

        public void UpdatePlayPos() {
            if (AudioOutput.PlaybackState == PlaybackState.Playing && masterMix != null) {
                double ms = (AudioOutput.GetPosition() / sizeof(float) - masterMix.Paused) * 1000.0 / 44100;
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
                var task = Task.Run(() => {
                    RenderEngine engine = new RenderEngine(project, driver, cache);
                    var trackMixes = engine.RenderTracks();
                    for (int i = 0; i < trackMixes.Count; ++i) {
                        var file = PathManager.Inst.GetExportPath(project.FilePath, i + 1);
                        DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, $"Exporting to {file}."));
                        WaveFileWriter.CreateWaveFile16(file, new ExportAdapter(trackMixes[i]));
                        DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, $"Exported to {file}."));
                    }
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
                if (faders != null && faders.Count > _cmd.TrackNo) {
                    faders[_cmd.TrackNo].Scale = DecibelToVolume(_cmd.Volume);
                }
            }
            if (!(cmd is UNotification) || cmd is LoadProjectNotification) {
                SchedulePreRender();
            }
        }

        #endregion
    }
}

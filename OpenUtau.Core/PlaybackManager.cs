using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OpenUtau.Core.Render;
using OpenUtau.Core.ResamplerDriver;
using OpenUtau.Core.SignalChain;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Core {
    public class AudioOutputDevice {
        public string name;
        public string api;
        public int deviceNumber;
        public Guid guid;
        public object data;

        public override string ToString() => $"[{api}] {name}";
    }

    public interface IAudioOutput {
        PlaybackState PlaybackState { get; }
        int DeviceNumber { get; }

        void SelectDevice(Guid guid, int deviceNumber);
        void Init(ISampleProvider sampleProvider);
        void Pause();
        void Play();
        void Stop();
        long GetPosition();

        List<AudioOutputDevice> GetOutputDevices();
    }

    class DummyAudioOutput : IAudioOutput {
        public PlaybackState PlaybackState => PlaybackState.Stopped;
        public int DeviceNumber => 0;
        public List<AudioOutputDevice> GetOutputDevices() => new List<AudioOutputDevice>();
        public long GetPosition() => 0;
        public void Init(ISampleProvider sampleProvider) { }
        public void Pause() { }
        public void Play() { }
        public void SelectDevice(Guid guid, int deviceNumber) { }
        public void Stop() { }
    }

    public class SineGen : ISampleProvider {
        public WaveFormat WaveFormat => waveFormat;
        public double Freq { get; set; }
        public bool Stop { get; set; }
        private WaveFormat waveFormat;
        private double phase;
        private double gain;
        public SineGen() {
            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);
            Freq = 440;
            gain = 1;
        }
        public int Read(float[] buffer, int offset, int count) {
            double delta = 2 * Math.PI * Freq / waveFormat.SampleRate;
            for (int i = 0; i < count; i++) {
                if (Stop) {
                    gain = Math.Max(0, gain - 0.01);
                }
                if (gain == 0) {
                    return i;
                }
                phase += delta;
                double sampleValue = Math.Sin(phase) * 0.2 * gain;
                buffer[offset++] = (float)sampleValue;
            }
            return count;
        }
    }

    public class PlaybackManager : ICmdSubscriber {
        private PlaybackManager() {
            DocManager.Inst.AddSubscriber(this);
            RenderEngine.ReleaseSourceTemp();
        }

        private static PlaybackManager _s;
        public static PlaybackManager Inst { get { if (_s == null) { _s = new PlaybackManager(); } return _s; } }

        readonly RenderCache cache = new RenderCache(4096);
        List<Fader> faders;
        MasterAdapter masterMix;
        double startMs;
        CancellationTokenSource playbakCancellationTokenSource;

        object previewDriverLockObj = new object();
        string resamplerSelected;
        IResamplerDriver previewDriver;
        CancellationTokenSource previewCancellationTokenSource;

        public IAudioOutput AudioOutput { get; set; } = new DummyAudioOutput();
        public bool Playing => AudioOutput.PlaybackState == PlaybackState.Playing;

        public bool CheckResampler() => ResamplerDrivers.CheckPreviewResampler();

        public void PlayTestSound() {
            masterMix = null;
            AudioOutput.Stop();
            AudioOutput.Init(new SignalGenerator(44100, 1).Take(TimeSpan.FromSeconds(1)));
            AudioOutput.Play();
        }

        public SineGen PlayTone(double freq) {
            masterMix = null;
            AudioOutput.Stop();
            var sineGen = new SineGen() {
                Freq = freq,
            };
            AudioOutput.Init(sineGen);
            AudioOutput.Play();
            return sineGen;
        }

        public bool PlayOrPause() {
            if (Playing) {
                PausePlayback();
                return true;
            }
            if (!ResamplerDrivers.CheckPreviewResampler()) {
                return false;
            }
            Play(DocManager.Inst.Project, DocManager.Inst.playPosTick);
            return true;
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

        private void Render(UProject project, int tick) {
            if (!ResamplerDrivers.CheckPreviewResampler()) {
                return;
            }
            var driver = ResamplerDrivers.GetResampler(Preferences.Default.ExternalPreviewEngine);
            if (driver == null) {
                return;
            }
            StopPreRender();
            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            Task.Run(() => {
                RenderEngine engine = new RenderEngine(project, driver, cache, tick);
                var result = engine.RenderProject(tick);
                faders = result.Item2;
                var source = result.Item3;
                source = Interlocked.Exchange(ref playbakCancellationTokenSource, source);
                if (source != null) {
                    source.Cancel();
                    Log.Information("Cancelling previous render");
                }
                StartPlayback(project.TickToMillisecond(tick), result.Item1);
            }).ContinueWith((task) => {
                if (task.IsFaulted) {
                    Log.Error(task.Exception, "Failed to render.");
                    DocManager.Inst.ExecuteCmd(new UserMessageNotification(task.Exception.ToString()));
                    throw task.Exception;
                }
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, scheduler);
        }

        public void UpdatePlayPos() {
            if (AudioOutput != null && AudioOutput.PlaybackState == PlaybackState.Playing && masterMix != null) {
                double ms = (AudioOutput.GetPosition() / sizeof(float) - masterMix.Paused / 2) * 1000.0 / 44100;
                int tick = DocManager.Inst.Project.MillisecondToTick(startMs + ms);
                DocManager.Inst.ExecuteCmd(new SetPlayPosTickNotification(tick));
            }
        }

        public static float DecibelToVolume(double db) {
            return (db <= -24) ? 0 : (float)MusicMath.DecibelToLinear((db < -16) ? db * 2 + 16 : db);
        }

        public void RenderToFiles(UProject project) {
            var driver = ResamplerDrivers.GetResampler(Preferences.Default.ExternalExportEngine);
            if (driver == null) {
                return;
            }
            StopPreRender();
            Task.Run(() => {
                var task = Task.Run(() => {
                    RenderEngine engine = new RenderEngine(project, driver, cache);
                    var trackMixes = engine.RenderTracks();
                    for (int i = 0; i < trackMixes.Count; ++i) {
                        if (project.tracks.Count > i) {
                            if (project.tracks[i].Mute) {
                                continue;
                            }
                        }
                        var file = PathManager.Inst.GetExportPath(project.FilePath, i + 1);
                        DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, $"Exporting to {file}."));
                        WaveFileWriter.CreateWaveFile16(file, new ExportAdapter(trackMixes[i]).ToMono(1, 0));
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
            var driver = ResamplerDrivers.GetResampler(Preferences.Default.ExternalPreviewEngine);
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

        public void ClearRenderCache() {
            cache.Clear();
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
            } else if (cmd is LoadProjectNotification) {
                StopPlayback();
                DocManager.Inst.ExecuteCmd(new SetPlayPosTickNotification(0));
            }
            if (cmd is PreRenderNotification || cmd is LoadProjectNotification) {
                SchedulePreRender();
            }
        }

        #endregion
    }
}

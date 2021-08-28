using System;
using System.Collections.Generic;
using System.Text;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;

namespace OpenUtau.App.ViewModels {
    public class MainWindowViewModel : ViewModelBase {
        public string AppVersion => $"OpenUtau v{System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version}";

        private TracksViewModel tracksViewModel;

        public MainWindowViewModel() {
            tracksViewModel = new TracksViewModel();
        }

        UProject Project => DocManager.Inst.Project;
        public int BeatPerBar {
            get => Project.beatPerBar;
            set => this.RaiseAndSetIfChanged(ref Project.beatPerBar, value);
        }
        public int BeatUnit {
            get => Project.beatPerBar;
            set => this.RaiseAndSetIfChanged(ref Project.beatUnit, value);
        }
        public double BPM {
            get => Project.bpm;
            set => this.RaiseAndSetIfChanged(ref Project.bpm, value);
        }
        public TimeSpan PlayPosTime => TimeSpan.FromMilliseconds((int)Project.TickToMillisecond(DocManager.Inst.playPosTick));

        public void SeekStart() {
            Stop();
            DocManager.Inst.ExecuteCmd(new SeekPlayPosTickNotification(0));
        }
        public void SeekEnd() {
            Stop();
            DocManager.Inst.ExecuteCmd(new SeekPlayPosTickNotification(Project.EndTick));
        }
        public bool PlayOrStop() {
            if (PlaybackManager.Inst.Playing) {
                Stop();
                return true;
            }
            if (!PlaybackManager.Inst.CheckResampler()) {
                return false;
            }
            PlaybackManager.Inst.Play(Project, DocManager.Inst.playPosTick);
            return true;
        }
        public void Stop() {
            PlaybackManager.Inst.PausePlayback();
        }
    }
}

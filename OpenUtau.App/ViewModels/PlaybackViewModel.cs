using System;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;

namespace OpenUtau.App.ViewModels {
    public class PlaybackViewModel : ViewModelBase {
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
        public int Resolution {
            get => Project.resolution;
            set => this.RaiseAndSetIfChanged(ref Project.resolution, value);
        }

        public TimeSpan PlayPosTime => TimeSpan.FromMilliseconds((int)Project.TickToMillisecond(DocManager.Inst.playPosTick));

        public void SeekStart() {
            Pause();
            DocManager.Inst.ExecuteCmd(new SeekPlayPosTickNotification(0));
        }
        public void SeekEnd() {
            Pause();
            DocManager.Inst.ExecuteCmd(new SeekPlayPosTickNotification(Project.EndTick));
        }
        public bool PlayOrPause() {
            if (PlaybackManager.Inst.Playing) {
                Pause();
                return true;
            }
            if (!PlaybackManager.Inst.CheckResampler()) {
                return false;
            }
            PlaybackManager.Inst.Play(Project, DocManager.Inst.playPosTick);
            return true;
        }
        public void Pause() {
            PlaybackManager.Inst.PausePlayback();
        }
    }
}

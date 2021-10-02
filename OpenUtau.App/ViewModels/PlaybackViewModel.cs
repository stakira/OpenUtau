using System;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;

namespace OpenUtau.App.ViewModels {
    public class PlaybackViewModel : ViewModelBase, ICmdSubscriber {
        UProject Project => DocManager.Inst.Project;
        public int BeatPerBar => Project.beatPerBar;
        public int BeatUnit => Project.beatUnit;
        public double Bpm => Project.bpm;
        public int Resolution => Project.resolution;
        public int PlayPosTick => DocManager.Inst.playPosTick;

        public TimeSpan PlayPosTime => TimeSpan.FromMilliseconds((int)Project.TickToMillisecond(DocManager.Inst.playPosTick));

        public PlaybackViewModel() {
            DocManager.Inst.AddSubscriber(this);
        }

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

        public void MovePlayPos(int tick) {
            if (DocManager.Inst.playPosTick != tick) {
                DocManager.Inst.ExecuteCmd(new SeekPlayPosTickNotification(Math.Max(0, tick)));
            }
        }

        public void SetTimeSignature(int beatPerBar, int beatUnit) {
            if (beatPerBar > 1 && (beatUnit == 2 || beatUnit == 4 || beatUnit == 8 || beatUnit == 16)) {
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new TimeSignatureCommand(Project, beatPerBar, beatUnit));
                DocManager.Inst.EndUndoGroup();
            }
        }

        public void SetBpm(double bpm) {
            if (bpm == DocManager.Inst.Project.bpm) {
                return;
            }
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new BpmCommand(Project, bpm));
            DocManager.Inst.EndUndoGroup();
        }

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is BpmCommand) {
                this.RaisePropertyChanged(nameof(Bpm));
            } else if (cmd is TimeSignatureCommand) {
                this.RaisePropertyChanged(nameof(BeatPerBar));
                this.RaisePropertyChanged(nameof(BeatUnit));
            } else if (cmd is SeekPlayPosTickNotification) {
                this.RaisePropertyChanged(nameof(PlayPosTick));
            }
        }
    }
}

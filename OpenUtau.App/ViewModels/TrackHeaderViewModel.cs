using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;

namespace OpenUtau.App.ViewModels {
    public class TrackHeaderViewModel : ViewModelBase {
        public int TrackNo => track.TrackNo;
        public USinger Singer => track.Singer;
        public Phonemizer Phonemizer => track.Phonemizer;
        public IEnumerable<USinger> Singers => DocManager.Inst.SingersOrdered;
        public IEnumerable<Phonemizer> Phonemizers => DocManager.Inst.Phonemizers;

        private readonly UTrack track;

        public TrackHeaderViewModel() {
        }

        public TrackHeaderViewModel(UTrack track) {
            this.track = track;
        }

        public void ManuallyRaise() {
            this.RaisePropertyChanged(nameof(Singer));
            this.RaisePropertyChanged(nameof(Phonemizer));
        }


        public void Remove() {
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new RemoveTrackCommand(DocManager.Inst.Project, track));
            DocManager.Inst.EndUndoGroup();
        }

        public void MoveUp() {
            if (track == DocManager.Inst.Project.tracks.First()) {
                return;
            }
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new MoveTrackCommand(DocManager.Inst.Project, track, true));
            DocManager.Inst.EndUndoGroup();
        }

        public void MoveDown() {
            if (track == DocManager.Inst.Project.tracks.Last()) {
                return;
            }
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new MoveTrackCommand(DocManager.Inst.Project, track, false));
            DocManager.Inst.EndUndoGroup();
        }
    }
}

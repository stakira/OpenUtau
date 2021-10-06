using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;

namespace OpenUtau.App.ViewModels {
    public class PianoRollViewModel : ViewModelBase, ICmdSubscriber {
        public NotesViewModel NotesViewModel { get; set; }
        public PlaybackViewModel PlaybackViewModel { get; set; }

        public PianoRollViewModel() {
            DocManager.Inst.AddSubscriber(this);
        }

        public void OnNext(UCommand cmd, bool isUndo) {
        }
    }
}

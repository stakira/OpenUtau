using OpenUtau.Core;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class PianoRollViewModel : ViewModelBase, ICmdSubscriber {
        [Reactive] public NotesViewModel NotesViewModel { get; set; }
        [Reactive] public PlaybackViewModel? PlaybackViewModel { get; set; }

        public PianoRollViewModel() {
            NotesViewModel = new NotesViewModel();
            DocManager.Inst.AddSubscriber(this);
        }

        public void Undo() {
            DocManager.Inst.Undo();
        }
        public void Redo() {
            DocManager.Inst.Redo();
        }

        public void OnNext(UCommand cmd, bool isUndo) {
        }
    }
}

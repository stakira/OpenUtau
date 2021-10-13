using System;
using System.Linq;
using System.Reactive;
using OpenUtau.Api;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace OpenUtau.App.ViewModels {
    public class PianoRollViewModel : ViewModelBase {
        public static ReactiveCommand<TransformerFactory, Unit>? TransformerCommand { get; private set; }

        [Reactive] public NotesViewModel NotesViewModel { get; set; }
        [Reactive] public PlaybackViewModel? PlaybackViewModel { get; set; }

        public Classic.Plugin[] Plugins => DocManager.Inst.Plugins;
        public TransformerFactory[] Transformers => DocManager.Inst.TransformerFactories;

        public PianoRollViewModel() {
            NotesViewModel = new NotesViewModel();
            TransformerCommand = ReactiveCommand.Create<TransformerFactory>((factory) => {
                var part = NotesViewModel.Part;
                if (part == null) {
                    return;
                }
                try {
                    var transformer = factory.Create();
                    DocManager.Inst.StartUndoGroup();
                    var notes = NotesViewModel.SelectedNotes.Count > 0 ?
                        NotesViewModel.SelectedNotes.ToArray() :
                        part.notes.ToArray();
                    string[] newLyrics = new string[notes.Length];
                    int i = 0;
                    foreach (var note in notes) {
                        newLyrics[i++] = transformer.Transform(note.lyric);
                    }
                    DocManager.Inst.ExecuteCmd(new ChangeNoteLyricCommand(part, notes, newLyrics));
                } catch (Exception e) {
                    Log.Error(e, $"Failed to run transformer {factory.name}");
                    DocManager.Inst.ExecuteCmd(new UserMessageNotification(e.ToString()));
                } finally {
                    DocManager.Inst.EndUndoGroup();
                }
            });
        }

        public void Undo() => DocManager.Inst.Undo();
        public void Redo() => DocManager.Inst.Redo();

        public void RenamePart(UVoicePart part, string name) {
            if (!string.IsNullOrWhiteSpace(name) && name != part.name) {
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new RenamePartCommand(DocManager.Inst.Project, part, name));
                DocManager.Inst.EndUndoGroup();
            }
        }
    }
}

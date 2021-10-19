using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using OpenUtau.Api;
using OpenUtau.Core;
using OpenUtau.Core.Editing;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace OpenUtau.App.ViewModels {
    public class PianoRollViewModel : ViewModelBase {
        public static ReactiveCommand<TransformerFactory, Unit>? TransformerCommand { get; private set; }

        public bool ExtendToFrame => OS.IsMacOS();
        [Reactive] public NotesViewModel NotesViewModel { get; set; }
        [Reactive] public PlaybackViewModel? PlaybackViewModel { get; set; }

        public Classic.Plugin[] Plugins => DocManager.Inst.Plugins;
        public TransformerFactory[] Transformers => DocManager.Inst.TransformerFactories;
        [Reactive] public List<MenuItemViewModel> NoteBatchEdits { get; set; }
        public ReactiveCommand<PitchPointHitInfo, Unit> PitEaseInOutCommand { get; set; }
        public ReactiveCommand<PitchPointHitInfo, Unit> PitLinearCommand { get; set; }
        public ReactiveCommand<PitchPointHitInfo, Unit> PitEaseInCommand { get; set; }
        public ReactiveCommand<PitchPointHitInfo, Unit> PitEaseOutCommand { get; set; }
        public ReactiveCommand<PitchPointHitInfo, Unit> PitSnapCommand { get; set; }
        public ReactiveCommand<PitchPointHitInfo, Unit> PitDelCommand { get; set; }
        public ReactiveCommand<PitchPointHitInfo, Unit> PitAddCommand { get; set; }

        private ReactiveCommand<NoteBatchEdit, Unit> noteBatchEditCommand;

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

            PitEaseInOutCommand = ReactiveCommand.Create<PitchPointHitInfo>(info => {
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new ChangePitchPointShapeCommand(info.Note.pitch.data[info.Index], PitchPointShape.io));
                DocManager.Inst.EndUndoGroup();
            });
            PitLinearCommand = ReactiveCommand.Create<PitchPointHitInfo>(info => {
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new ChangePitchPointShapeCommand(info.Note.pitch.data[info.Index], PitchPointShape.l));
                DocManager.Inst.EndUndoGroup();
            });
            PitEaseInCommand = ReactiveCommand.Create<PitchPointHitInfo>(info => {
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new ChangePitchPointShapeCommand(info.Note.pitch.data[info.Index], PitchPointShape.i));
                DocManager.Inst.EndUndoGroup();
            });
            PitEaseOutCommand = ReactiveCommand.Create<PitchPointHitInfo>(info => {
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new ChangePitchPointShapeCommand(info.Note.pitch.data[info.Index], PitchPointShape.o));
                DocManager.Inst.EndUndoGroup();
            });
            PitSnapCommand = ReactiveCommand.Create<PitchPointHitInfo>(info => {
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new SnapPitchPointCommand(info.Note));
                DocManager.Inst.EndUndoGroup();
            });
            PitDelCommand = ReactiveCommand.Create<PitchPointHitInfo>(info => {
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new DeletePitchPointCommand(NotesViewModel.Part, info.Note, info.Index));
                DocManager.Inst.EndUndoGroup();
            });
            PitAddCommand = ReactiveCommand.Create<PitchPointHitInfo>(info => {
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new AddPitchPointCommand(info.Note, new PitchPoint(info.X, info.Y), info.Index + 1));
                DocManager.Inst.EndUndoGroup();
            });

            noteBatchEditCommand = ReactiveCommand.Create<NoteBatchEdit>(edit => {
                if (NotesViewModel.Part != null) {
                    edit.Run(NotesViewModel.Project, NotesViewModel.Part, NotesViewModel.SelectedNotes, DocManager.Inst);
                }
            });
            NoteBatchEdits = new List<NoteBatchEdit>() {
                new AddTailDash(),
                new QuantizeNotes(15),
                new QuantizeNotes(30),
            }.Select(edit => new MenuItemViewModel() {
                Header = ThemeManager.GetString(edit.Name),
                Command = noteBatchEditCommand,
                CommandParameter = edit,
            }).ToList();
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

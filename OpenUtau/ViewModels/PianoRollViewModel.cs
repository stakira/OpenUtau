using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Security.Cryptography;
using Avalonia.Threading;
using DynamicData.Binding;
using OpenUtau.Core;
using OpenUtau.Core.Editing;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace OpenUtau.App.ViewModels {
    public class PhonemeMouseoverEvent {
        public readonly UPhoneme? mouseoverPhoneme;
        public PhonemeMouseoverEvent(UPhoneme? mouseoverPhoneme) {
            this.mouseoverPhoneme = mouseoverPhoneme;
        }
    }

    public class NotesContextMenuArgs {
        public PianoRollViewModel? ViewModel { get; set; }

        public bool ForNote { get; set; }
        public NoteHitInfo NoteHitInfo { get; set; }

        public bool ForPitchPoint { get; set; }
        public bool PitchPointIsFirst { get; set; }
        public bool PitchPointCanDel { get; set; }
        public bool PitchPointCanAdd { get; set; }
        public PitchPointHitInfo PitchPointHitInfo { get; set; }
    }

    public class PianoRollViewModel : ViewModelBase, ICmdSubscriber {

        public bool ExtendToFrame => OS.IsMacOS();
        [Reactive] public NotesViewModel NotesViewModel { get; set; }
        [Reactive] public PlaybackViewModel? PlaybackViewModel { get; set; }

        public ObservableCollectionExtended<MenuItemViewModel> LegacyPlugins { get; private set; }
            = new ObservableCollectionExtended<MenuItemViewModel>();
        public ObservableCollectionExtended<MenuItemViewModel> NoteBatchEdits { get; private set; }
            = new ObservableCollectionExtended<MenuItemViewModel>();
        public ObservableCollectionExtended<MenuItemViewModel> LyricBatchEdits { get; private set; }
            = new ObservableCollectionExtended<MenuItemViewModel>();
        public ObservableCollectionExtended<MenuItemViewModel> NotesContextMenuItems { get; private set; }
            = new ObservableCollectionExtended<MenuItemViewModel>();

        [Reactive] public double Progress { get; set; }
        public ReactiveCommand<NoteHitInfo, Unit> NoteDeleteCommand { get; set; }
        public ReactiveCommand<NoteHitInfo, Unit> NoteCopyCommand { get; set; }
        public ReactiveCommand<PitchPointHitInfo, Unit> PitEaseInOutCommand { get; set; }
        public ReactiveCommand<PitchPointHitInfo, Unit> PitLinearCommand { get; set; }
        public ReactiveCommand<PitchPointHitInfo, Unit> PitEaseInCommand { get; set; }
        public ReactiveCommand<PitchPointHitInfo, Unit> PitEaseOutCommand { get; set; }
        public ReactiveCommand<PitchPointHitInfo, Unit> PitSnapCommand { get; set; }
        public ReactiveCommand<PitchPointHitInfo, Unit> PitDelCommand { get; set; }
        public ReactiveCommand<PitchPointHitInfo, Unit> PitAddCommand { get; set; }

        private ReactiveCommand<Classic.Plugin, Unit> legacyPluginCommand;
        private ReactiveCommand<BatchEdit, Unit> noteBatchEditCommand;

        public PianoRollViewModel() {
            NotesViewModel = new NotesViewModel();

            NoteDeleteCommand = ReactiveCommand.Create<NoteHitInfo>(info => {
                NotesViewModel.DeleteSelectedNotes();
            });
            NoteCopyCommand = ReactiveCommand.Create<NoteHitInfo>(info => {
                NotesViewModel.CopyNotes();
            });
            PitEaseInOutCommand = ReactiveCommand.Create<PitchPointHitInfo>(info => {
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new ChangePitchPointShapeCommand(NotesViewModel.Part, info.Note.pitch.data[info.Index], PitchPointShape.io));
                DocManager.Inst.EndUndoGroup();
            });
            PitLinearCommand = ReactiveCommand.Create<PitchPointHitInfo>(info => {
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new ChangePitchPointShapeCommand(NotesViewModel.Part, info.Note.pitch.data[info.Index], PitchPointShape.l));
                DocManager.Inst.EndUndoGroup();
            });
            PitEaseInCommand = ReactiveCommand.Create<PitchPointHitInfo>(info => {
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new ChangePitchPointShapeCommand(NotesViewModel.Part, info.Note.pitch.data[info.Index], PitchPointShape.i));
                DocManager.Inst.EndUndoGroup();
            });
            PitEaseOutCommand = ReactiveCommand.Create<PitchPointHitInfo>(info => {
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new ChangePitchPointShapeCommand(NotesViewModel.Part, info.Note.pitch.data[info.Index], PitchPointShape.o));
                DocManager.Inst.EndUndoGroup();
            });
            PitSnapCommand = ReactiveCommand.Create<PitchPointHitInfo>(info => {
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new SnapPitchPointCommand(NotesViewModel.Part, info.Note));
                DocManager.Inst.EndUndoGroup();
            });
            PitDelCommand = ReactiveCommand.Create<PitchPointHitInfo>(info => {
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new DeletePitchPointCommand(NotesViewModel.Part, info.Note, info.Index));
                DocManager.Inst.EndUndoGroup();
            });
            PitAddCommand = ReactiveCommand.Create<PitchPointHitInfo>(info => {
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new AddPitchPointCommand(NotesViewModel.Part, info.Note, new PitchPoint(info.X, info.Y), info.Index + 1));
                DocManager.Inst.EndUndoGroup();
            });

            legacyPluginCommand = ReactiveCommand.Create<Classic.Plugin>(plugin => {
                if (NotesViewModel.Part == null || NotesViewModel.Part.notes.Count == 0) {
                    return;
                }
                try {
                    var project = NotesViewModel.Project;
                    var part = NotesViewModel.Part;
                    var tempFile = Path.Combine(PathManager.Inst.CachePath, "temp.tmp");
                    UNote? first = null;
                    UNote? last = null;
                    if (NotesViewModel.Selection.IsEmpty) {
                        first = part.notes.First();
                        last = part.notes.Last();
                    } else {
                        first = NotesViewModel.Selection.FirstOrDefault();
                        last = NotesViewModel.Selection.LastOrDefault();
                    }
                    var sequence = Classic.Ust.WritePlugin(project, part, first, last, tempFile, encoding: plugin.Encoding);
                    byte[]? beforeHash = HashFile(tempFile);
                    plugin.Run(tempFile);
                    byte[]? afterHash = HashFile(tempFile);
                    if (beforeHash == null || afterHash == null || Enumerable.SequenceEqual(beforeHash, afterHash)) {
                        Log.Information("Legacy plugin temp file has not changed.");
                        return;
                    }
                    Log.Information("Legacy plugin temp file has changed.");
                    var (toRemove, toAdd) = Classic.Ust.ParsePlugin(project, part, first, last, sequence, tempFile, encoding: plugin.Encoding);
                    DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new RemoveNoteCommand(part, toRemove));
                    DocManager.Inst.ExecuteCmd(new AddNoteCommand(part, toAdd));
                    DocManager.Inst.EndUndoGroup();
                } catch (Exception e) {
                    DocManager.Inst.ExecuteCmd(new ErrorMessageNotification("Failed to execute plugin", e));
                }
            });
            LegacyPlugins.AddRange(DocManager.Inst.Plugins.Select(plugin => new MenuItemViewModel() {
                Header = plugin.Name,
                Command = legacyPluginCommand,
                CommandParameter = plugin,
            }));

            noteBatchEditCommand = ReactiveCommand.Create<BatchEdit>(edit => {
                if (NotesViewModel.Part != null) {
                    edit.Run(NotesViewModel.Project, NotesViewModel.Part, NotesViewModel.Selection.ToList(), DocManager.Inst);
                }
            });
            NoteBatchEdits.AddRange(new List<BatchEdit>() {
                new LoadRenderedPitch(),
                new AddTailNote("-", "pianoroll.menu.notes.addtaildash"),
                new AddTailNote("R", "pianoroll.menu.notes.addtailrest"),
                new Transpose(12, "pianoroll.menu.notes.octaveup"),
                new Transpose(-12, "pianoroll.menu.notes.octavedown"),
                new QuantizeNotes(15),
                new QuantizeNotes(30),
                new HanziToPinyin(),
                new ResetPitchBends(),
                new ResetAllExpressions(),
                new ClearVibratos(),
                new ResetVibratos(),
                new ClearTimings(),
            }.Select(edit => new MenuItemViewModel() {
                Header = ThemeManager.GetString(edit.Name),
                Command = noteBatchEditCommand,
                CommandParameter = edit,
            }));
            LyricBatchEdits.AddRange(new List<BatchEdit>() {
                new RomajiToHiragana(),
                new HiraganaToRomaji(),
                new JapaneseVCVtoCV(),
                new RemoveToneSuffix(),
                new RemoveLetterSuffix(),
                new DashToPlus(),
            }.Select(edit => new MenuItemViewModel() {
                Header = ThemeManager.GetString(edit.Name),
                Command = noteBatchEditCommand,
                CommandParameter = edit,
            }));
            DocManager.Inst.AddSubscriber(this);
        }

        public void Undo() => DocManager.Inst.Undo();
        public void Redo() => DocManager.Inst.Redo();

        public void MouseoverPhoneme(UPhoneme? phoneme) {
            MessageBus.Current.SendMessage(new PhonemeMouseoverEvent(phoneme));
        }

        private byte[]? HashFile(string filePath) {
            using (var md5 = MD5.Create()) {
                using (var stream = File.OpenRead(filePath)) {
                    return md5.ComputeHash(stream);
                }
            }
        }

        #region ICmdSubscriber

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is ProgressBarNotification progressBarNotification) {
                Dispatcher.UIThread.InvokeAsync(() => {
                    Progress = progressBarNotification.Progress;
                });
            }
        }

        #endregion
    }
}

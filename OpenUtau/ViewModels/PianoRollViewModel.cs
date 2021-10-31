using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Security.Cryptography;
using OpenUtau.Core;
using OpenUtau.Core.Editing;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace OpenUtau.App.ViewModels {
    public class PianoRollViewModel : ViewModelBase {

        public bool ExtendToFrame => OS.IsMacOS();
        [Reactive] public NotesViewModel NotesViewModel { get; set; }
        [Reactive] public PlaybackViewModel? PlaybackViewModel { get; set; }

        [Reactive] public List<MenuItemViewModel> LegacyPlugins { get; set; }
        [Reactive] public List<MenuItemViewModel> NoteBatchEdits { get; set; }
        [Reactive] public List<MenuItemViewModel> LyricBatchEdits { get; set; }
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

            legacyPluginCommand = ReactiveCommand.Create<Classic.Plugin>(plugin => {
                if (NotesViewModel.Part == null) {
                    return;
                }
                try {
                    var project = NotesViewModel.Project;
                    var part = NotesViewModel.Part;
                    var tempFile = System.IO.Path.Combine(PathManager.Inst.GetCachePath(), "temp.tmp");
                    var newPart = (UVoicePart)part.Clone();
                    var sequence = Classic.Ust.WritePart(project, newPart, newPart.notes, tempFile);
                    byte[]? beforeHash = null;
                    using (var md5 = MD5.Create()) {
                        using (var stream = File.OpenRead(tempFile)) {
                            beforeHash = md5.ComputeHash(stream);
                        }
                    }
                    plugin.Run(tempFile);
                    byte[]? afterHash = null;
                    using (var md5 = MD5.Create()) {
                        using (var stream = File.OpenRead(tempFile)) {
                            afterHash = md5.ComputeHash(stream);
                        }
                    }
                    if (beforeHash != null && afterHash != null && !Enumerable.SequenceEqual(beforeHash, afterHash)) {
                        Log.Information("Legacy plugin temp file has changed.");
                        Classic.Ust.ParseDiffs(project, newPart, sequence, tempFile);
                        newPart.AfterLoad(project, project.tracks[part.trackNo]);
                        DocManager.Inst.StartUndoGroup();
                        DocManager.Inst.ExecuteCmd(new ReplacePartCommand(project, part, newPart));
                        DocManager.Inst.EndUndoGroup();
                    } else {
                        Log.Information("Legacy plugin temp file has not changed.");
                    }
                } catch (Exception e) {
                    DocManager.Inst.ExecuteCmd(new UserMessageNotification($"Failed to execute plugin {e}"));
                }
            });
            LegacyPlugins = DocManager.Inst.Plugins.Select(plugin => new MenuItemViewModel() {
                Header = plugin.Name,
                Command = legacyPluginCommand,
                CommandParameter = plugin,
            }).ToList();

            noteBatchEditCommand = ReactiveCommand.Create<BatchEdit>(edit => {
                if (NotesViewModel.Part != null) {
                    edit.Run(NotesViewModel.Project, NotesViewModel.Part, NotesViewModel.SelectedNotes, DocManager.Inst);
                }
            });
            NoteBatchEdits = new List<BatchEdit>() {
                new AddTailDash(),
                new QuantizeNotes(15),
                new QuantizeNotes(30),
            }.Select(edit => new MenuItemViewModel() {
                Header = ThemeManager.GetString(edit.Name),
                Command = noteBatchEditCommand,
                CommandParameter = edit,
            }).ToList();
            LyricBatchEdits = new List<BatchEdit>() {
                new RomajiToHiragana(),
                new HiraganaToRomaji(),
                new JapaneseVCVtoCV(),
                new RemoveToneSuffix(),
                new RemoveLetterSuffix(),
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

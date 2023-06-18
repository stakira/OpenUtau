using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Melanchall.DryWetMidi.MusicTheory;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class NotePropertyViewModel : ViewModelBase {

        [Reactive] public bool SetLyric { get; set; } = false;
        [Reactive] public string Lyric { get; set; } = "a";
        [Reactive] public bool SetPortamento { get; set; } = false;
        [Reactive] public int PortamentoLength { get; set; }
        [Reactive] public int PortamentoStart { get; set; }
        [Reactive] public bool SetVibrato { get; set; } = false;
        [Reactive] public float VibratoLength { get; set; }
        [Reactive] public float VibratoPeriod { get; set; }
        [Reactive] public float VibratoDepth { get; set; }
        [Reactive] public float VibratoIn { get; set; }
        [Reactive] public float VibratoOut { get; set; }
        [Reactive] public float VibratoShift { get; set; }
        [Reactive] public float AutoVibratoNoteLength { get; set; }
        [Reactive] public bool AutoVibratoToggle { get; set; }
        public List<NotePresets.PortamentoPreset>? PortamentoPresets { get; }
        public NotePresets.PortamentoPreset? ApplyPortamentoPreset {
            get => appliedPortamentoPreset;
            set => this.RaiseAndSetIfChanged(ref appliedPortamentoPreset, value);
        }
        public List<NotePresets.VibratoPreset>? VibratoPresets { get; }
        public NotePresets.VibratoPreset? ApplyVibratoPreset {
            get => appliedVibratoPreset;
            set => this.RaiseAndSetIfChanged(ref appliedVibratoPreset, value);
        }

        private NotePresets.PortamentoPreset? appliedPortamentoPreset = NotePresets.Default.DefaultPortamento;
        private NotePresets.VibratoPreset? appliedVibratoPreset = NotePresets.Default.DefaultVibrato;

        public bool IsPortamentoApplied => appliedPortamentoPreset != null;
        public bool IsVibratoApplied => appliedVibratoPreset != null;
        private NotesViewModel notesViewModel;

        public NotePropertyViewModel() {
            notesViewModel = new NotesViewModel();
        }

        public NotePropertyViewModel(NotesViewModel notesViewModel) {
            this.notesViewModel = notesViewModel;
            var note = notesViewModel.Selection.First();

            Lyric = note.lyric;
            AutoVibratoNoteLength = NotePresets.Default.AutoVibratoNoteDuration;
            AutoVibratoToggle = NotePresets.Default.AutoVibratoToggle;
            PortamentoPresets = NotePresets.Default.PortamentoPresets;
            VibratoPresets = NotePresets.Default.VibratoPresets;

            this.WhenAnyValue(vm => vm.ApplyPortamentoPreset)
                .WhereNotNull()
                .Subscribe(portamentoPreset => {
                    if (portamentoPreset != null) {
                        PortamentoLength = portamentoPreset.PortamentoLength;
                        PortamentoStart = portamentoPreset.PortamentoStart;
                    }
                });
            this.WhenAnyValue(vm => vm.ApplyVibratoPreset)
                .WhereNotNull()
                .Subscribe(vibratoPreset => {
                    if (vibratoPreset != null) {
                        VibratoLength = Math.Max(0, Math.Min(100, vibratoPreset.VibratoLength));
                        VibratoPeriod = Math.Max(5, Math.Min(500, vibratoPreset.VibratoPeriod));
                        VibratoDepth = Math.Max(5, Math.Min(200, vibratoPreset.VibratoDepth));
                        VibratoIn = Math.Max(0, Math.Min(100, vibratoPreset.VibratoIn));
                        VibratoOut = Math.Max(0, Math.Min(100, vibratoPreset.VibratoOut));
                        VibratoShift = Math.Max(0, Math.Min(100, vibratoPreset.VibratoShift));
                    }
                });
            PortamentoLength = NotePresets.Default.DefaultPortamento.PortamentoLength;
            PortamentoStart = NotePresets.Default.DefaultPortamento.PortamentoStart;
            VibratoLength = note.vibrato.length;
            VibratoPeriod = note.vibrato.period;
            VibratoDepth = note.vibrato.depth;
            VibratoIn = note.vibrato.@in;
            VibratoOut = note.vibrato.@out;
            VibratoShift = note.vibrato.shift;
        }

        // presets
        public void SavePortamentoPreset(string name) {
            if (string.IsNullOrEmpty(name)) {
                return;
            }
            NotePresets.Default.PortamentoPresets.Add(new NotePresets.PortamentoPreset(name, PortamentoLength, PortamentoStart));
            NotePresets.Save();
        }
        public void RemoveAppliedPortamentoPreset() {
            if (appliedPortamentoPreset == null) {
                return;
            }
            NotePresets.Default.PortamentoPresets.Remove(appliedPortamentoPreset);
            NotePresets.Save();
        }
        public void SaveVibratoPreset(string name) {
            if (string.IsNullOrEmpty(name)) {
                return;
            }
            NotePresets.Default.VibratoPresets.Add(new NotePresets.VibratoPreset(name, VibratoLength, VibratoPeriod, VibratoDepth, VibratoIn, VibratoOut, VibratoShift));
            NotePresets.Save();
        }
        public void RemoveAppliedVibratoPreset() {
            if (appliedVibratoPreset == null) {
                return;
            }
            NotePresets.Default.VibratoPresets.Remove(appliedVibratoPreset);
            NotePresets.Save();
        }

        public void Finish() {
            if (notesViewModel.Part != null) {
                UVoicePart part = notesViewModel.Part;
                List<UNote> selectedNotes = notesViewModel.Selection.ToList();

                DocManager.Inst.StartUndoGroup();

                if (SetLyric) {
                    foreach (UNote note in selectedNotes) {
                        if (note.lyric != Lyric) {
                            DocManager.Inst.ExecuteCmd(new ChangeNoteLyricCommand(part, note, Lyric));
                        }
                    }
                }
                if (SetPortamento) {
                    foreach (UNote note in selectedNotes) {
                        var pitch = new UPitch();
                        pitch.AddPoint(new PitchPoint(PortamentoStart, 0));
                        pitch.AddPoint(new PitchPoint(PortamentoStart + PortamentoLength, 0));
                        DocManager.Inst.ExecuteCmd(new SetPitchPointsCommand(part, note, pitch));
                    }
                }
                if (SetVibrato) {
                    foreach (UNote note in selectedNotes) {
                        if (!AutoVibratoToggle ||(AutoVibratoToggle && note.duration >= AutoVibratoNoteLength)) {
                            DocManager.Inst.ExecuteCmd(new VibratoLengthCommand(part, note, VibratoLength));
                            DocManager.Inst.ExecuteCmd(new VibratoFadeInCommand(part, note, VibratoIn));
                            DocManager.Inst.ExecuteCmd(new VibratoFadeOutCommand(part, note, VibratoOut));
                            DocManager.Inst.ExecuteCmd(new VibratoDepthCommand(part, note, VibratoDepth));
                            DocManager.Inst.ExecuteCmd(new VibratoPeriodCommand(part, note, VibratoPeriod));
                            DocManager.Inst.ExecuteCmd(new VibratoShiftCommand(part, note, VibratoShift));
                        }
                    }
                }

                DocManager.Inst.EndUndoGroup();
            }
        }
    }
}

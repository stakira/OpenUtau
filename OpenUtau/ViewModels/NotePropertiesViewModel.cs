using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Threading;
using OpenUtau.Core;
using OpenUtau.Core.Format;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SharpCompress;

namespace OpenUtau.App.ViewModels {
    public class NotePropertiesViewModel : ViewModelBase, ICmdSubscriber {
        [Reactive] public string Lyric { get; set; } = "a";
        [Reactive] public int PortamentoLength { get; set; }
        [Reactive] public int PortamentoStart { get; set; }
        [Reactive] public bool VibratoEnable { get; set; }
        [Reactive] public float VibratoLength { get; set; }
        [Reactive] public float VibratoPeriod { get; set; }
        [Reactive] public float VibratoDepth { get; set; }
        [Reactive] public float VibratoIn { get; set; }
        [Reactive] public float VibratoOut { get; set; }
        [Reactive] public float VibratoShift { get; set; }
        [Reactive] public float AutoVibratoNoteLength { get; set; }
        [Reactive] public bool AutoVibratoToggle { get; set; }
        [Reactive] public bool IsNoteSelected { get; set; } = false;

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

        private UVoicePart? part;
        private HashSet<UNote> selectedNotes = new HashSet<UNote>();
        public List<NotePropertyExpViewModel> Expressions = new List<NotePropertyExpViewModel>();

        public NotePropertiesViewModel() {
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

            MessageBus.Current.Listen<NotesSelectionEvent>()
                .Subscribe(e => {
                    selectedNotes.Clear();
                    selectedNotes.UnionWith(e.selectedNotes);
                    selectedNotes.UnionWith(e.tempSelectedNotes);
                    OnSelectNotes();
                });

            DocManager.Inst.AddSubscriber(this);
        }

        private void OnSelectNotes() {
            PortamentoLength = NotePresets.Default.DefaultPortamento.PortamentoLength;
            PortamentoStart = NotePresets.Default.DefaultPortamento.PortamentoStart;
            AutoVibratoNoteLength = NotePresets.Default.AutoVibratoNoteDuration;
            AutoVibratoToggle = NotePresets.Default.AutoVibratoToggle;

            if (selectedNotes.Count > 0) {
                IsNoteSelected = true;
                var note = selectedNotes.First();

                Lyric = note.lyric;
                VibratoEnable = note.vibrato.length == 0 ? false : true;
                VibratoLength = note.vibrato.length == 0 ? NotePresets.Default.DefaultVibrato.VibratoLength : note.vibrato.length;
                VibratoPeriod = note.vibrato.period;
                VibratoDepth = note.vibrato.depth;
                VibratoIn = note.vibrato.@in;
                VibratoOut = note.vibrato.@out;
                VibratoShift = note.vibrato.shift;
            } else {
                IsNoteSelected = false;
                Lyric = NotePresets.Default.DefaultLyric;
                VibratoLength = NotePresets.Default.DefaultVibrato.VibratoLength;
                VibratoPeriod = NotePresets.Default.DefaultVibrato.VibratoPeriod;
                VibratoDepth = NotePresets.Default.DefaultVibrato.VibratoDepth;
                VibratoIn = NotePresets.Default.DefaultVibrato.VibratoIn;
                VibratoOut = NotePresets.Default.DefaultVibrato.VibratoOut;
                VibratoShift = NotePresets.Default.DefaultVibrato.VibratoShift;
            }
            AttachExpressions();
        }

        public void LoadPart(UPart? part) {
            Expressions.Clear();
            if (part != null && part is UVoicePart) {
                this.part = part as UVoicePart;

                foreach (KeyValuePair<string, UExpressionDescriptor> pair in DocManager.Inst.Project.expressions) {
                    if (pair.Value.type != UExpressionType.Curve) {
                        var viewModel = new NotePropertyExpViewModel(pair.Value);
                        if (pair.Value.abbr == Ustx.CLR) {
                            var track = DocManager.Inst.Project.tracks[part.trackNo];
                            if (track.VoiceColorExp != null && track.VoiceColorExp.options.Length > 0) {
                                track.VoiceColorExp.options.ForEach(opt => viewModel.Options.Add(opt));
                            }
                        }
                        Expressions.Add(viewModel);
                    }
                }
                AttachExpressions();
            } else {
                this.part = null;
            }
        }

        private void AttachExpressions() {
            if (Expressions.Count > 0) {
                if (selectedNotes.Count > 0) {
                    var note = selectedNotes.First();

                    foreach (NotePropertyExpViewModel exp in Expressions) {
                        exp.IsNoteSelected = true;
                        var phonemeExpression = note.phonemeExpressions.FirstOrDefault(e => e.abbr == exp.abbr);
                        if (phonemeExpression != null) {
                            if (exp.IsNumerical) {
                                exp.Value = phonemeExpression.value;
                            } else if (exp.IsOptions) {
                                exp.SelectedOption = (int)phonemeExpression.value;
                            }
                        } else {
                            if (exp.IsNumerical) {
                                exp.Value = exp.defaultValue;
                            } else if (exp.IsOptions) {
                                exp.SelectedOption = (int)exp.defaultValue;
                            }
                        }
                    }
                } else {
                    foreach (NotePropertyExpViewModel exp in Expressions) {
                        exp.IsNoteSelected = false;
                        if (exp.IsNumerical) {
                            exp.Value = exp.defaultValue;
                        } else if (exp.IsOptions) {
                            exp.SelectedOption = (int)exp.defaultValue;
                        }
                    }
                }
            }
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

        #region ICmdSubscriber
        public void OnNext(UCommand cmd, bool isUndo) {
            var note = selectedNotes.FirstOrDefault();
            if (note == null) { return; }

            if (cmd is NoteCommand) {
                if (cmd is ChangeNoteLyricCommand changeNoteLyricCommand) {
                    if (changeNoteLyricCommand.Notes.Contains(note)) {
                        Lyric = note.lyric;
                    }
                } else if (cmd is VibratoLengthCommand vibratoLengthCommand) {
                    if (vibratoLengthCommand.Notes.Contains(note)) {
                        if (note.vibrato.length == 0) {
                            VibratoEnable = false;
                            VibratoLength = NotePresets.Default.DefaultVibrato.VibratoLength;
                        } else {
                            VibratoEnable = true;
                            VibratoLength = note.vibrato.length;
                        }
                    }
                } else if (cmd is VibratoFadeInCommand vibratoFadeInCommand) {
                    if (vibratoFadeInCommand.Notes.Contains(note)) {
                        VibratoIn = note.vibrato.@in;
                    }
                } else if (cmd is VibratoFadeOutCommand vibratoFadeOutCommand) {
                    if (vibratoFadeOutCommand.Notes.Contains(note)) {
                        VibratoOut = note.vibrato.@out;
                    }
                } else if (cmd is VibratoDepthCommand vibratoDepthCommand) {
                    if (vibratoDepthCommand.Notes.Contains(note)) {
                        VibratoDepth = note.vibrato.depth;
                    }
                } else if (cmd is VibratoPeriodCommand vibratoPeriodCommand) {
                    if (vibratoPeriodCommand.Notes.Contains(note)) {
                        VibratoPeriod = note.vibrato.period;
                    }
                } else if (cmd is VibratoShiftCommand vibratoShiftCommand) {
                    if (vibratoShiftCommand.Notes.Contains(note)) {
                        VibratoShift = note.vibrato.shift;
                    }
                }
            } else if (cmd is ExpCommand) {
                if (cmd is PitchExpCommand pitchExpCommand) {
                    // 
                } else if (cmd is SetPhonemeExpressionCommand || cmd is ResetExpressionsCommand) {
                    AttachExpressions();
                }
            }
        }
        #endregion

        /*public void Finish() {
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
                        if(VibratoEnable && VibratoLength != 0) {
                            if (!AutoVibratoToggle || (AutoVibratoToggle && note.duration >= AutoVibratoNoteLength)) {
                                DocManager.Inst.ExecuteCmd(new VibratoLengthCommand(part, note, VibratoLength));
                                DocManager.Inst.ExecuteCmd(new VibratoFadeInCommand(part, note, VibratoIn));
                                DocManager.Inst.ExecuteCmd(new VibratoFadeOutCommand(part, note, VibratoOut));
                                DocManager.Inst.ExecuteCmd(new VibratoDepthCommand(part, note, VibratoDepth));
                                DocManager.Inst.ExecuteCmd(new VibratoPeriodCommand(part, note, VibratoPeriod));
                                DocManager.Inst.ExecuteCmd(new VibratoShiftCommand(part, note, VibratoShift));
                            } else {
                                DocManager.Inst.ExecuteCmd(new VibratoLengthCommand(part, note, 0));
                            }
                        } else if (note.vibrato.length != 0) {
                            DocManager.Inst.ExecuteCmd(new VibratoLengthCommand(part, note, 0));
                        }
                    }
                }
                foreach (NotePropertyExpViewModel expVM in Expressions) {
                    if (expVM.Set) {
                        float value;
                        if (expVM.IsNumerical) {
                            value = expVM.Value;
                        } else if (expVM.IsOptions) {
                            value = expVM.SelectedOption;
                        } else {
                            continue;
                        }
                        var track = notesViewModel.Project.tracks[notesViewModel.Part.trackNo];
                        foreach (UNote note in selectedNotes) {
                            foreach (UPhoneme phoneme in notesViewModel.Part.phonemes) {
                                if (phoneme.Parent == note) {
                                    DocManager.Inst.ExecuteCmd(new SetPhonemeExpressionCommand(notesViewModel.Project, track, notesViewModel.Part, phoneme, expVM.abbr, value));
                                }
                            }
                        }
                    }
                }

                DocManager.Inst.EndUndoGroup();
            }
        }*/
    }

    public class NotePropertyExpViewModel : ViewModelBase {
        public string Name { get; set; }
        public bool IsNumerical { get; set; } = false;
        public bool IsOptions { get; set; } = false;
        public float Min { get; set; }
        public float Max { get; set; }
        public ObservableCollection<string> Options { get; set; } = new ObservableCollection<string>();
        public string abbr;
        public float defaultValue;

        [Reactive] public bool IsNoteSelected { get; set; } = false;
        [Reactive] public float Value { get; set; }
        [Reactive] public int SelectedOption { get; set; }

        public NotePropertyExpViewModel(UExpressionDescriptor descriptor) {
            Name = descriptor.name;
            defaultValue = descriptor.defaultValue;
            abbr = descriptor.abbr;
            if (descriptor.type == UExpressionType.Numerical) {
                IsNumerical = true;
                Max = descriptor.max;
                Min = descriptor.min;
                Value = defaultValue;
            } else if (descriptor.type == UExpressionType.Options) {
                IsOptions = true;
                descriptor.options.ForEach(opt => Options.Add(opt));
                SelectedOption = (int)defaultValue;
            }
        }
        public override string ToString() {
            return Name;
        }
    }
}

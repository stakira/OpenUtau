using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using DynamicData;
using Melanchall.DryWetMidi.MusicTheory;
using OpenUtau.Core;
using OpenUtau.Core.Format;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SharpCompress;
using YamlDotNet.Core.Tokens;
using static System.Windows.Forms.Design.AxImporter;

namespace OpenUtau.App.ViewModels {
    public class NotePropertyViewModel : ViewModelBase {

        [Reactive] public bool SetLyric { get; set; } = false;
        [Reactive] public string Lyric { get; set; } = "a";
        [Reactive] public bool SetPortamento { get; set; } = false;
        [Reactive] public int PortamentoLength { get; set; }
        [Reactive] public int PortamentoStart { get; set; }
        [Reactive] public bool SetVibrato { get; set; } = false;
        [Reactive] public bool VibratoEnable { get; set; } = true;
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

        public List<NotePropertyExpViewModel> Expressions = new List<NotePropertyExpViewModel>();

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
            
            VibratoLength = note.vibrato.length == 0 ? NotePresets.Default.DefaultVibrato.VibratoLength : note.vibrato.length;
            VibratoPeriod = note.vibrato.period;
            VibratoDepth = note.vibrato.depth;
            VibratoIn = note.vibrato.@in;
            VibratoOut = note.vibrato.@out;
            VibratoShift = note.vibrato.shift;

            foreach(KeyValuePair<string, UExpressionDescriptor> pair in DocManager.Inst.Project.expressions) {
                var viewModel = new NotePropertyExpViewModel(pair.Value);
                var phonemeExpression = note.phonemeExpressions.FirstOrDefault(e => e.abbr == pair.Value.abbr);
                if (phonemeExpression != null) {
                    if (viewModel.IsNumerical) {
                        viewModel.Value = phonemeExpression.value;
                    } else if (viewModel.IsOptions) {
                        viewModel.SelectedOption = (int)phonemeExpression.value;
                    }
                }
                if (pair.Value.abbr == Ustx.CLR) {
                    var track = DocManager.Inst.Project.tracks[notesViewModel.Part!.trackNo];
                    track.VoiceColorExp.options.ForEach(opt => viewModel.Options.Add(opt));
                }
                Expressions.Add(viewModel);
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
                        if(VibratoEnable && VibratoLength != 0) {
                            if (!AutoVibratoToggle || (AutoVibratoToggle && note.duration >= AutoVibratoNoteLength)) {
                                DocManager.Inst.ExecuteCmd(new VibratoLengthCommand(part, note, VibratoLength));
                                DocManager.Inst.ExecuteCmd(new VibratoFadeInCommand(part, note, VibratoIn));
                                DocManager.Inst.ExecuteCmd(new VibratoFadeOutCommand(part, note, VibratoOut));
                                DocManager.Inst.ExecuteCmd(new VibratoDepthCommand(part, note, VibratoDepth));
                                DocManager.Inst.ExecuteCmd(new VibratoPeriodCommand(part, note, VibratoPeriod));
                                DocManager.Inst.ExecuteCmd(new VibratoShiftCommand(part, note, VibratoShift));
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
                                    if (expVM.isCurve) {
                                        int start = (int)Math.Floor(phoneme.position - phoneme.preutter);
                                        int end = (int)Math.Ceiling(phoneme.End - phoneme.tailIntrude + phoneme.tailOverlap);
                                        int valueInt = (int)Math.Round(value);
                                        DocManager.Inst.ExecuteCmd(new SetCurveCommand(notesViewModel.Project, notesViewModel.Part, expVM.abbr,
                                            start, valueInt,
                                            start, valueInt));
                                        DocManager.Inst.ExecuteCmd(new SetCurveCommand(notesViewModel.Project, notesViewModel.Part, expVM.abbr,
                                            end, valueInt,
                                            end, valueInt));
                                        DocManager.Inst.ExecuteCmd(new SetCurveCommand(notesViewModel.Project, notesViewModel.Part, expVM.abbr,
                                            start, valueInt,
                                            end, valueInt));
                                    } else {
                                        DocManager.Inst.ExecuteCmd(new SetPhonemeExpressionCommand(notesViewModel.Project, track, notesViewModel.Part, phoneme, expVM.abbr, value));
                                    }
                                }
                            }
                        }
                    }
                }

                DocManager.Inst.EndUndoGroup();
            }
        }
    }

    public class NotePropertyExpViewModel : ViewModelBase {
        public string Name { get; set; }
        public bool IsNumerical { get; set; } = false;
        public bool IsOptions { get; set; } = false;
        public float Min { get; set; }
        public float Max { get; set; }
        public ObservableCollection<string> Options { get; set; } = new ObservableCollection<string>();
        public bool isCurve = false;
        public string abbr;

        [Reactive] public bool Set { get; set; } = false;
        [Reactive] public float Value { get; set; }
        [Reactive] public int SelectedOption { get; set; }

        public NotePropertyExpViewModel(UExpressionDescriptor descriptor) {
            Name = descriptor.name;
            Value = descriptor.defaultValue;
            abbr = descriptor.abbr;
            if (descriptor.type == UExpressionType.Numerical) {
                IsNumerical = true;
                Max = descriptor.max;
                Min = descriptor.min;
            } else if (descriptor.type == UExpressionType.Curve) {
                IsNumerical = true;
                Max = descriptor.max;
                Min = descriptor.min;
                isCurve = true;
            } else if (descriptor.type == UExpressionType.Options) {
                IsOptions = true;
                descriptor.options.ForEach(opt => Options.Add(opt));
            }
        }
        public override string ToString() {
            return Name;
        }
    }
}

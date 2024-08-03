using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;

namespace OpenUtau.Core.Editing {
    public class ResetAllParameters : BatchEdit {
        public virtual string Name => name;

        private string name;

        public ResetAllParameters() {
            name = "pianoroll.menu.notes.reset.allparameters";
        }

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            docManager.StartUndoGroup(true);
            foreach (var note in notes) {
                // pitch points
                docManager.ExecuteCmd(new ResetPitchPointsCommand(part, note));
                // expressions
                if (note.phonemeExpressions.Count > 0) {
                    docManager.ExecuteCmd(new ResetExpressionsCommand(part, note));
                }
                // vibrato
                if (note.vibrato.length > 0) {
                    docManager.ExecuteCmd(new VibratoLengthCommand(part, note, 0));
                }
                // timings
                bool shouldClear = false;
                foreach (var o in note.phonemeOverrides) {
                    if (o.offset != null || o.preutterDelta != null || o.overlapDelta != null) {
                        shouldClear = true;
                        break;
                    }
                }
                if (shouldClear) {
                    docManager.ExecuteCmd(new ClearPhonemeTimingCommand(part, note));
                }
                // aliases
                foreach (var o in note.phonemeOverrides) {
                    if (o.phoneme != null) {
                        docManager.ExecuteCmd(new ChangePhonemeAliasCommand(part, note, o.index, null));
                    }
                }
            }
            docManager.EndUndoGroup();
        }
    }

    public class ResetPitchBends : BatchEdit {
        public virtual string Name => name;

        private string name;

        public ResetPitchBends() {
            name = "pianoroll.menu.notes.reset.pitchbends";
        }

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            docManager.StartUndoGroup(true);
            foreach (var note in notes) {
                docManager.ExecuteCmd(new ResetPitchPointsCommand(part, note));
            }
            docManager.EndUndoGroup();
        }
    }

    public class ResetAllExpressions : BatchEdit {
        public virtual string Name => name;

        private string name;

        public ResetAllExpressions() {
            name = "pianoroll.menu.notes.reset.exps";
        }

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            docManager.StartUndoGroup(true);
            foreach (var note in notes) {
                if (note.phonemeExpressions.Count > 0) {
                    docManager.ExecuteCmd(new ResetExpressionsCommand(part, note));
                }
            }
            var curveAbbrs = part.curves.Select(c => c.abbr).ToArray();
            foreach (var abbr in curveAbbrs) {
                docManager.ExecuteCmd(new ClearCurveCommand(part, abbr));
            }
            docManager.EndUndoGroup();
        }
    }

    public class ClearVibratos : BatchEdit {
        public virtual string Name => name;

        private string name;

        public ClearVibratos() {
            name = "pianoroll.menu.notes.clear.vibratos";
        }

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            docManager.StartUndoGroup(true);
            foreach (var note in notes) {
                if (note.vibrato.length > 0) {
                    docManager.ExecuteCmd(new VibratoLengthCommand(part, note, 0));
                }
            }
            docManager.EndUndoGroup();
        }
    }

    public class ResetVibratos : BatchEdit {
        public virtual string Name => name;

        private string name;

        public ResetVibratos() {
            name = "pianoroll.menu.notes.reset.vibratos";
        }

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            docManager.StartUndoGroup(true);
            foreach (var note in notes) {
                docManager.ExecuteCmd(new VibratoPeriodCommand(part, note, NotePresets.Default.DefaultVibrato.VibratoPeriod));
                docManager.ExecuteCmd(new VibratoDepthCommand(part, note, NotePresets.Default.DefaultVibrato.VibratoDepth));
                docManager.ExecuteCmd(new VibratoFadeInCommand(part, note, NotePresets.Default.DefaultVibrato.VibratoIn));
                docManager.ExecuteCmd(new VibratoFadeOutCommand(part, note, NotePresets.Default.DefaultVibrato.VibratoOut));
                docManager.ExecuteCmd(new VibratoShiftCommand(part, note, NotePresets.Default.DefaultVibrato.VibratoShift));
                docManager.ExecuteCmd(new VibratoDriftCommand(part, note, NotePresets.Default.DefaultVibrato.VibratoDrift));
                if (NotePresets.Default.AutoVibratoToggle && note.duration >= NotePresets.Default.AutoVibratoNoteDuration) {
                    docManager.ExecuteCmd(new VibratoLengthCommand(part, note, NotePresets.Default.DefaultVibrato.VibratoLength));
                } else {
                    docManager.ExecuteCmd(new VibratoLengthCommand(part, note, 0));
                }
            }
            docManager.EndUndoGroup();
        }
    }

    public class ClearTimings : BatchEdit {
        public virtual string Name => name;

        private string name;

        public ClearTimings() {
            name = "pianoroll.menu.notes.reset.phonemetimings";
        }

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            docManager.StartUndoGroup(true);
            foreach (var note in notes) {
                bool shouldClear = false;
                foreach (var o in note.phonemeOverrides) {
                    if (o.offset != null || o.preutterDelta != null || o.overlapDelta != null) {
                        shouldClear = true;
                        break;
                    }
                }
                if (shouldClear) {
                    docManager.ExecuteCmd(new ClearPhonemeTimingCommand(part, note));
                }
            }
            docManager.EndUndoGroup();
        }
    }

    public class ResetAliases : BatchEdit {
        public virtual string Name => name;

        private string name;

        public ResetAliases() {
            name = "pianoroll.menu.notes.reset.aliases";
        }

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            docManager.StartUndoGroup(true);
            foreach (var note in notes) {
                foreach (var o in note.phonemeOverrides) {
                    if (o.phoneme != null) {
                        docManager.ExecuteCmd(new ChangePhonemeAliasCommand(part, note, o.index, null));
                    }
                }
            }
            docManager.EndUndoGroup();
        }
    }

    public class ResetAll : BatchEdit {
        public virtual string Name => name;

        private string name;

        public ResetAll() {
            name = "pianoroll.menu.notes.reset.all";
        }

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            docManager.StartUndoGroup(true);
            foreach (var note in notes) {
                // pitch points
                docManager.ExecuteCmd(new ResetPitchPointsCommand(part, note));
                // expressions
                if (note.phonemeExpressions.Count > 0) {
                    docManager.ExecuteCmd(new ResetExpressionsCommand(part, note));
                }
                // vibrato
                if (note.vibrato.length > 0) {
                    docManager.ExecuteCmd(new VibratoLengthCommand(part, note, 0));
                }
                // timings
                bool shouldClear = false;
                foreach (var o in note.phonemeOverrides) {
                    if (o.offset != null || o.preutterDelta != null || o.overlapDelta != null) {
                        shouldClear = true;
                        break;
                    }
                }
                if (shouldClear) {
                    docManager.ExecuteCmd(new ClearPhonemeTimingCommand(part, note));
                }
                // aliases
                foreach (var o in note.phonemeOverrides) {
                    if (o.phoneme != null) {
                        docManager.ExecuteCmd(new ChangePhonemeAliasCommand(part, note, o.index, null));
                    }
                }
            }
            docManager.EndUndoGroup();
        }
    }
}

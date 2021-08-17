using System.Collections.Generic;
using System.Linq;

using OpenUtau.Core.Ustx;

namespace OpenUtau.Core {
    public abstract class NoteCommand : UCommand {
        protected readonly UNote[] Notes;
        public readonly UVoicePart Part;
        public NoteCommand(UVoicePart part, UNote note) {
            Part = part;
            Notes = new UNote[] { note };
        }
        public NoteCommand(UVoicePart part, IEnumerable<UNote> notes) {
            Part = part;
            Notes = notes.ToArray();
        }
    }

    public class AddNoteCommand : NoteCommand {
        public AddNoteCommand(UVoicePart part, UNote note) : base(part, note) { }
        public AddNoteCommand(UVoicePart part, List<UNote> notes) : base(part, notes) { }
        public override string ToString() { return "Add note"; }
        public override void Execute() {
            lock (Part) {
                foreach (var note in Notes) {
                    Part.notes.Add(note);
                }
            }
        }
        public override void Unexecute() {
            lock (Part) {
                foreach (var note in Notes) {
                    Part.notes.Remove(note);
                }
            }
        }
    }

    public class RemoveNoteCommand : NoteCommand {
        public RemoveNoteCommand(UVoicePart part, UNote note) : base(part, note) { }
        public RemoveNoteCommand(UVoicePart part, List<UNote> notes) : base(part, notes) { }
        public override string ToString() { return "Remove note"; }
        public override void Execute() {
            lock (Part) {
                foreach (var note in Notes) {
                    Part.notes.Remove(note);
                }
            }
        }
        public override void Unexecute() {
            lock (Part) {
                foreach (var note in Notes) {
                    Part.notes.Add(note);
                }
            }
        }
    }

    public class MoveNoteCommand : NoteCommand {
        readonly int DeltaPos, DeltaNoteNum;
        public MoveNoteCommand(UVoicePart part, UNote note, int deltaPos, int deltaNoteNum) : base(part, note) {
            DeltaPos = deltaPos;
            DeltaNoteNum = deltaNoteNum;
        }
        public MoveNoteCommand(UVoicePart part, List<UNote> notes, int deltaPos, int deltaNoteNum) : base(part, notes) {
            DeltaPos = deltaPos;
            DeltaNoteNum = deltaNoteNum;
        }
        public override string ToString() { return $"Move {Notes.Count()} notes"; }
        public override void Execute() {
            lock (Part) {
                foreach (UNote note in Notes) {
                    Part.notes.Remove(note);
                    note.position += DeltaPos;
                    note.noteNum += DeltaNoteNum;
                    Part.notes.Add(note);
                }
            }
        }
        public override void Unexecute() {
            lock (Part) {
                foreach (UNote note in Notes) {
                    Part.notes.Remove(note);
                    note.position -= DeltaPos;
                    note.noteNum -= DeltaNoteNum;
                    Part.notes.Add(note);
                }
            }
        }
    }

    public class ResizeNoteCommand : NoteCommand {
        readonly int DeltaDur;
        public ResizeNoteCommand(UVoicePart part, UNote note, int deltaDur) : base(part, note) {
            DeltaDur = deltaDur;
        }
        public ResizeNoteCommand(UVoicePart part, List<UNote> notes, int deltaDur) : base(part, notes) {
            DeltaDur = deltaDur;
        }
        public override string ToString() { return $"Change {Notes.Count()} notes duration"; }
        public override void Execute() {
            lock (Part) {
                foreach (var note in Notes) {
                    note.duration += DeltaDur;
                }
            }
        }
        public override void Unexecute() {
            lock (Part) {
                foreach (var note in Notes) {
                    note.duration -= DeltaDur;
                }
            }
        }
    }

    public class ChangeNoteLyricCommand : NoteCommand {
        public UNote Note;
        readonly string NewLyric;
        readonly string OldLyric;
        public ChangeNoteLyricCommand(UVoicePart part, UNote note, string newLyric) : base(part, note) {
            Note = note;
            NewLyric = newLyric;
            OldLyric = note.lyric;
        }
        public override string ToString() {
            return "Change notes lyric";
        }
        public override void Execute() {
            lock (Part) {
                Note.lyric = NewLyric;
                Note.phonemes[0].phoneme = NewLyric;
            }
        }
        public override void Unexecute() {
            lock (Part) {
                Note.lyric = OldLyric;
                Note.phonemes[0].phoneme = OldLyric;
            }
        }
    }

    public class VibratoLengthCommand : NoteCommand {
        readonly UNote note;
        readonly float newLength;
        readonly float oldLength;
        public VibratoLengthCommand(UVoicePart part, UNote note, float length) : base(part, note) {
            this.note = note;
            newLength = length;
            oldLength = note.vibrato.length;
        }
        public override string ToString() {
            return "Change vibrato length";
        }
        public override void Execute() {
            lock (Part) {
                note.vibrato.length = newLength;
            }
        }
        public override void Unexecute() {
            lock (Part) {
                note.vibrato.length = oldLength;
            }
        }
    }

    public class VibratoFadeInCommand : NoteCommand {
        readonly UNote note;
        readonly float newFadeIn;
        readonly float oldFadeIn;
        public VibratoFadeInCommand(UVoicePart part, UNote note, float fadeIn) : base(part, note) {
            this.note = note;
            newFadeIn = fadeIn;
            oldFadeIn = note.vibrato.@in;

        }
        public override string ToString() {
            return "Change vibrato fade in";
        }
        public override void Execute() {
            lock (Part) {
                note.vibrato.@in = newFadeIn;
            }
        }
        public override void Unexecute() {
            lock (Part) {
                note.vibrato.@in = oldFadeIn;
            }
        }
    }

    public class VibratoFadeOutCommand : NoteCommand {
        readonly UNote note;
        readonly float newFadeOut;
        readonly float oldFadeOut;
        public VibratoFadeOutCommand(UVoicePart part, UNote note, float fadeOut) : base(part, note) {
            this.note = note;
            newFadeOut = fadeOut;
            oldFadeOut = note.vibrato.@out;
        }
        public override string ToString() {
            return "Change vibrato fade out";
        }
        public override void Execute() {
            lock (Part) {
                note.vibrato.@out = newFadeOut;
            }
        }
        public override void Unexecute() {
            lock (Part) {
                note.vibrato.@out = oldFadeOut;
            }
        }
    }

    public class VibratoDepthCommand : NoteCommand {
        readonly UNote note;
        readonly float newDepth;
        readonly float oldDepth;
        public VibratoDepthCommand(UVoicePart part, UNote note, float depth) : base(part, note) {
            this.note = note;
            newDepth = depth;
            oldDepth = note.vibrato.depth;
        }
        public override string ToString() {
            return "Change vibrato depth";
        }
        public override void Execute() {
            lock (Part) {
                note.vibrato.depth = newDepth;
            }
        }
        public override void Unexecute() {
            lock (Part) {
                note.vibrato.depth = oldDepth;
            }
        }
    }

    public class VibratoPeriodCommand : NoteCommand {
        readonly UNote note;
        readonly float newPeriod;
        readonly float oldPeriod;
        public VibratoPeriodCommand(UVoicePart part, UNote note, float period) : base(part, note) {
            this.note = note;
            newPeriod = period;
            oldPeriod = note.vibrato.period;
        }
        public override string ToString() {
            return "Change vibrato period";
        }
        public override void Execute() {
            lock (Part) {
                note.vibrato.period = newPeriod;
            }
        }
        public override void Unexecute() {
            lock (Part) {
                note.vibrato.period = oldPeriod;
            }
        }
    }

    public class VibratoShiftCommand : NoteCommand {
        readonly UNote note;
        readonly float newShift;
        readonly float oldShift;
        public VibratoShiftCommand(UVoicePart part, UNote note, float shift) : base(part, note) {
            this.note = note;
            newShift = shift;
            oldShift = note.vibrato.shift;
        }
        public override string ToString() {
            return "Change vibrato shift";
        }
        public override void Execute() {
            lock (Part) {
                note.vibrato.shift = newShift;
            }
        }
        public override void Unexecute() {
            lock (Part) {
                note.vibrato.shift = oldShift;
            }
        }
    }
}

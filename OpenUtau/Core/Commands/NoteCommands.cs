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
        readonly string NewLyric, OldLyric;

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
}

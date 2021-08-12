using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenUtau.Core.Ustx;

namespace OpenUtau.Core {
    public abstract class NoteCommand : UCommand {
        protected UNote[] Notes;
        public UVoicePart Part;
    }

    public class AddNoteCommand : NoteCommand {
        public AddNoteCommand(UVoicePart part, UNote note) { this.Part = part; this.Notes = new UNote[] { note }; }
        public AddNoteCommand(UVoicePart part, List<UNote> notes) { this.Part = part; this.Notes = notes.ToArray(); }
        public override string ToString() { return "Add note"; }
        public override void Execute() { lock (Part) { foreach (var note in Notes) Part.notes.Add(note); } }
        public override void Unexecute() { lock (Part) { foreach (var note in Notes) Part.notes.Remove(note); } }
    }

    public class RemoveNoteCommand : NoteCommand {
        public RemoveNoteCommand(UVoicePart part, UNote note) { this.Part = part; this.Notes = new UNote[] { note }; }
        public RemoveNoteCommand(UVoicePart part, List<UNote> notes) { this.Part = part; this.Notes = notes.ToArray(); }
        public override string ToString() { return "Remove note"; }
        public override void Execute() { lock (Part) { foreach (var note in Notes) Part.notes.Remove(note); } }
        public override void Unexecute() { lock (Part) { foreach (var note in Notes) Part.notes.Add(note); } }
    }

    public class MoveNoteCommand : NoteCommand {
        readonly int DeltaPos, DeltaNoteNum;
        public MoveNoteCommand(UVoicePart part, List<UNote> notes, int deltaPos, int deltaNoteNum) {
            this.Part = part;
            this.Notes = notes.ToArray();
            this.DeltaPos = deltaPos;
            this.DeltaNoteNum = deltaNoteNum;
        }
        public MoveNoteCommand(UVoicePart part, UNote note, int deltaPos, int deltaNoteNum) {
            this.Part = part;
            this.Notes = new UNote[] { note };
            this.DeltaPos = deltaPos;
            this.DeltaNoteNum = deltaNoteNum;
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
        public ResizeNoteCommand(UVoicePart part, List<UNote> notes, int deltaDur) { this.Part = part; this.Notes = notes.ToArray(); this.DeltaDur = deltaDur; }
        public ResizeNoteCommand(UVoicePart part, UNote note, int deltaDur) { this.Part = part; this.Notes = new UNote[] { note }; this.DeltaDur = deltaDur; }
        public override string ToString() { return $"Change {Notes.Count()} notes duration"; }
        public override void Execute() { lock (Part) { foreach (var note in Notes) note.duration += DeltaDur; } }
        public override void Unexecute() { lock (Part) { foreach (var note in Notes) note.duration -= DeltaDur; } }
    }

    public class ChangeNoteLyricCommand : NoteCommand {
        public UNote Note;
        readonly string NewLyric, OldLyric;

        public ChangeNoteLyricCommand(UVoicePart part, UNote note, string newLyric) {
            Part = part;
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

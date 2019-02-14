using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenUtau.Core.USTx;

namespace OpenUtau.Core
{
    public abstract class NoteCommand : UCommand 
    {
        protected UNote[] Notes;
        public UVoicePart Part;
    }

    public class AddNoteCommand : NoteCommand
    {
        public AddNoteCommand(UVoicePart part, UNote note) { this.Part = part; this.Notes = new UNote[] { note }; }
        public AddNoteCommand(UVoicePart part, List<UNote> notes) { this.Part = part; this.Notes = notes.ToArray(); }
        public override string ToString() { return "Add note"; }
        public override void Execute() { lock (Part) { foreach (var note in Notes) Part.Notes.Add(note); } }
        public override void Unexecute() { lock (Part) { foreach (var note in Notes) Part.Notes.Remove(note); } }
    }

    public class RemoveNoteCommand : NoteCommand
    {
        public RemoveNoteCommand(UVoicePart part, UNote note) { this.Part = part; this.Notes = new UNote[] { note }; }
        public RemoveNoteCommand(UVoicePart part, List<UNote> notes) { this.Part = part; this.Notes = notes.ToArray(); }
        public override string ToString() { return "Remove note"; }
        public override void Execute() { lock (Part) { foreach (var note in Notes) Part.Notes.Remove(note); } }
        public override void Unexecute() { lock (Part) { foreach (var note in Notes) Part.Notes.Add(note); } }
    }

    public class MoveNoteCommand : NoteCommand
    {
        int DeltaPos, DeltaNoteNum;
        public MoveNoteCommand(UVoicePart part, List<UNote> notes, int deltaPos, int deltaNoteNum)
        {
            this.Part = part;
            this.Notes = notes.ToArray();
            this.DeltaPos = deltaPos;
            this.DeltaNoteNum = deltaNoteNum;
        }
        public MoveNoteCommand(UVoicePart part, UNote note, int deltaPos, int deltaNoteNum)
        {
            this.Part = part;
            this.Notes = new UNote[] { note };
            this.DeltaPos = deltaPos;
            this.DeltaNoteNum = deltaNoteNum;
        }
        public override string ToString() { return $"Move {Notes.Count()} notes"; }
        public override void Execute() {
            lock (Part)
            {
                foreach (UNote note in Notes)
                {
                    Part.Notes.Remove(note);
                    note.PosTick += DeltaPos;
                    note.NoteNum += DeltaNoteNum;
                    Part.Notes.Add(note);
                }
            }
        }
        public override void Unexecute()
        {
            lock (Part)
            {
                foreach (UNote note in Notes)
                {
                    Part.Notes.Remove(note);
                    note.PosTick -= DeltaPos;
                    note.NoteNum -= DeltaNoteNum;
                    Part.Notes.Add(note);
                }
            }
        }
    }

    public class ResizeNoteCommand : NoteCommand
    {
        int DeltaDur;
        public ResizeNoteCommand(UVoicePart part, List<UNote> notes, int deltaDur) { this.Part = part; this.Notes = notes.ToArray(); this.DeltaDur = deltaDur; }
        public ResizeNoteCommand(UVoicePart part, UNote note, int deltaDur) { this.Part = part; this.Notes = new UNote[] { note }; this.DeltaDur = deltaDur; }
        public override string ToString() { return $"Change {Notes.Count()} notes duration"; }
        public override void Execute() { lock (Part) { foreach (var note in Notes) note.DurTick += DeltaDur; } }
        public override void Unexecute() { lock (Part) { foreach (var note in Notes) note.DurTick -= DeltaDur; } }
    }

    public class ChangeNoteLyricCommand : NoteCommand
    {
        public UNote Note;
        string NewLyric, OldLyric;
        public ChangeNoteLyricCommand(UVoicePart part, UNote note, string newLyric) { this.Part = part; this.Note = note; this.NewLyric = newLyric; this.OldLyric = note.Lyric; }
        public override string ToString() { return "Change notes lyric"; }
        public override void Execute() { lock (Part) { Note.Lyric = NewLyric; } }
        public override void Unexecute() { lock (Part) { Note.Lyric = OldLyric; } }
    }
}

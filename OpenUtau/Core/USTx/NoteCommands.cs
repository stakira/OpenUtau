using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.USTx
{
    public abstract class NoteCommand : UCommand 
    {
        public UVoicePart Part;
        public UNote Note;
    }

    public class AddNoteCommand : NoteCommand
    {
        public AddNoteCommand(UVoicePart part, UNote note) { this.Part = part; this.Note = note; }
        public override string ToString() { return "Add note"; }
        public override void Execute() { Part.Notes.Add(Note); }
        public override void Unexecute() { Part.Notes.Remove(Note); }
    }

    public class RemoveNoteCommand : NoteCommand
    {
        public RemoveNoteCommand(UVoicePart part, UNote note) { this.Part = part; this.Note = note; }
        public override string ToString() { return "Remove notes"; }
        public override void Execute() { Part.Notes.Remove(Note); }
        public override void Unexecute() { Part.Notes.Add(Note); }
    }

    public class MoveNoteCommand : NoteCommand
    {
        int NewPos, OldPos, NewNoteNum, OldNoteNum;
        public MoveNoteCommand(UVoicePart part, UNote note, int newPos, int newNoteNum)
        {
            this.Part = part;
            this.Note = note;
            this.NewPos = newPos;
            this.NewNoteNum = newNoteNum;
            this.OldPos = note.PosTick;
            this.OldNoteNum = note.NoteNum;
        }
        public override string ToString() { return "Move notes"; }
        public override void Execute() { Note.PosTick = NewPos; Note.NoteNum = NewNoteNum; }
        public override void Unexecute() { Note.PosTick = OldPos; Note.NoteNum = OldNoteNum; }
    }

    public class ResizeNoteCommand : NoteCommand
    {
        int NewDur, OldDur;
        public ResizeNoteCommand(UVoicePart part, UNote note, int newDur) { this.Part = part; this.Note = note; this.NewDur = newDur; this.OldDur = note.DurTick; }
        public override string ToString() { return "Change notes duration"; }
        public override void Execute() { Note.DurTick = NewDur; }
        public override void Unexecute() { Note.DurTick = OldDur; }
    }

    public class ChangeNoteLyricCommand : NoteCommand
    {
        string NewLyric, OldLyric;
        public ChangeNoteLyricCommand(UVoicePart part, UNote note, string newLyric) { this.Part = part; this.Note = note; this.NewLyric = newLyric; this.OldLyric = note.Lyric; }
        public override string ToString() { return "Change notes lyric"; }
        public override void Execute() { Note.Lyric = NewLyric; }
        public override void Unexecute() { Note.Lyric = OldLyric; }
    }
}

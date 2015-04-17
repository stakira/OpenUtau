using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.USTx
{
    public abstract class NoteCommand : UCommand 
    {
        public UVoicePart part;
        public UNote note;
    }

    public class AddNoteCommand : NoteCommand
    {
        public AddNoteCommand(UVoicePart part, UNote note) { this.part = part; this.note = note; }
        public override string ToString() { return "Add note"; }
        public override void Execute() { part.Notes.Add(note); }
        public override void Unexecute() { part.Notes.Remove(note); }
    }

    public class RemoveNoteCommand : NoteCommand
    {
        public RemoveNoteCommand(UVoicePart part, UNote note) { this.part = part; this.note = note; }
        public override string ToString() { return "Remove notes"; }
        public override void Execute() { part.Notes.Remove(note); }
        public override void Unexecute() { part.Notes.Add(note); }
    }

    public class MoveNoteCommand : NoteCommand
    {
        int newPos, oldPos, newNoteNum, oldNoteNum;
        public MoveNoteCommand(UVoicePart part, UNote note, int newPos, int newNoteNum)
        {
            this.part = part;
            this.note = note;
            this.newPos = newPos;
            this.newNoteNum = newNoteNum;
            this.oldPos = note.PosTick;
            this.oldNoteNum = note.NoteNum;
        }
        public override string ToString() { return "Move notes"; }
        public override void Execute() { note.PosTick = newPos; note.NoteNum = newNoteNum; }
        public override void Unexecute() { note.PosTick = oldPos; note.NoteNum = oldNoteNum; }
    }

    public class ResizeNoteCommand : NoteCommand
    {
        int newDur, oldDur;
        public ResizeNoteCommand(UVoicePart part, UNote note, int newDur) { this.part = part; this.note = note; this.newDur = newDur; this.oldDur = note.DurTick; }
        public override string ToString() { return "Change notes duration"; }
        public override void Execute() { note.DurTick = newDur; }
        public override void Unexecute() { note.DurTick = oldDur; }
    }

    public class ChangeNoteLyricCommand : NoteCommand
    {
        string newLyric, oldLyric;
        public ChangeNoteLyricCommand(UVoicePart part, UNote note, string newLyric) { this.part = part; this.note = note; this.newLyric = newLyric; this.oldLyric = note.Lyric; }
        public override string ToString() { return "Change notes lyric"; }
        public override void Execute() { note.Lyric = newLyric; }
        public override void Unexecute() { note.Lyric = oldLyric; }
    }
}

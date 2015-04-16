using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.USTx
{
    public class AddNoteCommand : UCommand
    {
        UVoicePart target;
        UNote note;
        public AddNoteCommand(UVoicePart target, UNote note) { this.target = target; this.note = note; }
        public override string ToString() { return "Add note"; }
        public override void Execute() { target.Notes.Add(note); }
        public override void Unexecute() { target.Notes.Remove(note); }
    }

    public class RemoveNoteCommand : UCommand
    {
        UVoicePart target;
        UNote note;
        public RemoveNoteCommand(UVoicePart target, UNote note) { this.target = target; this.note = note; }
        public override string ToString() { return "Remove notes"; }
        public override void Execute() { target.Notes.Remove(note); }
        public override void Unexecute() { target.Notes.Add(note); }
    }

    public class MoveNoteCommand : UCommand
    {
        UNote target;
        int newPos, oldPos, newNoteNum, oldNoteNum;
        public MoveNoteCommand(UNote target, int newPos, int newNoteNum)
        {
            this.target = target;
            this.newPos = newPos;
            this.newNoteNum = newNoteNum;
            this.oldPos = target.PosTick;
            this.oldNoteNum = target.NoteNum;
        }
        public override string ToString() { return "Move notes"; }
        public override void Execute() { target.PosTick = newPos; target.NoteNum = newNoteNum; }
        public override void Unexecute() { target.PosTick = oldPos; target.NoteNum = oldNoteNum; }
    }

    public class ResizeNoteCommand : UCommand
    {
        UNote target;
        int newDur, oldDur;
        public ResizeNoteCommand(UNote target, int newDur) { this.target = target; this.newDur = newDur; this.oldDur = target.DurTick; }
        public override string ToString() { return "Change notes duration"; }
        public override void Execute() { target.DurTick = newDur; }
        public override void Unexecute() { target.DurTick = oldDur; }
    }

    public class ChangeNoteLyricCommand : UCommand
    {
        UNote target;
        string newLyric, oldLyric;
        public ChangeNoteLyricCommand(UNote target, string newLyric) { this.target = target; this.newLyric = newLyric; this.oldLyric = target.Lyric; }
        public override string ToString() { return "Change notes lyric"; }
        public override void Execute() { target.Lyric = newLyric; }
        public override void Unexecute() { target.Lyric = oldLyric; }
    }
}

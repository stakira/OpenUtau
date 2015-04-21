using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.USTx
{
    public abstract class ExpCommand : UCommand
    {
        public UVoicePart Part;
        public UNote Note;
        public string Key;
    }

    public class SetFloatExpCommand : ExpCommand
    {
        public float NewValue, OldValue;
        public SetFloatExpCommand(UVoicePart part, UNote note, string key, float newValue)
        {
            this.Part = part;
            this.Note = note;
            this.Key = key;
            this.NewValue = newValue;
            this.OldValue = (float)Note.Expressions[Key].Data;
        }
        public override string ToString() { return "Set note expression " + Key; }
        public override void Execute() { Note.Expressions[Key].Data = NewValue; }
        public override void Unexecute() { Note.Expressions[Key].Data = OldValue; }
    }
}

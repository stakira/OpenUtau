using System.Linq;

using OpenUtau.Core.Ustx;

namespace OpenUtau.Core {
    public abstract class ExpCommand : UCommand {
        public UVoicePart Part;
        public UNote Note;
        public string Key;
    }

    /*
    public class SetNoteExpressionCommand : ExpCommand {
        public readonly UProject project;
        public readonly UPhoneme phoneme;
        public readonly float newValue;
        public readonly float oldValue;
        public SetNoteExpressionCommand(UProject project, UNote note, string abbr, float value) {
            this.project = project;
            this.Note = note;
            Key = abbr;
            newValue = value;
            oldValue = phoneme.GetExpression(project, abbr).Item1;
        }
        public override string ToString() => $"Set note expression {Key}";
        public override void Execute() => Note.SetExpression(project, Key, newValue);
        public override void Unexecute() => Note.SetExpression(project, Key, oldValue);
    }
    */

    public class SetPhonemeExpressionCommand : ExpCommand {
        public readonly UProject project;
        public readonly UPhoneme phoneme;
        public readonly float newValue;
        public readonly float oldValue;
        public SetPhonemeExpressionCommand(UProject project, UPhoneme phoneme, string abbr, float value) {
            this.project = project;
            this.phoneme = phoneme;
            Key = abbr;
            newValue = value;
            oldValue = phoneme.GetExpression(project, abbr).Item1;
        }
        public override string ToString() => $"Set phoneme expression {Key}";
        public override void Execute() => phoneme.SetExpression(project, Key, newValue);
        public override void Unexecute() => phoneme.SetExpression(project, Key, oldValue);
    }

    public abstract class PitchExpCommand : ExpCommand { }

    public class DeletePitchPointCommand : PitchExpCommand {
        public int Index;
        public PitchPoint Point;
        public DeletePitchPointCommand(UVoicePart part, UNote note, int index) {
            this.Part = part;
            this.Note = note;
            this.Index = index;
            this.Point = Note.pitch.data[Index];
        }
        public override string ToString() { return "Delete pitch point"; }
        public override void Execute() { Note.pitch.data.RemoveAt(Index); }
        public override void Unexecute() { Note.pitch.data.Insert(Index, Point); }
    }

    public class ChangePitchPointShapeCommand : PitchExpCommand {
        public PitchPoint Point;
        public PitchPointShape NewShape;
        public PitchPointShape OldShape;
        public ChangePitchPointShapeCommand(PitchPoint point, PitchPointShape shape) {
            this.Point = point;
            this.NewShape = shape;
            this.OldShape = point.shape;
        }
        public override string ToString() { return "Change pitch point shape"; }
        public override void Execute() { Point.shape = NewShape; }
        public override void Unexecute() { Point.shape = OldShape; }
    }

    public class SnapPitchPointCommand : PitchExpCommand {
        readonly float X, Y;
        public SnapPitchPointCommand(UNote note) {
            Note = note;
            X = Note.pitch.data.First().X;
            Y = Note.pitch.data.First().Y;
        }
        public override string ToString() { return "Toggle pitch snap"; }
        public override void Execute() {
            Note.pitch.snapFirst = !Note.pitch.snapFirst;
            if (!Note.pitch.snapFirst) {
                Note.pitch.data.First().X = X;
                Note.pitch.data.First().Y = Y;
            }
        }
        public override void Unexecute() {
            Note.pitch.snapFirst = !Note.pitch.snapFirst;
            if (!Note.pitch.snapFirst) {
                Note.pitch.data.First().X = X;
                Note.pitch.data.First().Y = Y;
            }
        }
    }

    public class AddPitchPointCommand : PitchExpCommand {
        public int Index;
        public PitchPoint Point;
        public AddPitchPointCommand(UNote note, PitchPoint point, int index) {
            this.Note = note;
            this.Index = index;
            this.Point = point;
        }
        public override string ToString() { return "Add pitch point"; }
        public override void Execute() { Note.pitch.data.Insert(Index, Point); }
        public override void Unexecute() { Note.pitch.data.RemoveAt(Index); }
    }

    public class MovePitchPointCommand : PitchExpCommand {
        public PitchPoint Point;
        public float DeltaX, DeltaY;
        public MovePitchPointCommand(PitchPoint point, float deltaX, float deltaY) {
            this.Point = point;
            this.DeltaX = deltaX;
            this.DeltaY = deltaY;
        }
        public override string ToString() { return "Move pitch point"; }
        public override void Execute() { Point.X += DeltaX; Point.Y += DeltaY; }
        public override void Unexecute() { Point.X -= DeltaX; Point.Y -= DeltaY; }
    }
}

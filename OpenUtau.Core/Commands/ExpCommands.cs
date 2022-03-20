using System;
using System.Collections.Generic;
using System.Linq;

using OpenUtau.Core.Ustx;

namespace OpenUtau.Core {
    public abstract class ExpCommand : UCommand {
        public UVoicePart Part;
        public UNote Note;
        public string Key;
        public override ValidateOptions ValidateOptions
            => new ValidateOptions {
                SkipPhonemizer = true,
            };
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
        static readonly HashSet<string> needsPhonemizer = new HashSet<string> {
            Format.Ustx.ALT, Format.Ustx.CLR, Format.Ustx.SHFT,
        };

        public readonly UProject project;
        public readonly UTrack track;
        public readonly UPhoneme phoneme;
        public readonly float newValue;
        public readonly float oldValue;
        public override ValidateOptions ValidateOptions
            => new ValidateOptions {
                SkipPhonemizer = !needsPhonemizer.Contains(Key),
            };
        public SetPhonemeExpressionCommand(UProject project, UTrack track, UPhoneme phoneme, string abbr, float value) {
            this.project = project;
            this.track = track;
            this.phoneme = phoneme;
            Key = abbr;
            newValue = value;
            oldValue = phoneme.GetExpression(project, track, abbr).Item1;
        }
        public override string ToString() => $"Set phoneme expression {Key}";
        public override void Execute() => phoneme.SetExpression(project, track, Key, newValue);
        public override void Unexecute() => phoneme.SetExpression(project, track, Key, oldValue);
    }

    public class ResetExpressionsCommand : ExpCommand {
        List<UExpression> noteExpressions;
        List<UExpression> phonemeExpressions;
        public ResetExpressionsCommand(UNote note) {
            Note = note;
            noteExpressions = note.noteExpressions;
            phonemeExpressions = note.phonemeExpressions;
        }
        public override string ToString() => "Reset expressions.";
        public override void Execute() {
            Note.noteExpressions = new List<UExpression>();
            Note.phonemeExpressions = new List<UExpression>();
        }
        public override void Unexecute() {
            Note.noteExpressions = noteExpressions;
            Note.phonemeExpressions = phonemeExpressions;
        }
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
        readonly UVoicePart part;
        readonly PitchPoint point;
        readonly float deltaX;
        readonly float deltaY;
        public override ValidateOptions ValidateOptions
            => new ValidateOptions {
                part = part,
                SkipPhonemizer = true,
                SkipPhoneme = true,
            };
        public MovePitchPointCommand(UVoicePart part, PitchPoint point, float deltaX, float deltaY) {
            this.part = part;
            this.point = point;
            this.deltaX = deltaX;
            this.deltaY = deltaY;
        }
        public override string ToString() { return "Move pitch point"; }
        public override void Execute() { point.X += deltaX; point.Y += deltaY; }
        public override void Unexecute() { point.X -= deltaX; point.Y -= deltaY; }
    }

    public class ResetPitchPointsCommand : PitchExpCommand {
        UPitch oldPitch;
        UPitch newPitch;
        public ResetPitchPointsCommand(UNote note) {
            Note = note;
            oldPitch = note.pitch;
            newPitch = new UPitch();
            newPitch.AddPoint(new PitchPoint(-40, 0));
            newPitch.AddPoint(new PitchPoint(40, 0));
        }
        public override string ToString() => "Reset pitch points";
        public override void Execute() => Note.pitch = newPitch;
        public override void Unexecute() => Note.pitch = oldPitch;
    }

    public class SetCurveCommand : ExpCommand {
        readonly UProject project;
        readonly UVoicePart part;
        readonly string abbr;
        readonly int x;
        readonly int y;
        readonly int lastX;
        readonly int lastY;
        int[] oldXs;
        int[] oldYs;
        public override ValidateOptions ValidateOptions
            => new ValidateOptions {
                part = part,
                SkipPhonemizer = true,
                SkipPhoneme = true,
            };
        public SetCurveCommand(UProject project, UVoicePart part, string abbr, int x, int y, int lastX, int lastY) {
            this.project = project;
            this.part = part;
            this.abbr = abbr;
            this.x = x;
            this.y = y;
            this.lastX = lastX;
            this.lastY = lastY;
            var curve = part.curves.FirstOrDefault(c => c.abbr == abbr);
            oldXs = curve?.xs.ToArray();
            oldYs = curve?.ys.ToArray();
        }
        public override string ToString() => "Edit Curve";
        public override void Execute() {
            var curve = part.curves.FirstOrDefault(c => c.abbr == abbr);
            if (project.expressions.TryGetValue(abbr, out var descriptor)) {
                if (curve == null) {
                    curve = new UCurve(descriptor);
                    part.curves.Add(curve);
                }
                int y1 = (int)Math.Clamp(y, descriptor.min, descriptor.max);
                int lastY1 = (int)Math.Clamp(lastY, descriptor.min, descriptor.max);
                curve.Set(x, y1, lastX, lastY1);
            }
        }
        public override void Unexecute() {
            var curve = part.curves.FirstOrDefault(c => c.abbr == abbr);
            if (curve == null) {
                return;
            }
            curve.xs.Clear();
            curve.ys.Clear();
            if (oldXs != null && oldYs != null) {
                curve.xs.AddRange(oldXs);
                curve.ys.AddRange(oldYs);
            }
        }
        public override bool Mergeable => true;
        public override UCommand Merge(IList<UCommand> commands) {
            var first = commands.First() as SetCurveCommand;
            var last = commands.Last() as SetCurveCommand;
            var curve = part.curves.FirstOrDefault(c => c.abbr == abbr);
            curve.Simplify();
            int[] newXs = curve?.xs.ToArray();
            int[] newYs = curve?.ys.ToArray();
            return new MergedSetCurveCommand(
                last.project, last.part, last.abbr,
                first.oldXs, first.oldYs, newXs, newYs);
        }
    }

    public class MergedSetCurveCommand : ExpCommand {
        readonly UProject project;
        readonly UVoicePart part;
        readonly string abbr;
        readonly int[] oldXs;
        readonly int[] oldYs;
        readonly int[] newXs;
        readonly int[] newYs;
        public MergedSetCurveCommand(UProject project, UVoicePart part,
            string abbr, int[] oldXs, int[] oldYs, int[] newXs, int[] newYs) {
            this.project = project;
            this.part = part;
            this.abbr = abbr;
            this.oldXs = oldXs;
            this.oldYs = oldYs;
            this.newXs = newXs;
            this.newYs = newYs;
        }
        public override string ToString() => "Edit Curve";
        public override void Execute() {
            var curve = part.curves.FirstOrDefault(c => c.abbr == abbr);
            if (curve == null && project.expressions.TryGetValue(abbr, out var descriptor)) {
                curve = new UCurve(descriptor);
                part.curves.Add(curve);
            }
            curve.xs.Clear();
            curve.ys.Clear();
            if (newXs != null && newYs != null) {
                curve.xs.AddRange(newXs);
                curve.ys.AddRange(newYs);
            }
        }
        public override void Unexecute() {
            var curve = part.curves.FirstOrDefault(c => c.abbr == abbr);
            if (curve == null && project.expressions.TryGetValue(abbr, out var descriptor)) {
                curve = new UCurve(descriptor);
                part.curves.Add(curve);
            }
            curve.xs.Clear();
            curve.ys.Clear();
            if (oldXs != null && oldYs != null) {
                curve.xs.AddRange(oldXs);
                curve.ys.AddRange(oldYs);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;

namespace OpenUtau.Core {
    public abstract class ExpCommand : UCommand {
        public UVoicePart Part;
        public UNote Note;
        public string Key;
        public override ValidateOptions ValidateOptions
            => new ValidateOptions {
                SkipTiming = true,
                Part = Part,
                SkipPhonemizer = true,
            };
        public ExpCommand(UVoicePart part) {
            Part = part;
        }
    }

    public class SetNoteExpressionCommand : ExpCommand {
        public readonly UProject project;
        public readonly UTrack track;
        public readonly float?[] newValue;
        public readonly float?[] oldValue;
        public SetNoteExpressionCommand(UProject project, UTrack track, UVoicePart part, UNote note, string abbr, float?[] values) : base(part) {
            this.project = project;
            this.track = track;
            this.Note = note;
            Key = abbr;
            newValue = values;
            oldValue = note.GetExpressionNoteHas(project, track, abbr);
        }
        public override string ToString() => $"Set note expression {Key}";
        public override void Execute() => Note.SetExpression(project, track, Key, newValue);
        public override void Unexecute() => Note.SetExpression(project, track, Key, oldValue);
    }

    public class SetPhonemeExpressionCommand : ExpCommand {
        static readonly HashSet<string> needsPhonemizer = new HashSet<string> {
            Format.Ustx.ALT, Format.Ustx.CLR, Format.Ustx.SHFT, Format.Ustx.VEL
        };

        public readonly UProject project;
        public readonly UTrack track;
        public readonly UPhoneme phoneme;
        public readonly float? newValue;
        public readonly float? oldValue;
        public override ValidateOptions ValidateOptions
            => new ValidateOptions {
                SkipTiming = true,
                Part = Part,
                SkipPhonemizer = !needsPhonemizer.Contains(Key),
            };
        public SetPhonemeExpressionCommand(UProject project, UTrack track, UVoicePart part, UPhoneme phoneme, string abbr, float? value) : base(part) {
            this.project = project;
            this.track = track;
            this.phoneme = phoneme;
            Key = abbr;
            newValue = value;
            var oldExp = phoneme.GetExpression(project, track, abbr);
            if (oldExp.Item2) {
                oldValue = oldExp.Item1;
            } else {
                oldValue = null;
            }
        }
        public override string ToString() => $"Set phoneme expression {Key}";
        public override void Execute() {
            phoneme.SetExpression(project, track, Key, newValue);
        }
        public override void Unexecute() {
            phoneme.SetExpression(project, track, Key, oldValue);
        }
    }

    public class ResetExpressionsCommand : ExpCommand {
        List<UExpression> phonemeExpressions;
        public ResetExpressionsCommand(UVoicePart part, UNote note) : base(part) {
            Note = note;
            phonemeExpressions = note.phonemeExpressions;
        }
        public override string ToString() => "Reset expressions.";
        public override void Execute() {
            Note.phonemeExpressions = new List<UExpression>();
        }
        public override void Unexecute() {
            Note.phonemeExpressions = phonemeExpressions;
        }
    }

    public abstract class PitchExpCommand : ExpCommand {
        public PitchExpCommand(UVoicePart part) : base(part) { }
        public override ValidateOptions ValidateOptions => new ValidateOptions {
            SkipTiming = true,
            Part = Part,
            SkipPhonemizer = true,
            SkipPhoneme = true,
        };
    }

    public class DeletePitchPointCommand : PitchExpCommand {
        public int Index;
        public PitchPoint Point;
        public DeletePitchPointCommand(UVoicePart part, UNote note, int index) : base(part) {
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
        public ChangePitchPointShapeCommand(UVoicePart part, PitchPoint point, PitchPointShape shape) : base(part) {
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
        public SnapPitchPointCommand(UVoicePart part, UNote note) : base(part) {
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
        public AddPitchPointCommand(UVoicePart part, UNote note, PitchPoint point, int index) : base(part) {
            this.Note = note;
            this.Index = index;
            this.Point = point;
        }
        public override string ToString() { return "Add pitch point"; }
        public override void Execute() { Note.pitch.data.Insert(Index, Point); }
        public override void Unexecute() { Note.pitch.data.RemoveAt(Index); }
    }

    public class MovePitchPointCommand : PitchExpCommand {
        readonly PitchPoint point;
        readonly float deltaX;
        readonly float deltaY;
        public MovePitchPointCommand(UVoicePart part, PitchPoint point, float deltaX, float deltaY) : base(part) {
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
        public ResetPitchPointsCommand(UVoicePart part, UNote note) : base(part) {
            Note = note;
            oldPitch = note.pitch;
            newPitch = new UPitch();
            int start = NotePresets.Default.DefaultPortamento.PortamentoStart;
            int length = NotePresets.Default.DefaultPortamento.PortamentoLength;
            newPitch.AddPoint(new PitchPoint(start, 0));
            newPitch.AddPoint(new PitchPoint(start + length, 0));
        }
        public override string ToString() => "Reset pitch points";
        public override void Execute() => Note.pitch = newPitch;
        public override void Unexecute() => Note.pitch = oldPitch;
    }

    public class SetPitchPointsCommand : PitchExpCommand {
        UPitch[] oldPitch;
        UNote[] Notes;
        UPitch newPitch;
        public SetPitchPointsCommand(UVoicePart part, UNote note, UPitch pitch) : base(part) {
            Notes = new UNote[] { note };
            oldPitch = Notes.Select(note => note.pitch).ToArray();
            newPitch = pitch;
        }

        public SetPitchPointsCommand(UVoicePart part, IEnumerable<UNote> notes, UPitch pitch) : base(part) {
            Notes = notes.ToArray();
            oldPitch = Notes.Select(note => note.pitch).ToArray();
            newPitch = pitch;
        }
        public override string ToString() => "Set pitch points";
        public override void Execute(){
            lock (Part) {
                for (var i=0; i<Notes.Length; i++) {
                    Notes[i].pitch = newPitch.Clone();
                }
            }
        }
        public override void Unexecute() {
            lock (Part) {
                for (var i = 0; i < Notes.Length; i++) {
                    Notes[i].pitch = oldPitch[i];
                }
            }
        }
    }

    public class SetCurveCommand : ExpCommand {
        readonly UProject project;
        readonly string abbr;
        readonly int x;
        readonly int y;
        readonly int lastX;
        readonly int lastY;
        int[] oldXs;
        int[] oldYs;
        public override ValidateOptions ValidateOptions
            => new ValidateOptions {
                SkipTiming = true,
                Part = Part,
                SkipPhonemizer = true,
                SkipPhoneme = true,
            };
        public SetCurveCommand(UProject project, UVoicePart part, string abbr, int x, int y, int lastX, int lastY) : base(part) {
            this.project = project;
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
            var curve = Part.curves.FirstOrDefault(c => c.abbr == abbr);
            if (project.expressions.TryGetValue(abbr, out var descriptor)) {
                if (curve == null) {
                    curve = new UCurve(descriptor);
                    Part.curves.Add(curve);
                }
                int y1 = (int)Math.Clamp(y, descriptor.min, descriptor.max);
                int lastY1 = (int)Math.Clamp(lastY, descriptor.min, descriptor.max);
                curve.Set(x, y1, lastX, lastY1);
            }
        }
        public override void Unexecute() {
            var curve = Part.curves.FirstOrDefault(c => c.abbr == abbr);
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
        public override bool CanMerge(IList<UCommand> commands) {
            return commands.All(c => c is SetCurveCommand);
        }
        public override UCommand Merge(IList<UCommand> commands) {
            var first = commands.First() as SetCurveCommand;
            var last = commands.Last() as SetCurveCommand;
            var curve = Part.curves.FirstOrDefault(c => c.abbr == abbr);
            curve.Simplify();
            int[] newXs = curve?.xs.ToArray();
            int[] newYs = curve?.ys.ToArray();
            return new MergedSetCurveCommand(
                last.project, last.Part, last.abbr,
                first.oldXs, first.oldYs, newXs, newYs);
        }
    }

    public class MergedSetCurveCommand : ExpCommand {
        readonly UProject project;
        readonly string abbr;
        readonly int[] oldXs;
        readonly int[] oldYs;
        readonly int[] newXs;
        readonly int[] newYs;
        public MergedSetCurveCommand(UProject project, UVoicePart part,
            string abbr, int[] oldXs, int[] oldYs, int[] newXs, int[] newYs) : base(part) {
            this.project = project;
            this.abbr = abbr;
            this.oldXs = oldXs;
            this.oldYs = oldYs;
            this.newXs = newXs;
            this.newYs = newYs;
        }
        public override string ToString() => "Edit Curve";
        public override void Execute() {
            var curve = Part.curves.FirstOrDefault(c => c.abbr == abbr);
            if (curve == null && project.expressions.TryGetValue(abbr, out var descriptor)) {
                curve = new UCurve(descriptor);
                Part.curves.Add(curve);
            }
            curve.xs.Clear();
            curve.ys.Clear();
            if (newXs != null && newYs != null) {
                curve.xs.AddRange(newXs);
                curve.ys.AddRange(newYs);
            }
        }
        public override void Unexecute() {
            var curve = Part.curves.FirstOrDefault(c => c.abbr == abbr);
            if (curve == null && project.expressions.TryGetValue(abbr, out var descriptor)) {
                curve = new UCurve(descriptor);
                Part.curves.Add(curve);
            }
            curve.xs.Clear();
            curve.ys.Clear();
            if (oldXs != null && oldYs != null) {
                curve.xs.AddRange(oldXs);
                curve.ys.AddRange(oldYs);
            }
        }
    }

    public class ClearCurveCommand : ExpCommand {
        readonly string abbr;
        readonly int[] oldXs;
        readonly int[] oldYs;
        public ClearCurveCommand(UVoicePart part, string abbr) : base(part) {
            this.abbr = abbr;
            var curve = Part.curves.FirstOrDefault(curve => curve.abbr == abbr);
            if (curve != null) {
                oldXs = curve.xs.ToArray();
                oldYs = curve.ys.ToArray();
            }
        }
        public override string ToString() => "Clear Curve";
        public override void Execute() {
            var curve = Part.curves.FirstOrDefault(curve => curve.abbr == abbr);
            if (curve != null) {
                curve.xs.Clear();
                curve.ys.Clear();
            }
        }
        public override void Unexecute() {
            var curve = Part.curves.FirstOrDefault(curve => curve.abbr == abbr);
            if (curve != null && oldXs != null && oldYs != null) {
                curve.xs.Clear();
                curve.xs.AddRange(oldXs);
                curve.ys.Clear();
                curve.ys.AddRange(oldYs);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using OpenUtau.App.Controls;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.App.Views {
    class NoteEditState {
        public virtual MouseButton MouseButton => MouseButton.Left;
        public readonly Canvas canvas;
        public readonly PianoRollViewModel vm;
        public Point startPoint;
        public NoteEditState(Canvas canvas, PianoRollViewModel vm) {
            this.canvas = canvas;
            this.vm = vm;
        }
        public virtual void Begin(IPointer pointer, Point point) {
            pointer.Capture(canvas);
            startPoint = point;
            DocManager.Inst.StartUndoGroup();
        }
        public virtual void End(IPointer pointer, Point point) {
            pointer.Capture(null);
            DocManager.Inst.EndUndoGroup();
        }
        public virtual void Update(IPointer pointer, Point point) { }
        public static void Swap<T>(ref T a, ref T b) {
            T temp = a;
            a = b;
            b = temp;
        }
    }

    class NoteSelectionEditState : NoteEditState {
        public readonly Rectangle selectionBox;
        public NoteSelectionEditState(Canvas canvas, PianoRollViewModel vm, Rectangle selectionBox) : base(canvas, vm) {
            this.selectionBox = selectionBox;
        }
        public override void Begin(IPointer pointer, Point point) {
            base.Begin(pointer, point);
            selectionBox.IsVisible = true;
        }
        public override void End(IPointer pointer, Point point) {
            base.End(pointer, point);
            selectionBox.IsVisible = false;
            var notesVm = vm.NotesViewModel;
            notesVm.CommitTempSelectNotes();
        }
        public override void Update(IPointer pointer, Point point) {
            var notesVm = vm.NotesViewModel;
            int x0 = notesVm.PointToSnappedTick(point);
            int x1 = notesVm.PointToSnappedTick(startPoint);
            int y0 = notesVm.PointToTone(point);
            int y1 = notesVm.PointToTone(startPoint);
            if (x0 > x1) {
                Swap(ref x0, ref x1);
            }
            if (y0 > y1) {
                Swap(ref y0, ref y1);
            }
            x1 += notesVm.SnapUnit;
            y0--;
            var leftTop = notesVm.TickToneToPoint(x0, y1);
            var Size = notesVm.TickToneToSize(x1 - x0, y1 - y0);
            Canvas.SetLeft(selectionBox, leftTop.X);
            Canvas.SetTop(selectionBox, leftTop.Y);
            selectionBox.Width = Size.Width + 1;
            selectionBox.Height = Size.Height;
            notesVm.TempSelectNotes(x0, x1, y0, y1);
        }
    }

    class NoteMoveEditState : NoteEditState {
        public readonly UNote note;
        private double xOffset;
        public NoteMoveEditState(Canvas canvas, PianoRollViewModel vm, UNote note) : base(canvas, vm) {
            this.note = note;
            var notesVm = vm.NotesViewModel;
            if (!notesVm.SelectedNotes.Contains(note)) {
                notesVm.DeselectNotes();
            }
        }
        public override void Begin(IPointer pointer, Point point) {
            base.Begin(pointer, point);
            var notesVm = vm.NotesViewModel;
            xOffset = point.X - notesVm.TickToneToPoint(note.position, 0).X;
        }
        public override void Update(IPointer pointer, Point point) {
            var notesVm = vm.NotesViewModel;
            var part = notesVm.Part;

            int deltaTone = notesVm.PointToTone(point) - note.tone;
            int minDeltaTone;
            int maxDeltaTone;
            if (notesVm.SelectedNotes.Count > 0) {
                minDeltaTone = -notesVm.SelectedNotes.Select(p => p.tone).Min();
                maxDeltaTone = ViewConstants.MaxTone - 1 - notesVm.SelectedNotes.Select(p => p.tone).Max();
            } else {
                minDeltaTone = -note.tone;
                maxDeltaTone = ViewConstants.MaxTone - 1 - note.tone;
            }
            deltaTone = Math.Clamp(deltaTone, minDeltaTone, maxDeltaTone);

            int deltaTick = notesVm.IsSnapOn
                ? notesVm.PointToSnappedTick(point - new Point(xOffset, 0)) - note.position
                : notesVm.PointToTick(point - new Point(xOffset, 0)) - note.position;
            int minDeltaTick;
            if (notesVm.SelectedNotes.Count > 0) {
                minDeltaTick = -notesVm.SelectedNotes.Select(n => n.position).Min();
            } else {
                minDeltaTick = -note.position;
            }
            deltaTick = Math.Max(deltaTick, minDeltaTick);

            if (deltaTone == 0 && deltaTick == 0) {
                return;
            }
            if (notesVm.SelectedNotes.Count == 0) {
                DocManager.Inst.ExecuteCmd(new MoveNoteCommand(
                    part, note, deltaTick, deltaTone));
            } else {
                DocManager.Inst.ExecuteCmd(new MoveNoteCommand(
                    part, new List<UNote>(notesVm.SelectedNotes), deltaTick, deltaTone));
            }
        }
    }

    class NoteResizeEditState : NoteEditState {
        public readonly UNote note;
        public NoteResizeEditState(Canvas canvas, PianoRollViewModel vm, UNote note) : base(canvas, vm) {
            this.note = note;
            var notesVm = vm.NotesViewModel;
            if (!notesVm.SelectedNotes.Contains(note)) {
                notesVm.DeselectNotes();
            }
        }
        public override void Update(IPointer pointer, Point point) {
            var project = DocManager.Inst.Project;
            var notesVm = vm.NotesViewModel;
            int deltaDuration = notesVm.PointToSnappedTick(point) + notesVm.SnapUnit - note.End;
            if (deltaDuration < 0) {
                int minNoteTicks = notesVm.IsSnapOn ? notesVm.SnapUnit : 15;
                int maxDurReduction = note.duration - minNoteTicks;
                if (notesVm.SelectedNotes.Count > 0) {
                    maxDurReduction = notesVm.SelectedNotes.Min(n => n.duration - minNoteTicks);
                }
                if (notesVm.IsSnapOn && notesVm.SnapUnit > 0) {
                    maxDurReduction = (int)Math.Floor((double)maxDurReduction / notesVm.SnapUnit) * notesVm.SnapUnit;
                }
                deltaDuration = Math.Max(deltaDuration, -maxDurReduction);
            }
            if (deltaDuration == 0) {
                return;
            }
            if (notesVm.SelectedNotes.Count == 0) {
                DocManager.Inst.ExecuteCmd(new ResizeNoteCommand(notesVm.Part, note, deltaDuration));
                return;
            }
            DocManager.Inst.ExecuteCmd(new ResizeNoteCommand(notesVm.Part, new List<UNote>(notesVm.SelectedNotes), deltaDuration));
        }
    }

    class NoteEraseEditState : NoteEditState {
        public override MouseButton MouseButton => MouseButton.Right;
        public NoteEraseEditState(Canvas canvas, PianoRollViewModel vm) : base(canvas, vm) { }
        public override void Update(IPointer pointer, Point point) {
            var notesVm = vm.NotesViewModel;
            var noteHitInfo = notesVm.HitTest.HitTestNote(point);
            if (noteHitInfo.hitBody) {
                DocManager.Inst.ExecuteCmd(new RemoveNoteCommand(notesVm.Part, noteHitInfo.note));
            }
        }
    }

    class NotePanningState : NoteEditState {
        public override MouseButton MouseButton => MouseButton.Middle;
        public NotePanningState(Canvas canvas, PianoRollViewModel vm) : base(canvas, vm) { }
        public override void Begin(IPointer pointer, Point point) {
            pointer.Capture(canvas);
            startPoint = point;
        }
        public override void End(IPointer pointer, Point point) {
            pointer.Capture(null);
        }
        public override void Update(IPointer pointer, Point point) {
            var notesVm = vm.NotesViewModel;
            double deltaX = (point.X - startPoint.X) / notesVm.TickWidth;
            double deltaY = (point.Y - startPoint.Y) / notesVm.TrackHeight;
            startPoint = point;
            notesVm.TickOffset = Math.Max(0, notesVm.TickOffset - deltaX);
            notesVm.TrackOffset = Math.Max(0, notesVm.TrackOffset - deltaY);
        }
    }

    class PitchPointEditState : NoteEditState {
        public readonly UNote note;
        private bool onPoint;
        private float x;
        private float y;
        private int index;
        private PitchPoint pitchPoint;
        public PitchPointEditState(
            Canvas canvas, PianoRollViewModel vm, UNote note,
            int index, bool onPoint, float x, float y) : base(canvas, vm) {
            this.note = note;
            this.index = index;
            this.onPoint = onPoint;
            this.x = x;
            this.y = y;
            pitchPoint = note.pitch.data[index];
        }
        public override void Begin(IPointer pointer, Point point) {
            base.Begin(pointer, point);
            if (!onPoint) {
                pitchPoint = new PitchPoint(x, y);
                index++;
                DocManager.Inst.ExecuteCmd(new AddPitchPointCommand(note, pitchPoint, index));
            }
        }
        public override void End(IPointer pointer, Point point) {
            if (note.pitch.data.Count > 2) {
                var notesVm = vm.NotesViewModel;
                bool removed = false;
                if (index > 0) {
                    var prev = note.pitch.data[index - 1];
                    var size = notesVm.TickToneToSize(prev.X - pitchPoint.X, (prev.Y - pitchPoint.Y) * 0.1);
                    if (size.Width * size.Width + size.Height * size.Height < 64) {
                        DocManager.Inst.ExecuteCmd(new DeletePitchPointCommand(notesVm.Part, note, index));
                        removed = true;
                    }
                }
                if (!removed && index < note.pitch.data.Count - 1) {
                    var next = note.pitch.data[index + 1];
                    var size = notesVm.TickToneToSize(next.X - pitchPoint.X, (next.Y - pitchPoint.Y) * 0.1);
                    if (size.Width * size.Width + size.Height * size.Height < 64) {
                        DocManager.Inst.ExecuteCmd(new DeletePitchPointCommand(notesVm.Part, note, index));
                    }
                }
            }
            base.End(pointer, point);
        }
        public override void Update(IPointer pointer, Point point) {
            var notesVm = vm.NotesViewModel;
            int tick = notesVm.PointToTick(point) - note.position;
            double deltaX = notesVm.Project.TickToMillisecond(tick) - pitchPoint.X;
            bool isFirst = index == 0;
            bool isLast = index == note.pitch.data.Count - 1;
            if (!isFirst) {
                deltaX = Math.Max(deltaX, note.pitch.data[index - 1].X - pitchPoint.X);
            }
            if (!isLast) {
                deltaX = Math.Min(deltaX, note.pitch.data[index + 1].X - pitchPoint.X);
            }
            double deltaY = 0;
            if (!(isFirst && note.pitch.snapFirst) && !isLast) {
                deltaY = (notesVm.PointToToneDouble(point) - note.tone) * 10 - pitchPoint.Y;
            }
            if (deltaX == 0 && deltaY == 0) {
                return;
            }
            DocManager.Inst.ExecuteCmd(new MovePitchPointCommand(pitchPoint, (float)deltaX, (float)deltaY));
        }
    }

    class ExpSetValueState : NoteEditState {
        private Border tip;
        public ExpSetValueState(Canvas canvas, PianoRollViewModel vm, Border tip) : base(canvas, vm) {
            this.tip = tip;
        }
        public override void Begin(IPointer pointer, Point point) {
            base.Begin(pointer, point);
            var notesVm = vm.NotesViewModel;
            notesVm.ShowExpValueTip = true;
        }
        public override void End(IPointer pointer, Point point) {
            base.End(pointer, point);
            var notesVm = vm.NotesViewModel;
            notesVm.ShowExpValueTip = false;
        }
        public override void Update(IPointer pointer, Point point) {
            var notesVm = vm.NotesViewModel;
            var noteHitInfo = notesVm.HitTest.HitTestExp(point);
            string key = notesVm.PrimaryKey;
            var descriptor = notesVm.Project.expressions[key];
            double newValue = descriptor.min + (descriptor.max - descriptor.min) * (1 - point.Y / canvas.Bounds.Height);
            newValue = Math.Max(descriptor.min, Math.Min(descriptor.max, newValue));
            notesVm.ExpValueTipText = ((int)newValue).ToString();
            Canvas.SetLeft(tip, point.X);
            Canvas.SetTop(tip, point.Y - 21);
            if (noteHitInfo.phoneme == null) {
                return;
            }
            float value = noteHitInfo.phoneme.GetExpression(notesVm.Project, key).Item1;
            if ((int)value != (int)newValue) {
                DocManager.Inst.ExecuteCmd(new SetPhonemeExpressionCommand(
                    notesVm.Project, noteHitInfo.phoneme, key, (int)newValue));
            }
        }
    }

    class ExpResetValueState : NoteEditState {
        public override MouseButton MouseButton => MouseButton.Right;
        public ExpResetValueState(Canvas canvas, PianoRollViewModel vm) : base(canvas, vm) { }
        public override void Update(IPointer pointer, Point point) {
            var notesVm = vm.NotesViewModel;
            var noteHitInfo = notesVm.HitTest.HitTestExp(point);
            if (noteHitInfo.phoneme == null) {
                return;
            }
            string key = notesVm.PrimaryKey;
            var descriptor = notesVm.Project.expressions[key];
            float value = noteHitInfo.phoneme.GetExpression(notesVm.Project, key).Item1;
            if (value != descriptor.defaultValue) {
                DocManager.Inst.ExecuteCmd(new SetPhonemeExpressionCommand(
                    notesVm.Project, noteHitInfo.phoneme, key, descriptor.defaultValue));
            }
        }
    }
}

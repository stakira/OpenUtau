using System;
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
            y1++;
            var leftTop = notesVm.TickToneToPoint(x0, y1);
            var Size = notesVm.TickToneToSize(x1 - x0, y1 - y0);
            Canvas.SetLeft(selectionBox, leftTop.X);
            Canvas.SetTop(selectionBox, leftTop.Y);
            selectionBox.Width = Size.Width + 1;
            selectionBox.Height = Size.Height;
            notesVm.TempSelectNotes(x0, x1, y0, y1);
        }
    }

}

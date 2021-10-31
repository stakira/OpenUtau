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
    class PartEditState {
        public virtual MouseButton MouseButton => MouseButton.Left;
        public readonly Canvas canvas;
        public readonly MainWindowViewModel vm;
        public Point startPoint;
        public PartEditState(Canvas canvas, MainWindowViewModel vm) {
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

    class PartSelectionEditState : PartEditState {
        public readonly Rectangle selectionBox;
        public PartSelectionEditState(Canvas canvas, MainWindowViewModel vm, Rectangle selectionBox) : base(canvas, vm) {
            this.selectionBox = selectionBox;
        }
        public override void Begin(IPointer pointer, Point point) {
            base.Begin(pointer, point);
            selectionBox.IsVisible = true;
        }

        public override void End(IPointer pointer, Point point) {
            base.End(pointer, point);
            selectionBox.IsVisible = false;
            var tracksVm = vm.TracksViewModel;
            tracksVm.CommitTempSelectParts();
        }
        public override void Update(IPointer pointer, Point point) {
            var tracksVm = vm.TracksViewModel;
            int x0 = tracksVm.PointToSnappedTick(point);
            int x1 = tracksVm.PointToSnappedTick(startPoint);
            int y0 = tracksVm.PointToTrackNo(point);
            int y1 = tracksVm.PointToTrackNo(startPoint);
            if (x0 > x1) {
                Swap(ref x0, ref x1);
            }
            if (y0 > y1) {
                Swap(ref y0, ref y1);
            }
            x1 += tracksVm.SnapUnit;
            y1++;
            var leftTop = tracksVm.TickTrackToPoint(x0, y0);
            var Size = tracksVm.TickTrackToSize(x1 - x0, y1 - y0);
            Canvas.SetLeft(selectionBox, leftTop.X);
            Canvas.SetTop(selectionBox, leftTop.Y);
            selectionBox.Width = Size.Width + 1;
            selectionBox.Height = Size.Height;
            tracksVm.TempSelectParts(x0, x1, y0, y1);
        }
    }

    class PartMoveEditState : PartEditState {
        public readonly UPart part;
        public readonly bool isVoice;
        private double xOffset;
        public PartMoveEditState(Canvas canvas, MainWindowViewModel vm, UPart part) : base(canvas, vm) {
            this.part = part;
            isVoice = part is UVoicePart;
            var tracksVm = vm.TracksViewModel;
            if (!tracksVm.SelectedParts.Contains(part)) {
                tracksVm.DeselectParts();
            }
        }
        public override void Begin(IPointer pointer, Point point) {
            base.Begin(pointer, point);
            var tracksVm = vm.TracksViewModel;
            xOffset = point.X - tracksVm.TickTrackToPoint(part.position, 0).X;
        }
        public override void Update(IPointer pointer, Point point) {
            var project = DocManager.Inst.Project;
            var tracksVm = vm.TracksViewModel;

            int deltaTrack = tracksVm.PointToTrackNo(point) - part.trackNo;
            int minDeltaTrack;
            int maxDeltaTrack;
            if (tracksVm.SelectedParts.Count > 0) {
                minDeltaTrack = -tracksVm.SelectedParts.Select(p => p.trackNo).Min();
                maxDeltaTrack = project.tracks.Count - 1 - tracksVm.SelectedParts.Select(p => p.trackNo).Max();
            } else {
                minDeltaTrack = -part.trackNo;
                maxDeltaTrack = project.tracks.Count - 1 - part.trackNo;
            }
            deltaTrack = Math.Clamp(deltaTrack, minDeltaTrack, maxDeltaTrack);

            int deltaTick = isVoice
                ? tracksVm.PointToSnappedTick(point - new Point(xOffset, 0)) - part.position
                : tracksVm.PointToTick(point - new Point(xOffset, 0)) - part.position;
            int minDeltaTick;
            if (tracksVm.SelectedParts.Count > 0) {
                minDeltaTick = -tracksVm.SelectedParts.Select(p => p.position).Min();
            } else {
                minDeltaTick = -part.position;
            }
            deltaTick = Math.Max(deltaTick, minDeltaTick);

            if (deltaTrack == 0 && deltaTick == 0) {
                return;
            }
            if (tracksVm.SelectedParts.Count == 0) {
                DocManager.Inst.ExecuteCmd(new MovePartCommand(
                    project, part, part.position + deltaTick, part.trackNo + deltaTrack));
                return;
            }
            foreach (var part in tracksVm.SelectedParts) {
                DocManager.Inst.ExecuteCmd(new MovePartCommand(
                    project, part, part.position + deltaTick, part.trackNo + deltaTrack));
            }
        }
    }

    class PartResizeEditState : PartEditState {
        public readonly UPart part;
        public PartResizeEditState(Canvas canvas, MainWindowViewModel vm, UPart part) : base(canvas, vm) {
            this.part = part;
            var tracksVm = vm.TracksViewModel;
            if (!tracksVm.SelectedParts.Contains(part)) {
                tracksVm.DeselectParts();
            }
        }
        public override void Update(IPointer pointer, Point point) {
            var project = DocManager.Inst.Project;
            var tracksVm = vm.TracksViewModel;
            int deltaDuration = tracksVm.PointToSnappedTick(point) + tracksVm.SnapUnit - part.EndTick;
            if (deltaDuration < 0) {
                int maxDurReduction = part.Duration - part.GetMinDurTick(project);
                if (tracksVm.SelectedParts.Count > 0) {
                    maxDurReduction = tracksVm.SelectedParts.Min(p => p.Duration - p.GetMinDurTick(project));
                }
                if (tracksVm.SnapUnit > 0) {
                    maxDurReduction = (int)Math.Floor((double)maxDurReduction / tracksVm.SnapUnit) * tracksVm.SnapUnit;
                }
                deltaDuration = Math.Max(deltaDuration, -maxDurReduction);
            }
            if (deltaDuration == 0) {
                return;
            }
            if (tracksVm.SelectedParts.Count == 0) {
                DocManager.Inst.ExecuteCmd(new ResizePartCommand(project, part, part.Duration + deltaDuration));
                return;
            }
            foreach (UPart part in tracksVm.SelectedParts) {
                DocManager.Inst.ExecuteCmd(new ResizePartCommand(project, part, part.Duration + deltaDuration));
            }
        }
    }

    class PartEraseEditState : PartEditState {
        public override MouseButton MouseButton => MouseButton.Right;
        public PartEraseEditState(Canvas canvas, MainWindowViewModel vm) : base(canvas, vm) { }
        public override void Update(IPointer pointer, Point point) {
            var project = DocManager.Inst.Project;
            var control = canvas.InputHitTest(point);
            if (control is PartControl partControl) {
                DocManager.Inst.ExecuteCmd(new RemovePartCommand(project, partControl.part));
            }
        }
    }

    class PartPanningState : PartEditState {
        public override MouseButton MouseButton => MouseButton.Middle;
        public PartPanningState(Canvas canvas, MainWindowViewModel vm) : base(canvas, vm) { }
        public override void Begin(IPointer pointer, Point point) {
            pointer.Capture(canvas);
            startPoint = point;
        }
        public override void End(IPointer pointer, Point point) {
            pointer.Capture(null);
        }
        public override void Update(IPointer pointer, Point point) {
            var tracksVm = vm.TracksViewModel;
            double deltaX = (point.X - startPoint.X) / tracksVm.TickWidth;
            double deltaY = (point.Y - startPoint.Y) / tracksVm.TrackHeight;
            startPoint = point;
            tracksVm.TickOffset = Math.Max(0, tracksVm.TickOffset - deltaX);
            tracksVm.TrackOffset = Math.Max(0, tracksVm.TrackOffset - deltaY);
        }
    }
}

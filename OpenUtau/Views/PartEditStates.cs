﻿using System;
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
        public readonly Control control;
        public readonly MainWindowViewModel vm;
        public Point startPoint;
        public PartEditState(Control control, MainWindowViewModel vm) {
            this.control = control;
            this.vm = vm;
        }
        public virtual void Begin(IPointer pointer, Point point) {
            pointer.Capture(control);
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
        private int startTick;
        private int startTrack;

        public readonly Rectangle selectionBox;
        public PartSelectionEditState(Control control, MainWindowViewModel vm, Rectangle selectionBox) : base(control, vm) {
            this.selectionBox = selectionBox;
        }
        public override void Begin(IPointer pointer, Point point) {
            base.Begin(pointer, point);
            selectionBox.IsVisible = true;
            var tracksVm = vm.TracksViewModel;
            startTick = tracksVm.PointToTick(point);
            startTrack = tracksVm.PointToTrackNo(point);
        }
        public override void End(IPointer pointer, Point point) {
            base.End(pointer, point);
            selectionBox.IsVisible = false;
            var tracksVm = vm.TracksViewModel;
            tracksVm.CommitTempSelectParts();
        }
        public override void Update(IPointer pointer, Point point) {
            var tracksVm = vm.TracksViewModel;
            int tick = tracksVm.PointToTick(point);
            int track = tracksVm.PointToTrackNo(point);

            int minTick = Math.Min(tick, startTick);
            int maxTick = Math.Max(tick, startTick);
            tracksVm.TickToLineTick(minTick, out int x0, out int _);
            tracksVm.TickToLineTick(maxTick, out int _, out int x1);

            int y0 = Math.Min(track, startTrack);
            int y1 = Math.Max(track, startTrack) + 1;

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
        public PartMoveEditState(Control control, MainWindowViewModel vm, UPart part) : base(control, vm) {
            this.part = part;
            isVoice = part is UVoicePart;
            var tracksVm = vm.TracksViewModel;
            if (!tracksVm.SelectedParts.Contains(part)) {
                tracksVm.DeselectParts();
                tracksVm.SelectPart(part);
            }
        }
        public override void Begin(IPointer pointer, Point point) {
            base.Begin(pointer, point);
            var tracksVm = vm.TracksViewModel;
            xOffset = point.X - tracksVm.TickTrackToPoint(part.position, 0).X;
        }
        public override void Update(IPointer pointer, Point point) {
            var delta = point - startPoint;
            if (Math.Abs(delta.X) + Math.Abs(delta.Y) < 4) {
                return;
            }
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

            int newPos = 0;
            if (!isVoice) {
                newPos = tracksVm.PointToTick(point - new Point(xOffset, 0));
            } else {
                tracksVm.PointToLineTick(point - new Point(xOffset, 0), out int left, out int right);
                newPos = left;
            }
            int deltaTick = newPos - part.position;
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
        public readonly bool fromStart;
        public PartResizeEditState(Control control, MainWindowViewModel vm, UPart part, bool fromStart = false) : base(control, vm) {
            this.part = part;
            var tracksVm = vm.TracksViewModel;
            if (!tracksVm.SelectedParts.Contains(part)) {
                tracksVm.DeselectParts();
            }
            this.fromStart = fromStart;
        }
        public override void Update(IPointer pointer, Point point) {
            var project = DocManager.Inst.Project;
            var tracksVm = vm.TracksViewModel;
            tracksVm.PointToLineTick(point, out int left, out int right);

            int deltaDuration = this.fromStart
                ? part.position - left
                : right - part.End;
            if (deltaDuration < 0) {
                int maxDurReduction = fromStart ? part.GetMaxPosiTick(project) - part.position : part.Duration - part.GetMinDurTick(project);
                if (tracksVm.SelectedParts.Count > 0) {
                    maxDurReduction = tracksVm.SelectedParts.Min(p => fromStart ? p.GetMaxPosiTick(project) - p.position : p.Duration - p.GetMinDurTick(project));
                }
                deltaDuration = Math.Max(deltaDuration, -maxDurReduction);
            }
            if (deltaDuration == 0) {
                return;
            }
            if (tracksVm.SelectedParts.Count == 0) {
                DocManager.Inst.ExecuteCmd(new ResizePartCommand(project, part, deltaDuration, fromStart));
                return;
            }
            foreach (UPart part in tracksVm.SelectedParts) {
                DocManager.Inst.ExecuteCmd(new ResizePartCommand(project, part, deltaDuration, fromStart));
            }
        }
    }

    class PartEraseEditState : PartEditState {
        public override MouseButton MouseButton => MouseButton.Right;
        public PartEraseEditState(Control control, MainWindowViewModel vm) : base(control, vm) { }
        public override void Update(IPointer pointer, Point point) {
            var project = DocManager.Inst.Project;
            var control = base.control.InputHitTest(point);
            if (control is PartControl partControl) {
                DocManager.Inst.ExecuteCmd(new RemovePartCommand(project, partControl.part));
            }
        }
    }

    class PartPanningState : PartEditState {
        public override MouseButton MouseButton => MouseButton.Middle;
        public PartPanningState(Control control, MainWindowViewModel vm) : base(control, vm) { }
        public override void Begin(IPointer pointer, Point point) {
            pointer.Capture(control);
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

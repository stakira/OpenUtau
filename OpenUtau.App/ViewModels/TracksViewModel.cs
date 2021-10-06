using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using DynamicData.Binding;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;

namespace OpenUtau.App.ViewModels {
    public class TracksRefreshEvent { }
    public class PartsSelectionEvent {
        public readonly UPart[] selectedParts;
        public readonly UPart[] tempSelectedParts;
        public PartsSelectionEvent(UPart[] selectedParts, UPart[] tempSelectedParts) {
            this.selectedParts = selectedParts;
            this.tempSelectedParts = tempSelectedParts;
        }
    }
    public class PartResizeEvent {
        public readonly UPart part;
        public PartResizeEvent(UPart part) { this.part = part; }
    }
    public class PartMoveEvent {
        public readonly UPart part;
        public PartMoveEvent(UPart part) { this.part = part; }
    }

    public class TracksViewModel : ViewModelBase, ICmdSubscriber {
        public Rect Bounds {
            get => bounds;
            set => this.RaiseAndSetIfChanged(ref bounds, value);
        }
        public int TickCount {
            get => tickCount;
            set => this.RaiseAndSetIfChanged(ref tickCount, value);
        }
        public int TrackCount {
            get => trackCount;
            set => this.RaiseAndSetIfChanged(ref trackCount, value);
        }
        public double TickWidth {
            get => tickWidth;
            set => this.RaiseAndSetIfChanged(ref tickWidth, Math.Clamp(value, ViewConstants.TickWidthMin, ViewConstants.TickWidthMax));
        }
        public double TrackHeightMin => ViewConstants.TrackHeightMin;
        public double TrackHeightMax => ViewConstants.TrackHeightMax;
        public double TrackHeight {
            get => trackHeight;
            set => this.RaiseAndSetIfChanged(ref trackHeight, Math.Clamp(value, ViewConstants.TrackHeightMin, ViewConstants.TrackHeightMax));
        }
        public double TickOffset {
            get => tickOffset;
            set => this.RaiseAndSetIfChanged(ref tickOffset, value);
        }
        public double TrackOffset {
            get => trackOffset;
            set => this.RaiseAndSetIfChanged(ref trackOffset, value);
        }
        public double ViewportTicks => viewportTicks.Value;
        public double ViewportTracks => viewportTracks.Value;
        public double SmallChangeX => smallChangeX.Value;
        public double SmallChangeY => smallChangeY.Value;
        public ObservableCollectionExtended<UPart> Parts { get; } = new ObservableCollectionExtended<UPart>();
        public ObservableCollectionExtended<UTrack> Tracks { get; } = new ObservableCollectionExtended<UTrack>();

        private Rect bounds;
        private int tickCount;
        private int trackCount;
        private double tickWidth = ViewConstants.TickWidthDefault;
        private double trackHeight = ViewConstants.TrackHeightDefault;
        private double tickOffset;
        private double trackOffset;
        private readonly ObservableAsPropertyHelper<double> viewportTicks;
        private readonly ObservableAsPropertyHelper<double> viewportTracks;
        private readonly ObservableAsPropertyHelper<double> smallChangeX;
        private readonly ObservableAsPropertyHelper<double> smallChangeY;

        public readonly List<UPart> SelectedParts = new List<UPart>();
        private readonly HashSet<UPart> TempSelectedParts = new HashSet<UPart>();

        public TracksViewModel() {
            viewportTicks = this.WhenAnyValue(x => x.Bounds, x => x.TickWidth)
                .Select(v => v.Item1.Width / v.Item2)
                .ToProperty(this, x => x.ViewportTicks);
            viewportTracks = this.WhenAnyValue(x => x.Bounds, x => x.TrackHeight)
                .Select(v => v.Item1.Height / v.Item2)
                .ToProperty(this, x => x.ViewportTracks);
            smallChangeX = this.WhenAnyValue(x => x.ViewportTicks)
                .Select(w => w / 8)
                .ToProperty(this, x => x.SmallChangeX);
            smallChangeY = this.WhenAnyValue(x => x.ViewportTracks)
                .Select(h => h / 8)
                .ToProperty(this, x => x.SmallChangeY);

            TrackCount = 10;
            TickCount = 480 * 100;

            DocManager.Inst.AddSubscriber(this);
        }

        public void OnXZoomed(Point position, double delta) {
            double tick = TickOffset;
            bool recenter = true;
            if (TickOffset == 0 && position.X < 0.1) {
                recenter = false;
            }
            double center = TickOffset + position.X * ViewportTicks;
            TickWidth *= 1.0 + delta;
            if (recenter) {
                tick = Math.Max(0, center - position.X * ViewportTicks);
            }
            if (TickOffset != tick) {
                TickOffset = tick;
            } else {
                // Force a redraw when only ViewportWidth is changed.
                TickOffset = tick + 1;
                TickOffset = tick - 1;
            }
        }

        public void OnYZoomed(Point position, double delta) {
            TrackHeight *= 1.0 + delta;
            double track = TrackOffset;
            TrackOffset = track + 1;
            TrackOffset = track - 1;
            TrackOffset = track;
        }

        public int PointToTick(Point point) {
            return (int)(point.X / TickWidth - TickOffset);
        }

        public int SnapUnit { get; private set; } = 480;
        public int PointToSnappedTick(Point point) {
            int tick = (int)(point.X / TickWidth + TickOffset);
            return (int)((double)tick / SnapUnit) * SnapUnit;
        }

        public int PointToTrackNo(Point point) {
            return (int)(point.Y / TrackHeight + TrackOffset);
        }

        public Point TickTrackToPoint(int tick, int trackNo) {
            return new Point(
                (tick - TickOffset) * TickWidth,
                (trackNo - TrackOffset) * TrackHeight);
        }

        public Size TickTrackToSize(int ticks, int tracks) {
            return new Size(ticks * TickWidth, tracks * TrackHeight);
        }

        public void AddTrack() {
            var project = DocManager.Inst.Project;
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new AddTrackCommand(project, new UTrack() { TrackNo = project.tracks.Count() }));
            DocManager.Inst.EndUndoGroup();
        }

        public UPart? MaybeAddPart(Point point) {
            int trackNo = PointToTrackNo(point);
            var project = DocManager.Inst.Project;
            if (trackNo >= project.tracks.Count) {
                return null;
            }
            UVoicePart part = new UVoicePart() {
                position = PointToSnappedTick(point), // todo: snap
                trackNo = trackNo,
                Duration = project.resolution * 16 / project.beatUnit * project.beatPerBar,
            };
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new AddPartCommand(project, part));
            DocManager.Inst.EndUndoGroup();
            return part;
        }

        public void DeselectParts() {
            SelectedParts.Clear();
            TempSelectedParts.Clear();
            MessageBus.Current.SendMessage(
                new PartsSelectionEvent(
                    SelectedParts.ToArray(), TempSelectedParts.ToArray()));
        }

        public void SelectAllParts() {
            var project = DocManager.Inst.Project;
            DeselectParts();
            SelectedParts.AddRange(project.parts);
            MessageBus.Current.SendMessage(
                new PartsSelectionEvent(
                    SelectedParts.ToArray(), TempSelectedParts.ToArray()));
        }

        public void TempSelectParts(int x0, int x1, int y0, int y1) {
            var project = DocManager.Inst.Project;
            TempSelectedParts.Clear();
            foreach (var part in project.parts) {
                if (part.EndTick >= x0 && part.position <= x1 && part.trackNo >= y0 && part.trackNo < y1) {
                    TempSelectedParts.Add(part);
                }
            }
            MessageBus.Current.SendMessage(
                new PartsSelectionEvent(
                    SelectedParts.ToArray(), TempSelectedParts.ToArray()));
        }

        public void CommitTempSelectParts() {
            var newSelection = SelectedParts.Union(TempSelectedParts).ToList();
            SelectedParts.Clear();
            SelectedParts.AddRange(newSelection);
            TempSelectedParts.Clear();
            MessageBus.Current.SendMessage(
                new PartsSelectionEvent(
                    SelectedParts.ToArray(), TempSelectedParts.ToArray()));
        }

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is PartCommand partCommand) {
                if (partCommand is AddPartCommand) {
                    if (!isUndo) {
                        Parts.Add(partCommand.part);
                    } else {
                        Parts.Remove(partCommand.part);
                    }
                } else if (partCommand is RemovePartCommand) {
                    if (!isUndo) {
                        Parts.Remove(partCommand.part);
                    } else {
                        Parts.Add(partCommand.part);
                    }
                } else if (partCommand is ReplacePartCommand replacePart) {
                    if (!isUndo) {
                        Parts.Remove(replacePart.part);
                        Parts.Add(replacePart.newPart);
                    } else {
                        Parts.Remove(replacePart.newPart);
                        Parts.Add(replacePart.part);
                    }
                } else if (partCommand is ResizePartCommand resizePart) {
                    MessageBus.Current.SendMessage(new PartResizeEvent(resizePart.part));
                } else if (partCommand is MovePartCommand movePart) {
                    MessageBus.Current.SendMessage(new PartMoveEvent(movePart.part));
                }
            } else if (cmd is TrackCommand) {
                if (cmd is AddTrackCommand addTrack) {
                    if (!isUndo) {
                        Tracks.Add(addTrack.track);
                    } else {
                        Tracks.Remove(addTrack.track);
                    }
                } else if (cmd is RemoveTrackCommand removeTrack) {
                    if (!isUndo) {
                        Tracks.Remove(removeTrack.track);
                    } else {
                        Tracks.Add(removeTrack.track);
                    }
                }
                MessageBus.Current.SendMessage(new TracksRefreshEvent());
            } else if (cmd is UNotification) {
                if (cmd is LoadProjectNotification loadProjectNotif) {
                    Parts.Clear();
                    Parts.AddRange(loadProjectNotif.project.parts);
                    Tracks.Clear();
                    Tracks.AddRange(loadProjectNotif.project.tracks);
                }
            }
        }
    }
}

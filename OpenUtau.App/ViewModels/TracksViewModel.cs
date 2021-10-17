using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using DynamicData.Binding;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class TracksRefreshEvent { }
    public class TracksSoloEvent {
        public readonly int trackNo;
        public readonly bool solo;
        public TracksSoloEvent(int trackNo, bool solo) {
            this.trackNo = trackNo;
            this.solo = solo;
        }
    }
    public class PartsSelectionEvent {
        public readonly UPart[] selectedParts;
        public readonly UPart[] tempSelectedParts;
        public PartsSelectionEvent(UPart[] selectedParts, UPart[] tempSelectedParts) {
            this.selectedParts = selectedParts;
            this.tempSelectedParts = tempSelectedParts;
        }
    }
    public class PartRefreshEvent {
        public readonly UPart part;
        public PartRefreshEvent(UPart part) { this.part = part; }
    }
    public class PartRedrawEvent {
        public readonly UPart part;
        public PartRedrawEvent(UPart part) { this.part = part; }
    }

    public class TracksViewModel : ViewModelBase, ICmdSubscriber {
        public UProject Project => DocManager.Inst.Project;
        [Reactive] public Rect Bounds { get; set; }
        public int TickCount => Math.Max(Project.BarTicks * 32, Project.EndTick);
        public int TrackCount => Math.Max(20, Project.tracks.Count + 1);
        [Reactive] public double TickWidth { get; set; }
        public double TrackHeightMin => ViewConstants.TrackHeightMin;
        public double TrackHeightMax => ViewConstants.TrackHeightMax;
        [Reactive] public double TrackHeight { get; set; }
        [Reactive] public double TickOffset { get; set; }
        [Reactive] public double TrackOffset { get; set; }
        [Reactive] public int SnapUnit { get; set; }
        public double SnapUnitWidth => snapUnitWidth.Value;
        [Reactive] public double PlayPosX { get; set; }
        [Reactive] public double PlayPosHighlightX { get; set; }
        public double ViewportTicks => viewportTicks.Value;
        public double ViewportTracks => viewportTracks.Value;
        public double SmallChangeX => smallChangeX.Value;
        public double SmallChangeY => smallChangeY.Value;
        public double HScrollBarMax => Math.Max(0, TickCount - ViewportTicks);
        public double VScrollBarMax => Math.Max(0, TrackCount - ViewportTracks);
        public ObservableCollectionExtended<UPart> Parts { get; } = new ObservableCollectionExtended<UPart>();
        public ObservableCollectionExtended<UTrack> Tracks { get; } = new ObservableCollectionExtended<UTrack>();

        private readonly ObservableAsPropertyHelper<double> snapUnitWidth;
        private readonly ObservableAsPropertyHelper<double> viewportTicks;
        private readonly ObservableAsPropertyHelper<double> viewportTracks;
        private readonly ObservableAsPropertyHelper<double> smallChangeX;
        private readonly ObservableAsPropertyHelper<double> smallChangeY;

        public readonly List<UPart> SelectedParts = new List<UPart>();
        private readonly HashSet<UPart> TempSelectedParts = new HashSet<UPart>();

        public TracksViewModel() {
            snapUnitWidth = this.WhenAnyValue(x => x.SnapUnit, x => x.TickWidth)
                .Select(v => v.Item1 * v.Item2)
                .ToProperty(this, v => v.SnapUnitWidth);
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
            this.WhenAnyValue(x => x.Bounds)
                .Subscribe(_ => {
                    OnXZoomed(new Point(), 0);
                    OnYZoomed(new Point(), 0);
                });

            this.WhenAnyValue(x => x.TickWidth)
                .Subscribe(tickWidth => {
                    int ticks = Project.resolution * 4 / Project.beatUnit;
                    double width = ticks * tickWidth;
                    if (width < ViewConstants.MinTicklineWidth) {
                        SnapUnit = ticks * Project.beatPerBar;
                        return;
                    }
                    while (width / 2 >= ViewConstants.MinTicklineWidth) {
                        width /= 2;
                        ticks /= 2;
                    }
                    SnapUnit = ticks;
                });
            this.WhenAnyValue(x => x.TickOffset)
                .Subscribe(tickOffset => {
                    SetPlayPos(DocManager.Inst.playPosTick, true);
                });

            TickWidth = ViewConstants.TickWidthDefault;
            TrackHeight = ViewConstants.TrackHeightDefault;
            Notify();

            DocManager.Inst.AddSubscriber(this);
        }

        public void OnXZoomed(Point position, double delta) {
            bool recenter = true;
            if (TickOffset == 0 && position.X < 0.1) {
                recenter = false;
            }
            double center = TickOffset + position.X * ViewportTicks;
            double tickWidth = TickWidth * (1.0 + delta * 2);
            tickWidth = Math.Clamp(tickWidth, ViewConstants.TickWidthMin, ViewConstants.TickWidthMax);
            tickWidth = Math.Max(tickWidth, Bounds.Width / TickCount);
            TickWidth = tickWidth;
            double tickOffset = recenter
                    ? center - position.X * ViewportTicks
                    : TickOffset;
            TickOffset = Math.Clamp(tickOffset, 0, HScrollBarMax);
            Notify();
        }

        public void OnYZoomed(Point position, double delta) {
            double trackHeight = TrackHeight * (1.0 + delta * 2);
            trackHeight = Math.Clamp(trackHeight, ViewConstants.TrackHeightMin, ViewConstants.TrackHeightMax);
            trackHeight = Math.Max(trackHeight, Bounds.Height / TrackCount);
            TrackHeight = trackHeight;
            TrackOffset = Math.Clamp(TrackOffset, 0, VScrollBarMax);
            Notify();
        }

        private void Notify() {
            this.RaisePropertyChanged(nameof(TickCount));
            this.RaisePropertyChanged(nameof(HScrollBarMax));
            this.RaisePropertyChanged(nameof(ViewportTicks));
            this.RaisePropertyChanged(nameof(TrackCount));
            this.RaisePropertyChanged(nameof(VScrollBarMax));
            this.RaisePropertyChanged(nameof(ViewportTracks));
        }

        public int PointToTick(Point point) {
            return (int)(point.X / TickWidth + TickOffset);
        }

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
                position = PointToSnappedTick(point),
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
                if (part.EndTick > x0 && part.position < x1 && part.trackNo >= y0 && part.trackNo < y1) {
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

        public void DeleteSelectedParts() {
            if (SelectedParts.Count <= 0) {
                return;
            }
            DocManager.Inst.StartUndoGroup();
            foreach (var part in SelectedParts) {
                DocManager.Inst.ExecuteCmd(new RemovePartCommand(Project, part));
            }
            DocManager.Inst.EndUndoGroup();
            DeselectParts();
        }

        private void SetPlayPos(int tick, bool noScroll = false) {
            double playPosX = TickTrackToPoint(tick, 0).X;
            double scroll = 0;
            if (!noScroll && playPosX > PlayPosX) {
                double margin = ViewConstants.PlayPosMarkerMargin * Bounds.Width;
                if (playPosX > margin) {
                    scroll = playPosX - margin;
                }
                TickOffset = Math.Clamp(TickOffset + scroll, 0, HScrollBarMax);
            }
            PlayPosX = playPosX;
            int highlightTick = (int)Math.Floor((double)tick / SnapUnit) * SnapUnit;
            PlayPosHighlightX = TickTrackToPoint(highlightTick, 0).X;
        }

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is NoteCommand noteCommand) {
                MessageBus.Current.SendMessage(new PartRedrawEvent(noteCommand.Part));
            } else if (cmd is PartCommand partCommand) {
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
                }
                MessageBus.Current.SendMessage(new PartRefreshEvent(partCommand.part));
                Notify();
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
                Notify();
                MessageBus.Current.SendMessage(new TracksRefreshEvent());
            } else if (cmd is UNotification) {
                if (cmd is LoadProjectNotification loadProjectNotif) {
                    Parts.Clear();
                    Parts.AddRange(loadProjectNotif.project.parts);
                    Tracks.Clear();
                    Tracks.AddRange(loadProjectNotif.project.tracks);
                    MessageBus.Current.SendMessage(new TracksRefreshEvent());
                } else if (cmd is SetPlayPosTickNotification setPlayPosTick) {
                    SetPlayPos(setPlayPosTick.playPosTick);
                }
                Notify();
            }
        }
    }
}

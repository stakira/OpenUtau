using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using DynamicData.Binding;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;

namespace OpenUtau.App.ViewModels {
    public class TracksRefreshEvent { }

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

        public void AddTrack() {
            var project = DocManager.Inst.Project;
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new AddTrackCommand(project, new UTrack() { TrackNo = project.tracks.Count() }));
            DocManager.Inst.EndUndoGroup();
        }

        public void MaybeAddPart(Point position) {
            double tick = position.X / TickWidth - TickOffset;
            int trackNo = (int)(position.Y / TrackHeight - TrackOffset);
            var project = DocManager.Inst.Project;
            if (trackNo >= project.tracks.Count) {
                return;
            }
            UVoicePart part = new UVoicePart() {
                position = (int)tick, // todo: snap
                trackNo = trackNo,
                Duration = project.resolution * 16 / project.beatUnit * project.beatPerBar,
            };
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new AddPartCommand(project, part));
            DocManager.Inst.EndUndoGroup();
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

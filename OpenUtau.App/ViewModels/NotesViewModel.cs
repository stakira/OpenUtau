using System;
using System.Numerics;
using System.Reactive.Linq;
using Avalonia;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class NotesViewModel : ViewModelBase, ICmdSubscriber {
        [Reactive] public Rect Bounds { get; set; }
        [Reactive] public int TickCount { get; set; }
        public int TrackCount => ViewConstants.MaxTone;
        public double TickWidth {
            get => tickWidth;
            set => this.RaiseAndSetIfChanged(ref tickWidth, Math.Clamp(value, ViewConstants.PianoRollTickWidthMin, ViewConstants.PianoRollTickWidthMax));
        }
        public double TrackHeightMin => ViewConstants.NoteHeightMin;
        public double TrackHeightMax => ViewConstants.NoteHeightMax;
        public double TrackHeight {
            get => trackHeight;
            set => this.RaiseAndSetIfChanged(ref trackHeight, Math.Clamp(value, ViewConstants.NoteHeightMin, ViewConstants.NoteHeightMax));
        }
        [Reactive] public double TickOrigin { get; set; }
        [Reactive] public double TickOffset { get; set; }
        [Reactive] public double TrackOffset { get; set; }
        [Reactive] public int SnapUnit { get; set; }
        [Reactive] public UPart? Part { get; set; }
        public double ViewportTicks => viewportTicks.Value;
        public double ViewportTracks => viewportTracks.Value;
        public double SmallChangeX => smallChangeX.Value;
        public double SmallChangeY => smallChangeY.Value;
        public UProject Project => DocManager.Inst.Project;

        private double tickWidth = ViewConstants.PianoRollTickWidthDefault;
        private double trackHeight = ViewConstants.NoteHeightDefault;
        private readonly ObservableAsPropertyHelper<double> viewportTicks;
        private readonly ObservableAsPropertyHelper<double> viewportTracks;
        private readonly ObservableAsPropertyHelper<double> smallChangeX;
        private readonly ObservableAsPropertyHelper<double> smallChangeY;

        public NotesViewModel() {
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

            this.WhenAnyValue(x => x.TickWidth)
                .Subscribe(tickWidth => {
                    int ticks = Project.resolution * 4 / Project.beatUnit;
                    double width = ticks * tickWidth;
                    while (width / 2 >= ViewConstants.PianoRollMinTicklineWidth && ticks % 2 == 0) {
                        width /= 2;
                        ticks /= 2;
                    }
                    SnapUnit = ticks;
                });

            TrackOffset = 4 * 12 + 6;

            DocManager.Inst.AddSubscriber(this);
        }

        public void OnXZoomed(Point position, double delta) {
            double tick = TickOffset;
            bool recenter = true;
            if (TickOffset == 0 && position.X < 0.1) {
                recenter = false;
            }
            double center = TickOffset + position.X * ViewportTicks;
            double before = TickWidth;
            TickWidth *= 1.0 + delta * 2;
            if (before == TickWidth) {
                return;
            }
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

        public int PointToSnappedTick(Point point) {
            int tick = (int)(point.X / TickWidth + TickOffset);
            return (int)((double)tick / SnapUnit) * SnapUnit;
        }

        public int PointToTone(Point point) {
            return (int)(ViewConstants.MaxTone - 1 - point.Y / TrackHeight - TrackOffset);
        }

        public Point TickToneToPoint(double tick, double tone) {
            return new Point(
                (tick - TickOffset) * TickWidth,
                (ViewConstants.MaxTone - 1 - tone - TrackOffset) * TrackHeight);
        }
        public Point TickToneToPoint(Vector2 tickTone) {
            return TickToneToPoint(tickTone.X, tickTone.Y);
        }

        public Size TickToneToSize(double ticks, double tone) {
            return new Size(ticks * TickWidth, tone * TrackHeight);
        }

        private void LoadPart(UPart part, UProject project) {
            if (!(part is UVoicePart)) {
                return;
            }
            UnloadPart();
            this.Part = part;
            OnPartModified();
        }

        private void UnloadPart() {
            Part = null;
        }

        private void OnPartModified() {
            if (Part == null) {
                return;
            }
            TickOrigin = Part.position;
            TickCount = Part.Duration;
        }

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is UNotification) {
                if (cmd is LoadPartNotification loadPart) {
                    LoadPart(loadPart.part, loadPart.project);
                } else if (cmd is LoadProjectNotification) {
                    UnloadPart();
                }
            } else if (cmd is PartCommand partCommand) {
                if (cmd is ReplacePartCommand replacePart) {
                    if (!isUndo) {
                        LoadPart(replacePart.part, replacePart.project);
                    } else {
                        LoadPart(replacePart.newPart, replacePart.project);
                    }
                }
                if (partCommand.part != Part) {
                    return;
                }
                if (cmd is RemovePartCommand) {
                    if (!isUndo) {
                        UnloadPart();
                    }
                } else if (cmd is ResizePartCommand) {
                    OnPartModified();
                } else if (cmd is MovePartCommand) {
                    OnPartModified();
                }
            }
        }
    }
}

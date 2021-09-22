using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using OpenUtau.Core;
using ReactiveUI;

namespace OpenUtau.App.ViewModels {
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
        public double Tick {
            get => tick;
            set => this.RaiseAndSetIfChanged(ref tick, value);
        }
        public double Track {
            get => track;
            set => this.RaiseAndSetIfChanged(ref track, value);
        }
        public double ViewportTicks => viewportTicks.Value;
        public double ViewportTracks => viewportTracks.Value;
        public double SmallChangeX => smallChangeX.Value;
        public double SmallChangeY => smallChangeY.Value;

        private Rect bounds;
        private int tickCount;
        private int trackCount;
        private double tickWidth = ViewConstants.TickWidthDefault;
        private double trackHeight = ViewConstants.TrackHeightDefault;
        private double tick;
        private double track;
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
            double tick = Tick;
            bool recenter = true;
            if (Tick == 0 && position.X < 0.1) {
                recenter = false;
            }
            double center = Tick + position.X * ViewportTicks;
            TickWidth *= 1.0 + delta;
            if (recenter) {
                tick = Math.Max(0, center - position.X * ViewportTicks);
            }
            if (Tick != tick) {
                Tick = tick;
            } else {
                // Force a redraw when only ViewportWidth is changed.
                Tick = tick + 1;
                Tick = tick - 1;
            }
        }

        public void OnYZoomed(Point position, double delta) {
            TrackHeight *= 1.0 + delta;
            double track = Track;
            Track = track + 1;
            Track = track - 1;
            Track = track;
        }

        public void OnNext(UCommand cmd, bool isUndo) { }
    }
}

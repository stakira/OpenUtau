using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using ReactiveUI;

namespace OpenUtau.App.ViewModels {
    public class TracksViewModel : ViewModelBase {
        public double TickWidth {
            get => tickWidth;
            set => this.RaiseAndSetIfChanged(ref tickWidth, Math.Clamp(value, ViewConstants.TickWidthMin, ViewConstants.TickWidthMax));
        }
        public double TrackHeight {
            get => trackHeight;
            set => this.RaiseAndSetIfChanged(ref trackHeight, Math.Clamp(value, ViewConstants.TrackHeightMin, ViewConstants.TrackHeightMax));
        }
        private double totalWidth;
        public double TotalWidth {
            get => totalWidth;
            set => this.RaiseAndSetIfChanged(ref totalWidth, value);
        }

        private double totalHeight;
        public double TotalHeight {
            get => totalHeight;
            set => this.RaiseAndSetIfChanged(ref totalHeight, value);
        }

        private double viewportWidth;
        public double ViewportWidth {
            get => viewportWidth;
            set => this.RaiseAndSetIfChanged(ref viewportWidth, value);
        }

        private double viewportHeight;
        public double ViewportHeight {
            get => viewportHeight;
            set => this.RaiseAndSetIfChanged(ref viewportHeight, value);
        }

        private double viewportX;
        public double ViewportX {
            get => viewportX;
            set => this.RaiseAndSetIfChanged(ref viewportX, value);
        }

        private double viewportY;
        public double ViewportY {
            get => viewportY;
            set => this.RaiseAndSetIfChanged(ref viewportY, value);
        }

        readonly ObservableAsPropertyHelper<double> smallChangeX;
        public double SmallChangeX => smallChangeX.Value;

        readonly ObservableAsPropertyHelper<double> smallChangeY;
        public double SmallChangeY => smallChangeY.Value;

        private double tickWidth = ViewConstants.TickWidthDefault;
        private double trackHeight = ViewConstants.TrackHeightDefault;

        public TracksViewModel() {
            smallChangeX = this.WhenAnyValue(x => x.ViewportWidth)
                .Select(w => w / 8)
                .ToProperty(this, x => x.SmallChangeX);
            smallChangeY = this.WhenAnyValue(x => x.ViewportHeight)
                .Select(h => h / 8)
                .ToProperty(this, x => x.SmallChangeY);

            TotalWidth = 2000;
            TotalHeight = 2000;
            ViewportWidth = 500;
            ViewportHeight = 500;
            ViewportY = TotalHeight / 2;
        }

        public void OnXZoomed(Point position, double delta) {
            double zoomCenter = ViewportX == 0 && position.X < 0.1
                ? 0
                : ViewportX + position.X * ViewportWidth;
            double width = ViewportWidth * (1.0 + delta);
            double x = Math.Clamp(zoomCenter - position.X * width, 0, TotalWidth);
            ViewportX = x;
            ViewportWidth = width;
        }

        public void OnYZoomed(Point position, double delta) {
            double zoomCenter = ViewportY + position.Y * ViewportHeight;
            double height = ViewportHeight * (1.0 + delta);
            double y = Math.Clamp(zoomCenter - position.Y * height, 0, TotalHeight);
            ViewportY = y;
            ViewportHeight = height;
        }
    }
}

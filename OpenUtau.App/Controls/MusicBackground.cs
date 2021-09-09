using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using OpenUtau.Core;

namespace OpenUtau.App.Controls {
    class MusicBackground : Control {
        public static readonly DirectProperty<MusicBackground, int> BeatUnitProperty =
            AvaloniaProperty.RegisterDirect<MusicBackground, int>(
                nameof(BeatUnit),
                o => o.BeatUnit);
        public static readonly DirectProperty<MusicBackground, int> BeatPerBarProperty =
            AvaloniaProperty.RegisterDirect<MusicBackground, int>(
                nameof(BeatPerBar),
                o => o.BeatPerBar);
        public static readonly DirectProperty<MusicBackground, int> ResolutionProperty =
            AvaloniaProperty.RegisterDirect<MusicBackground, int>(
                nameof(Resolution),
                o => o.Resolution);
        public static readonly DirectProperty<MusicBackground, int> TickOffsetProperty =
            AvaloniaProperty.RegisterDirect<MusicBackground, int>(
                nameof(TickOffset),
                o => o.TickOffset);
        public static readonly DirectProperty<MusicBackground, double> TickWidthProperty =
            AvaloniaProperty.RegisterDirect<MusicBackground, double>(
                nameof(TickWidth),
                o => o.TickWidth);
        public static readonly DirectProperty<MusicBackground, double> TrackHeightProperty =
            AvaloniaProperty.RegisterDirect<MusicBackground, double>(
                nameof(TrackHeight),
                o => o.TrackHeight);
        public static readonly DirectProperty<MusicBackground, double> OffsetXProperty =
            AvaloniaProperty.RegisterDirect<MusicBackground, double>(
                nameof(OffsetX),
                o => o.OffsetX);
        public static readonly DirectProperty<MusicBackground, double> OffsetYProperty =
            AvaloniaProperty.RegisterDirect<MusicBackground, double>(
                nameof(OffsetY),
                o => o.OffsetY);

        public int BeatUnit {
            get { return _beatUnit; }
            private set { SetAndRaise(BeatUnitProperty, ref _beatUnit, value); }
        }
        public int BeatPerBar {
            get { return _beatPerBar; }
            private set { SetAndRaise(BeatPerBarProperty, ref _beatPerBar, value); }
        }
        public int Resolution {
            get { return _resolution; }
            private set { SetAndRaise(ResolutionProperty, ref _resolution, value); }
        }
        public int TickOffset {
            get { return _tickOffset; }
            private set { SetAndRaise(TickOffsetProperty, ref _tickOffset, value); }
        }
        public double TickWidth {
            get { return _tickWidth; }
            private set { SetAndRaise(TickWidthProperty, ref _tickWidth, value); }
        }
        public double TrackHeight {
            get { return _trackHeight; }
            private set { SetAndRaise(TrackHeightProperty, ref _trackHeight, value); }
        }
        public double OffsetX {
            get { return _offsetX; }
            private set { SetAndRaise(OffsetXProperty, ref _offsetX, value); }
        }
        public double OffsetY {
            get { return _offsetY; }
            private set { SetAndRaise(OffsetYProperty, ref _offsetY, value); }
        }

        private int _beatPerBar = 4;
        private int _beatUnit = 4;
        private int _resolution = 480;
        private int _tickOffset;
        private double _tickWidth = 10;
        private double _trackHeight = 10;
        private double _offsetX;
        private double _offsetY;

        private readonly Pen pen1;
        private readonly Pen pen2;
        private readonly Pen pen3;

        public MusicBackground() {
            //pen1 = new Pen(ThemeManager.TickLineBrushDark, 1);
            //pen2 = new Pen(ThemeManager.TickLineBrushLight, 1);
            //pen3 = new Pen(ThemeManager.TickLineBrushLight, 1) { DashStyle = new DashStyle(UIConstants.DashLineArray, 0) };

            pen1 = new Pen(new SolidColorBrush(Colors.Black), 1);
            pen2 = new Pen(new SolidColorBrush(Colors.Black), 1);
            pen3 = new Pen(new SolidColorBrush(Colors.Black), 1) { DashStyle = UIConstants.TickDashStyle };
        }

        public override void Render(DrawingContext context) {
            context.DrawRectangle(pen1, new Rect(0, 0, 10, 10));
            RenderTracks(context);
            RenderTicks(context);
        }

        private void RenderTicks(DrawingContext context) {
            int TickMode = 1;
            double quarterWidth = TickWidth * Resolution;
            double quarterOffset = TickOffset * Resolution;
            double zoomRatio = MusicMath.getZoomRatio(quarterWidth, BeatPerBar, BeatUnit, /*MinTickWidth*/ 5);
            double interval = zoomRatio * quarterWidth;
            int tick = (int)((OffsetX + quarterOffset * quarterWidth) / interval) + 1;
            double left = tick * interval - OffsetX - quarterOffset * quarterWidth;

            while (left < Width) {
                double snappedLeft = Math.Round(left) + 0.5;
                if ((tick * zoomRatio * BeatUnit) % (BeatPerBar * 4) == 0) {
                    context.DrawLine(pen1, new Point(snappedLeft, -0.5), new Point(snappedLeft, Height + 0.5));
                } else if ((tick * zoomRatio * BeatUnit) % 4 == 0) {
                    if (TickMode == 1)
                        context.DrawLine(pen1, new Point(snappedLeft, -0.5), new Point(snappedLeft, Height + 0.5));
                    else
                        context.DrawLine(pen2, new Point(snappedLeft, -0.5), new Point(snappedLeft, Height + 0.5));
                } else if ((tick * zoomRatio * BeatUnit) % 1 == 0) {
                    if (TickMode == 1)
                        context.DrawLine(pen2, new Point(snappedLeft, -0.5), new Point(snappedLeft, Height + 0.5));
                    else
                        context.DrawLine(pen3, new Point(snappedLeft, -0.5), new Point(snappedLeft, Height + 0.5));
                } else {
                    context.DrawLine(pen3, new Point(snappedLeft, -0.5), new Point(snappedLeft, Height + 0.5));
                }
                left += interval;
                tick++;
            }
        }

        private void RenderTracks(DrawingContext context) {
            int firstTrack = (int)(OffsetY / TrackHeight);
            double top = TrackHeight * firstTrack - OffsetY;
            while (top < Height) {
                bool alt = firstTrack % 2 == 1;
                var brush = alt ? ThemeManager.TrackBackgroundBrushAlt : ThemeManager.TrackBackgroundBrush;
                context.DrawRectangle(
                    brush,
                    null,
                    new Rect(0, (int)top, Width, TrackHeight));
                top += TrackHeight;
                alt = !alt;
            }
        }
    }
}

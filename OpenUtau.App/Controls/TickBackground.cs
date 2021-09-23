using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace OpenUtau.App.Controls {
    class TickBackground : TemplatedControl {
        private static readonly IDashStyle DashStyle = new ImmutableDashStyle(new double[] { 2, 4 }, 0);
        readonly Dictionary<int, FormattedText> fTextPool = new Dictionary<int, FormattedText>();

        public static readonly DirectProperty<TickBackground, int> BeatPerBarProperty =
            AvaloniaProperty.RegisterDirect<TickBackground, int>(
                nameof(BeatPerBar),
                o => o.BeatPerBar,
                (o, v) => o.BeatPerBar = v);
        public static readonly DirectProperty<TickBackground, int> BeatUnitProperty =
            AvaloniaProperty.RegisterDirect<TickBackground, int>(
                nameof(BeatUnit),
                o => o.BeatUnit,
                (o, v) => o.BeatUnit = v);
        public static readonly DirectProperty<TickBackground, int> ResolutionProperty =
            AvaloniaProperty.RegisterDirect<TickBackground, int>(
                nameof(Resolution),
                o => o.Resolution,
                (o, v) => o.Resolution = v);
        public static readonly DirectProperty<TickBackground, double> TickWidthProperty =
            AvaloniaProperty.RegisterDirect<TickBackground, double>(
                nameof(TickWidth),
                o => o.TickWidth,
                (o, v) => o.TickWidth = v);
        public static readonly DirectProperty<TickBackground, double> TickOffsetProperty =
            AvaloniaProperty.RegisterDirect<TickBackground, double>(
                nameof(TickOffset),
                o => o.TickOffset,
                (o, v) => o.TickOffset = v);
        public static readonly DirectProperty<TickBackground, int> TickOriginProperty =
            AvaloniaProperty.RegisterDirect<TickBackground, int>(
                nameof(TickOrigin),
                o => o.TickOrigin,
                (o, v) => o.TickOrigin = v);

        public int BeatPerBar {
            get => _beatPerBar;
            private set => SetAndRaise(BeatPerBarProperty, ref _beatPerBar, value);
        }
        public int BeatUnit {
            get => _beatUnit;
            private set => SetAndRaise(BeatUnitProperty, ref _beatUnit, value);
        }
        public int Resolution {
            get => _resolution;
            private set => SetAndRaise(ResolutionProperty, ref _resolution, value);
        }
        // Tick width in pixel.
        public double TickWidth {
            get => _tickWidth;
            private set => SetAndRaise(TickWidthProperty, ref _tickWidth, value);
        }
        public double TickOffset {
            get => _tickOffset;
            private set => SetAndRaise(TickOffsetProperty, ref _tickOffset, value);
        }
        public int TickOrigin {
            get => _tickOrigin;
            private set => SetAndRaise(TickOriginProperty, ref _tickOrigin, value);
        }

        private int _beatUnit = 4;
        private int _beatPerBar = 4;
        private int _resolution = 480;
        private double _tickWidth;
        private double _tickOffset;
        private int _tickOrigin;

        private Pen pen1;
        private Pen pen2;
        private Pen pen3;

        public TickBackground() {
            pen1 = new Pen(Foreground, 1);
            pen2 = new Pen(Foreground, 1);
            pen3 = new Pen(Foreground, 1) {
                DashStyle = DashStyle,
            };
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change) {
            base.OnPropertyChanged(change);
            if (!change.IsEffectiveValueChange) {
                return;
            }
            if (change.Property == ForegroundProperty) {
                pen1 = new Pen(Foreground, 1);
                pen2 = new Pen(Foreground, 1);
                pen3 = new Pen(Foreground, 1) {
                    DashStyle = DashStyle,
                };
            }
            if (change.Property == BeatPerBarProperty ||
                change.Property == BeatUnitProperty ||
                change.Property == ResolutionProperty ||
                change.Property == TickOriginProperty ||
                change.Property == TickWidthProperty ||
                change.Property == TickOffsetProperty) {
                InvalidateVisual();
            }
        }

        public override void Render(DrawingContext context) {
            if (TickWidth == 0) {
                return;
            }
            int beatTicks = Resolution * 4 / BeatUnit;
            double beatWidth = TickWidth * beatTicks;
            double pixelOffset = (TickOffset + TickOrigin) * TickWidth;
            int beat = (int)((TickOffset + TickOrigin) / beatTicks);
            for (; beat * beatWidth - pixelOffset < Bounds.Width; ++beat) {
                double x = Math.Round(beat * beatWidth - pixelOffset) + 0.5;
                var pen = pen3;
                if (beat % BeatPerBar == 0) {
                    pen = pen1;
                    int bar = beat / BeatPerBar + 1;
                    if (!fTextPool.TryGetValue(bar, out var formattedText)) {
                        formattedText = new FormattedText(
                            bar.ToString(),
                            new Typeface(TextBlock.GetFontFamily(this)),
                            12,
                            TextAlignment.Left,
                            TextWrapping.NoWrap,
                            new Size(beatWidth, 20));
                        fTextPool.Add(bar, formattedText);
                    }
                    context.DrawText(Foreground, new Point(x + 3, 8), formattedText);
                }
                context.DrawLine(pen, new Point(x, -0.5), new Point(x, Bounds.Height + 0.5f));
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using ReactiveUI;

namespace OpenUtau.App.Controls {
    class TickBackground : TemplatedControl {
        private static readonly IDashStyle DashStyle = new ImmutableDashStyle(new double[] { 2, 4 }, 0);

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
        public static readonly DirectProperty<TickBackground, int> SnapUnitProperty =
            AvaloniaProperty.RegisterDirect<TickBackground, int>(
                nameof(SnapUnit),
                o => o.SnapUnit,
                (o, v) => o.SnapUnit = v);
        public static readonly DirectProperty<TickBackground, bool> IsPianoRollProperty =
            AvaloniaProperty.RegisterDirect<TickBackground, bool>(
                nameof(IsPianoRoll),
                o => o.IsPianoRoll,
                (o, v) => o.IsPianoRoll = v);
        public static readonly DirectProperty<TickBackground, bool> ShowBarNumberProperty =
            AvaloniaProperty.RegisterDirect<TickBackground, bool>(
                nameof(ShowBarNumber),
                o => o.ShowBarNumber,
                (o, v) => o.ShowBarNumber = v);

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
        public int SnapUnit {
            get => _snapUnit;
            set => SetAndRaise(SnapUnitProperty, ref _snapUnit, value);
        }
        public bool IsPianoRoll {
            get => _isPianoRoll;
            set => SetAndRaise(IsPianoRollProperty, ref _isPianoRoll, value);
        }
        public bool ShowBarNumber {
            get => _showBarNumber;
            set => SetAndRaise(ShowBarNumberProperty, ref _showBarNumber, value);
        }

        private int _beatUnit = 4;
        private int _beatPerBar = 4;
        private int _resolution = 480;
        private double _tickWidth;
        private double _tickOffset;
        private int _tickOrigin;
        private int _snapUnit;
        private bool _isPianoRoll;
        private bool _showBarNumber;

        private Pen penBar;
        private Pen penBeatUnit;
        private Pen penDanshed;

        public TickBackground() {
            penBar = new Pen(Foreground, 1);
            penBeatUnit = new Pen(Background, 1);
            penDanshed = new Pen(Background, 1) {
                DashStyle = DashStyle,
            };
            MessageBus.Current.Listen<ThemeChangedEvent>()
                .Subscribe(e => InvalidateVisual());
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change) {
            base.OnPropertyChanged(change);
            if (!change.IsEffectiveValueChange) {
                return;
            }
            if (change.Property == ForegroundProperty) {
                penBar = new Pen(Foreground, 1);
            }
            if (change.Property == BackgroundProperty) {
                penBeatUnit = new Pen(Background, 1);
                penDanshed = new Pen(Background, 1) {
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
            if (TickWidth <= 0 || SnapUnit <= 0 || BeatUnit <= 0) {
                return;
            }
            int beatUnitTicks = Resolution * 4 / BeatUnit;
            int barTicks = beatUnitTicks * BeatPerBar;
            int snapUnitIndex = (int)((TickOffset + TickOrigin) / SnapUnit);
            double pixelOffset = (TickOffset + TickOrigin) * TickWidth;
            for (; snapUnitIndex * SnapUnit * TickWidth - pixelOffset < Bounds.Width; ++snapUnitIndex) {
                int tick = snapUnitIndex * SnapUnit;
                double x = Math.Round(tick * TickWidth - pixelOffset) + 0.5;
                double y = ShowBarNumber ? 24.5 : -0.5;
                var pen = penDanshed;
                if (tick % barTicks == 0) {
                    pen = penBar;
                    if (ShowBarNumber) {
                        y = -0.5;
                        int bar = tick / barTicks + 1;
                        var textLayout = TextLayoutCache.Get(bar.ToString(), ThemeManager.BarNumberBrush, 12);
                        using (var state = context.PushPreTransform(Matrix.CreateTranslation(x + 3, 8))) {
                            textLayout.Draw(context);
                        }
                    }
                } else if (tick % beatUnitTicks == 0) {
                    pen = penBeatUnit;
                }
                context.DrawLine(pen, new Point(x, y), new Point(x, Bounds.Height + 0.5f));
            }
        }
    }
}

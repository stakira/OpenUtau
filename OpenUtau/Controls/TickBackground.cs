using System;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using OpenUtau.App.ViewModels;
using ReactiveUI;

namespace OpenUtau.App.Controls {
    class TickBackground : TemplatedControl {
        private static readonly IDashStyle DashStyle = new ImmutableDashStyle(new double[] { 2, 4 }, 0);

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
        public static readonly DirectProperty<TickBackground, bool> ShowBarNumberProperty =
            AvaloniaProperty.RegisterDirect<TickBackground, bool>(
                nameof(ShowBarNumber),
                o => o.ShowBarNumber,
                (o, v) => o.ShowBarNumber = v);

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
        public bool ShowBarNumber {
            get => _showBarNumber;
            set => SetAndRaise(ShowBarNumberProperty, ref _showBarNumber, value);
        }

        private int _resolution = 480;
        private double _tickWidth;
        private double _tickOffset;
        private int _tickOrigin;
        private int _snapUnit;
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
            MessageBus.Current.Listen<TimeAxisChangedEvent>()
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
            if (change.Property == ResolutionProperty ||
                change.Property == TickOriginProperty ||
                change.Property == TickWidthProperty ||
                change.Property == TickOffsetProperty ||
                change.Property == SnapUnitProperty) {
                InvalidateVisual();
            }
        }

        public override void Render(DrawingContext context) {
            if (TickWidth <= 0 || SnapUnit <= 0) {
                return;
            }
            int snapUnit = SnapUnit;
            while (snapUnit * TickWidth < ViewConstants.MinTicklineWidth / 2) {
                // Avoid drawing too dense.
                snapUnit *= 2;
            }
            var project = Core.DocManager.Inst.Project;
            double pixelOffset = (TickOffset + TickOrigin) * TickWidth;
            double leftTick = TickOffset + TickOrigin;
            double rightTick = TickOffset + TickOrigin + Bounds.Width / TickWidth;

            project.timeAxis.TickPosToBarBeat((int)leftTick, out int bar, out int beat, out int remainingTicks);
            int barTick = project.timeAxis.BarBeatToTickPos(bar++, 0);
            while (barTick < rightTick) {
                // Bar lines and numbers.
                double x = Math.Round(barTick * TickWidth - pixelOffset) + 0.5;
                double y = -0.5;
                var textLayout = TextLayoutCache.Get(bar.ToString(), ThemeManager.BarNumberBrush, 10);
                using (var state = context.PushPreTransform(Matrix.CreateTranslation(x + 3, 10))) {
                    textLayout.Draw(context);
                }
                context.DrawLine(penBar, new Point(x, y), new Point(x, Bounds.Height + 0.5f));
                // Lines between bars.
                int nextBarTick = project.timeAxis.BarBeatToTickPos(bar++, 0);
                for (int tick = barTick + snapUnit; tick < nextBarTick; tick += snapUnit) {
                    project.timeAxis.TickPosToBarBeat(tick, out int snapBar, out int snapBeat, out int snapRemainingTicks);
                    var pen = snapRemainingTicks != 0 ? penDanshed : penBeatUnit;
                    x = Math.Round(tick * TickWidth - pixelOffset) + 0.5;
                    y = 24;
                    context.DrawLine(pen, new Point(x, y), new Point(x, Bounds.Height + 0.5f));
                }
                barTick = nextBarTick;
            }
            foreach (var tempo in project.tempos) {
                double x = Math.Round(tempo.position * TickWidth - pixelOffset) + 0.5;
                var textLayout = TextLayoutCache.Get(tempo.bpm.ToString("#0.00"), ThemeManager.BarNumberBrush, 10);
                using (var state = context.PushPreTransform(Matrix.CreateTranslation(x + 3, 0))) {
                    textLayout.Draw(context);
                }
            }
            foreach (var timeSig in project.timeSignatures) {
                int tick = project.timeAxis.BarBeatToTickPos(timeSig.barPosition, 0);
                var barTextLayout = TextLayoutCache.Get((timeSig.barPosition + 1).ToString(), ThemeManager.BarNumberBrush, 10);
                double x = Math.Round(tick * TickWidth - pixelOffset) + 0.5 + barTextLayout.Size.Width + 4;
                var textLayout = TextLayoutCache.Get($"{timeSig.beatPerBar}/{timeSig.beatUnit}", ThemeManager.BarNumberBrush, 10);
                using (var state = context.PushPreTransform(Matrix.CreateTranslation(x + 3, 10))) {
                    textLayout.Draw(context);
                }
            }
        }
    }
}

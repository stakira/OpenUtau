using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using OpenUtau.App.ViewModels;
using OpenUtau.Core.Ustx;
using ReactiveUI;


namespace OpenUtau.App.Controls {

    class RemarkCanvas : Control {
        public static readonly DirectProperty<RemarkCanvas, IBrush> BackgroundProperty =
            AvaloniaProperty.RegisterDirect<RemarkCanvas, IBrush>(
                nameof(Background),
                o => o.Background,
                (o, v) => o.Background = v);
        public static readonly DirectProperty<RemarkCanvas, double> TickWidthProperty =
            AvaloniaProperty.RegisterDirect<RemarkCanvas, double>(
                nameof(TickWidth),
                o => o.TickWidth,
                (o, v) => o.TickWidth = v);
        public static readonly DirectProperty<RemarkCanvas, double> TickOffsetProperty =
            AvaloniaProperty.RegisterDirect<RemarkCanvas, double>(
                nameof(TickOffset),
                o => o.TickOffset,
                (o, v) => o.TickOffset = v);
        public static readonly DirectProperty<RemarkCanvas, UVoicePart?> PartProperty =
            AvaloniaProperty.RegisterDirect<RemarkCanvas, UVoicePart?>(
                nameof(Part),
                o => o.Part,
                (o, v) => o.Part = v);

        public IBrush Background {
            get => background;
            private set => SetAndRaise(BackgroundProperty, ref background, value);
        }
        public double TickWidth {
            get => tickWidth;
            private set => SetAndRaise(TickWidthProperty, ref tickWidth, value);
        }
        public double TickOffset {
            get => tickOffset;
            private set => SetAndRaise(TickOffsetProperty, ref tickOffset, value);
        }
        public UVoicePart? Part {
            get => part;
            set => SetAndRaise(PartProperty, ref part, value);
        }
        private IBrush background = Brushes.White;
        private double tickWidth;
        private double tickOffset;
        private UVoicePart? part;
        private PathGeometry remarkGeometry;

        public RemarkCanvas() {
            ClipToBounds = true;

            remarkGeometry = new PathGeometry();
            PathFigure figure = new PathFigure() {
                StartPoint = new Point(-6, -6),
                IsClosed = true,
                IsFilled = true,
            };
            figure.Segments?.Add(new LineSegment() { Point = new Point(6, -6) });
            figure.Segments?.Add(new LineSegment() { Point = new Point(6, 2) });
            figure.Segments?.Add(new LineSegment() { Point = new Point(3, 2) });
            figure.Segments?.Add(new LineSegment() { Point = new Point(0, 6) });
            figure.Segments?.Add(new LineSegment() { Point = new Point(-3, 2) });
            figure.Segments?.Add(new LineSegment() { Point = new Point(-6, 2) });
            remarkGeometry.Figures?.Add(figure);

            MessageBus.Current.Listen<NotesRefreshEvent>()
                .Subscribe(_ => InvalidateVisual());
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
            base.OnPropertyChanged(change);
            InvalidateVisual();
        }

        public override void Render(DrawingContext context) {
            base.Render(context);
            if (Part == null) {
                return;
            }
            if(Part.remarks == null) {
                return;
            }
            var viewModel = ((PianoRollViewModel?)DataContext)?.NotesViewModel;
            if (viewModel == null) {
                return;
            }

            //DrawBackgroundForHitTest(context);
            double leftTick = TickOffset - 480;
            double rightTick = TickOffset + Bounds.Width / TickWidth + 480;

            for (int i = 0; i < Part.remarks.Count; i++) {
                var remark = Part.remarks[i];

                if (remark.position < leftTick || remark.position > rightTick) {
                    continue;
                }

                double x = viewModel.TickToneToPoint(remark.position, 0).X;
                double y = 10;

                IBrush brush = Brushes.Red;
                try {
                    brush = Brush.Parse(remark.color);
                } catch (FormatException) {
                    
                }

                using (var state = context.PushTransform(Matrix.CreateTranslation(x, y))) {
                    context.DrawGeometry(brush, null, remarkGeometry);
                }
            }
        }

        private void DrawBackgroundForHitTest(DrawingContext context) {
            context.DrawRectangle(Background, null, Bounds.WithX(0).WithY(0));
        }
    }
}

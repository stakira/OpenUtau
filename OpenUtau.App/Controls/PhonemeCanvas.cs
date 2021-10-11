using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using OpenUtau.App.ViewModels;
using OpenUtau.Core.Ustx;
using ReactiveUI;

namespace OpenUtau.App.Controls {
    class PhonemeCanvas : Canvas {
        public static readonly DirectProperty<PhonemeCanvas, double> TickWidthProperty =
            AvaloniaProperty.RegisterDirect<PhonemeCanvas, double>(
                nameof(TickWidth),
                o => o.TickWidth,
                (o, v) => o.TickWidth = v);
        public static readonly DirectProperty<PhonemeCanvas, double> TickOffsetProperty =
            AvaloniaProperty.RegisterDirect<PhonemeCanvas, double>(
                nameof(TickOffset),
                o => o.TickOffset,
                (o, v) => o.TickOffset = v);
        public static readonly DirectProperty<PhonemeCanvas, UVoicePart?> PartProperty =
            AvaloniaProperty.RegisterDirect<PhonemeCanvas, UVoicePart?>(
                nameof(Part),
                o => o.Part,
                (o, v) => o.Part = v);
        public static readonly DirectProperty<PhonemeCanvas, bool> ShowPhonemeProperty =
            AvaloniaProperty.RegisterDirect<PhonemeCanvas, bool>(
                nameof(ShowPhoneme),
                o => o.ShowPhoneme,
                (o, v) => o.ShowPhoneme = v);

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
        public bool ShowPhoneme {
            get => showPhoneme;
            private set => SetAndRaise(ShowPhonemeProperty, ref showPhoneme, value);
        }

        private double tickWidth;
        private double tickOffset;
        private UVoicePart? part;
        private bool showPhoneme = true;

        private HashSet<UNote> selectedNotes = new HashSet<UNote>();
        private Geometry pointGeometry;

        public PhonemeCanvas() {
            ClipToBounds = true;
            pointGeometry = new EllipseGeometry(new Rect(-2.5, -2.5, 5, 5));
            MessageBus.Current.Listen<NotesRefreshEvent>()
                .Subscribe(_ => InvalidateVisual());
            MessageBus.Current.Listen<NotesSelectionEvent>()
                .Subscribe(e => {
                    selectedNotes.Clear();
                    selectedNotes.UnionWith(e.selectedNotes);
                    selectedNotes.UnionWith(e.tempSelectedNotes);
                    InvalidateVisual();
                });
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change) {
            base.OnPropertyChanged(change);
            if (!change.IsEffectiveValueChange) {
                return;
            }
            InvalidateVisual();
        }

        public override void Render(DrawingContext context) {
            base.Render(context);
            if (Part == null || !ShowPhoneme) {
                return;
            }
            var viewModel = ((PianoRollViewModel?)DataContext)?.NotesViewModel;
            if (viewModel == null) {
                return;
            }
            double leftTick = TickOffset - 480;
            double rightTick = TickOffset + Bounds.Width / TickWidth + 480;
            foreach (var note in Part.notes) {
                if (note.LeftBound >= rightTick || note.RightBound <= leftTick) {
                    continue;
                }
                RenderPhonemes(note, viewModel, context);
            }
        }

        private void RenderPhonemes(UNote note, NotesViewModel viewModel, DrawingContext context) {

            const double y = 23.5;
            const double height = 24;
            for (int i = 0; i < note.phonemes.Count; i++) {
                var phoneme = note.phonemes[i];
                if (note.OverlapError) {
                    continue;
                }

                int position = note.position + phoneme.position;

                double x = Math.Round(viewModel.TickToneToPoint(position, 0).X) + 0.5;
                if (!phoneme.Error) {
                    double x0 = viewModel.TickToneToPoint(position + viewModel.Project.MillisecondToTick(phoneme.envelope.data[0].X), 0).X;
                    double y0 = (1 - phoneme.envelope.data[0].Y / 100) * height;
                    double x1 = viewModel.TickToneToPoint(position + viewModel.Project.MillisecondToTick(phoneme.envelope.data[1].X), 0).X;
                    double y1 = (1 - phoneme.envelope.data[1].Y / 100) * height;
                    double x2 = viewModel.TickToneToPoint(position + viewModel.Project.MillisecondToTick(phoneme.envelope.data[2].X), 0).X;
                    double y2 = (1 - phoneme.envelope.data[2].Y / 100) * height;
                    double x3 = viewModel.TickToneToPoint(position + viewModel.Project.MillisecondToTick(phoneme.envelope.data[3].X), 0).X;
                    double y3 = (1 - phoneme.envelope.data[3].Y / 100) * height;
                    double x4 = viewModel.TickToneToPoint(position + viewModel.Project.MillisecondToTick(phoneme.envelope.data[4].X), 0).X;
                    double y4 = (1 - phoneme.envelope.data[4].Y / 100) * height;

                    var pen = selectedNotes.Contains(note) ? ThemeManager.AccentPen2 : ThemeManager.AccentPen1;
                    var brush = selectedNotes.Contains(note) ? ThemeManager.AccentBrush2Semi : ThemeManager.AccentBrush1Semi;

                    var point0 = new Point(x0, y + y0);
                    var point1 = new Point(x1, y + y1);
                    var point2 = new Point(x2, y + y2);
                    var point3 = new Point(x3, y + y3);
                    var point4 = new Point(x4, y + y4);
                    var polyline = new PolylineGeometry(new Point[] { point0, point1, point2, point3, point4 }, true);
                    context.DrawGeometry(brush, pen, polyline);

                    brush = phoneme.preutterScale.HasValue ? pen!.Brush : ThemeManager.BackgroundBrush;
                    using (var state = context.PushPreTransform(Matrix.CreateTranslation(x0, y + y0 - 1))) {
                        context.DrawGeometry(brush, pen, pointGeometry);
                    }
                    brush = phoneme.overlapScale.HasValue ? pen!.Brush : ThemeManager.BackgroundBrush;
                    using (var state = context.PushPreTransform(Matrix.CreateTranslation(point1))) {
                        context.DrawGeometry(brush, pen, pointGeometry);
                    }
                }

                var penPos = ThemeManager.AccentPen2;
                if (phoneme.HasOffsetOverride) {
                    penPos = ThemeManager.AccentPen2Thickness3;
                }
                context.DrawLine(penPos, new Point(x, y), new Point(x, y + height));

                if (viewModel.TickWidth > ViewConstants.PianoRollTickWidthShowDetails) {
                    string phonemeText = !string.IsNullOrEmpty(phoneme.phonemeMapped) ? phoneme.phonemeMapped : phoneme.phoneme;
                    if (!string.IsNullOrEmpty(phonemeText)) {
                        var textLayout = TextLayoutCache.Get(phonemeText, ThemeManager.ForegroundBrush!, 12);
                        using (var state = context.PushPreTransform(Matrix.CreateTranslation(x, 8))) {
                            textLayout.Draw(context);
                        }
                    }
                }
            }
        }
    }
}

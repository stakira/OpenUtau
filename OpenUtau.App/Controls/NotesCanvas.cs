using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;

namespace OpenUtau.App.Controls {
    class NotesCanvas : Canvas {
        public static readonly DirectProperty<NotesCanvas, double> TickWidthProperty =
            AvaloniaProperty.RegisterDirect<NotesCanvas, double>(
                nameof(TickWidth),
                o => o.TickWidth,
                (o, v) => o.TickWidth = v);
        public static readonly DirectProperty<NotesCanvas, double> TrackHeightProperty =
            AvaloniaProperty.RegisterDirect<NotesCanvas, double>(
                nameof(TrackHeight),
                o => o.TrackHeight,
                (o, v) => o.TrackHeight = v);
        public static readonly DirectProperty<NotesCanvas, double> TickOffsetProperty =
            AvaloniaProperty.RegisterDirect<NotesCanvas, double>(
                nameof(TickOffset),
                o => o.TickOffset,
                (o, v) => o.TickOffset = v);
        public static readonly DirectProperty<NotesCanvas, double> TrackOffsetProperty =
            AvaloniaProperty.RegisterDirect<NotesCanvas, double>(
                nameof(TrackOffset),
                o => o.TrackOffset,
                (o, v) => o.TrackOffset = v);
        public static readonly DirectProperty<NotesCanvas, UVoicePart?> PartProperty =
            AvaloniaProperty.RegisterDirect<NotesCanvas, UVoicePart?>(
                nameof(Part),
                o => o.Part,
                (o, v) => o.Part = v);
        public static readonly DirectProperty<NotesCanvas, bool> ShowPitchProperty =
            AvaloniaProperty.RegisterDirect<NotesCanvas, bool>(
                nameof(ShowPitch),
                o => o.ShowPitch,
                (o, v) => o.ShowPitch = v);
        public static readonly DirectProperty<NotesCanvas, bool> ShowVibratoProperty =
            AvaloniaProperty.RegisterDirect<NotesCanvas, bool>(
                nameof(ShowVibrato),
                o => o.ShowVibrato,
                (o, v) => o.ShowVibrato = v);

        public double TickWidth {
            get => tickWidth;
            private set => SetAndRaise(TickWidthProperty, ref tickWidth, value);
        }
        public double TrackHeight {
            get => trackHeight;
            private set => SetAndRaise(TrackHeightProperty, ref trackHeight, value);
        }
        public double TickOffset {
            get => tickOffset;
            private set => SetAndRaise(TickOffsetProperty, ref tickOffset, value);
        }
        public double TrackOffset {
            get => trackOffset;
            private set => SetAndRaise(TrackOffsetProperty, ref trackOffset, value);
        }
        public UVoicePart? Part {
            get => part;
            set => SetAndRaise(PartProperty, ref part, value);
        }
        public bool ShowPitch {
            get => showPitch;
            private set => SetAndRaise(ShowPitchProperty, ref showPitch, value);
        }
        public bool ShowVibrato {
            get => showVibrato;
            private set => SetAndRaise(ShowVibratoProperty, ref showVibrato, value);
        }

        private double tickWidth;
        private double trackHeight;
        private double tickOffset;
        private double trackOffset;
        private UVoicePart? part;
        private bool showPitch = true;
        private bool showVibrato = true;

        private HashSet<UNote> selectedNotes = new HashSet<UNote>();
        private Geometry pointGeometry;

        public NotesCanvas() {
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
            if (Part == null) {
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
                RenderNoteBody(note, viewModel, context);
                if (!note.Error) {
                    if (ShowPitch) {
                        RenderPitchBend(note, viewModel, context);
                    }
                    if (ShowPitch || ShowVibrato) {
                        RenderVibrato(note, viewModel, context);
                    }
                    if (ShowVibrato) {
                        RenderVibratoToggle(note, viewModel, context);
                        RenderVibratoControl(note, viewModel, context);
                    }
                }
            }
        }

        private void RenderNoteBody(UNote note, NotesViewModel viewModel, DrawingContext context) {
            Point leftTop = viewModel.TickToneToPoint(note.position, note.tone);
            leftTop = leftTop.WithX(leftTop.X + 1).WithY(Math.Round(leftTop.Y + 1));
            Size size = viewModel.TickToneToSize(note.duration, 1);
            size = size.WithWidth(size.Width - 1).WithHeight(Math.Floor(size.Height - 2));
            Point rightBottom = new Point(leftTop.X + size.Width, leftTop.Y + size.Height);
            var brush = selectedNotes.Contains(note)
                ? (note.Error ? ThemeManager.AccentBrush2Semi : ThemeManager.AccentBrush2)
                : (note.Error ? ThemeManager.AccentBrush1Semi : ThemeManager.AccentBrush1);
            context.DrawRectangle(brush, null, new Rect(leftTop, rightBottom), 2, 2);
            if (TrackHeight < 10 || note.lyric.Length == 0) {
                return;
            }
            string displayLyric = note.lyric;
            var textLayout = TextLayoutCache.Get(displayLyric, Brushes.White, 12);
            if (textLayout.Size.Width + 5 > size.Width) {
                displayLyric = displayLyric[0] + "..";
                textLayout = TextLayoutCache.Get(displayLyric, Brushes.White, 12);
                if (textLayout.Size.Width + 5 > size.Width) {
                    return;
                }
            }
            Point textPosition = leftTop.WithX(leftTop.X + 5)
                .WithY(Math.Round(leftTop.Y + (size.Height - textLayout.Size.Height) / 2));
            using (var state = context.PushPreTransform(Matrix.CreateTranslation(textPosition.X, textPosition.Y))) {
                textLayout.Draw(context);
            }
        }

        private void RenderPitchBend(UNote note, NotesViewModel viewModel, DrawingContext context) {
            var pitchExp = note.pitch;
            var pts = pitchExp.data;
            if (pts.Count < 2) return;

            var project = viewModel.Project;
            double p0Tick = note.position + project.MillisecondToTick(pts[0].X);
            double p0Tone = note.tone + pts[0].Y / 10.0;
            Point p0 = viewModel.TickToneToPoint(p0Tick, p0Tone - 0.5);

            var brush = note.pitch.snapFirst ? ThemeManager.AccentBrush3 : null;
            var pen = ThemeManager.AccentPen3;
            using (var state = context.PushPreTransform(Matrix.CreateTranslation(p0.X, p0.Y))) {
                context.DrawGeometry(brush, pen, pointGeometry);
            }

            for (int i = 1; i < pts.Count; i++) {
                double p1Tick = note.position + project.MillisecondToTick(pts[i].X);
                double p1Tone = note.tone + pts[i].Y / 10.0;
                Point p1 = viewModel.TickToneToPoint(p1Tick, p1Tone - 0.5);

                // Draw arc
                double x0 = p0.X;
                double y0 = p0.Y;
                double x1 = p0.X;
                double y1 = p0.Y;
                if (p1.X - p0.X < 5) {
                    context.DrawLine(pen, p0, p1);
                } else {
                    while (x0 < p1.X) {
                        x1 = Math.Min(x1 + 4, p1.X);
                        y1 = MusicMath.InterpolateShape(p0.X, p1.X, p0.Y, p1.Y, x1, pts[i - 1].shape);
                        context.DrawLine(pen, new Point(x0, y0), new Point(x1, y1));
                        x0 = x1;
                        y0 = y1;
                    }
                }
                p0 = p1;
                using (var state = context.PushPreTransform(Matrix.CreateTranslation(p0.X, p0.Y))) {
                    context.DrawGeometry(null, pen, pointGeometry);
                }
            }
        }

        private void RenderVibrato(UNote note, NotesViewModel viewModel, DrawingContext context) {
            var vibrato = note.vibrato;
            if (vibrato == null || vibrato.length == 0) {
                return;
            }

            var pen = ThemeManager.AccentPen3;
            float nPeriod = (float)viewModel.Project.MillisecondToTick(vibrato.period) / note.duration;
            float nPos = vibrato.NormalizedStart;
            var point = vibrato.Evaluate(nPos, nPeriod, note);
            Point p0 = viewModel.TickToneToPoint(point.X, point.Y);
            while (nPos < 1) {
                nPos = Math.Min(1, nPos + nPeriod / 16);
                point = vibrato.Evaluate(nPos, nPeriod, note);
                var p1 = viewModel.TickToneToPoint(point.X, point.Y);
                context.DrawLine(pen, p0, p1);
                p0 = p1;
            }
        }

        private readonly Geometry vibratoIcon = Geometry.Parse("M-6.5 1 L-6 1.5 L-4.5 0 L-2 2.5 L0.5 0 L3 2.5 L6.5 -1 L6 -1.5 L4.5 0 L2 -2.5 L-0.5 0 L-3 -2.5 Z");
        private void RenderVibratoToggle(UNote note, NotesViewModel viewModel, DrawingContext context) {
            var vibrato = note.vibrato;
            var togglePos = vibrato.GetToggle(note);
            Point icon = viewModel.TickToneToPoint(togglePos.X, togglePos.Y);
            var pen = ThemeManager.BarNumberPen;
            using (var state = context.PushPreTransform(Matrix.CreateTranslation(icon.X - 10, icon.Y))) {
                context.DrawGeometry(vibrato.length == 0 ? null : pen.Brush, pen, vibratoIcon);
            }
        }

        private void RenderVibratoControl(UNote note, NotesViewModel viewModel, DrawingContext context) {
            var vibrato = note.vibrato;
            if (vibrato.length == 0) {
                return;
            }
            var pen = ThemeManager.BarNumberPen!;
            Point start = viewModel.TickToneToPoint(vibrato.GetEnvelopeStart(note));
            Point fadeIn = viewModel.TickToneToPoint(vibrato.GetEnvelopeFadeIn(note));
            Point fadeOut = viewModel.TickToneToPoint(vibrato.GetEnvelopeFadeOut(note));
            Point end = viewModel.TickToneToPoint(vibrato.GetEnvelopeEnd(note));
            context.DrawLine(pen, start, fadeIn);
            context.DrawLine(pen, fadeIn, fadeOut);
            context.DrawLine(pen, fadeOut, end);
            using (var state = context.PushPreTransform(Matrix.CreateTranslation(start))) {
                context.DrawGeometry(pen.Brush, pen, pointGeometry);
            }
            using (var state = context.PushPreTransform(Matrix.CreateTranslation(fadeIn))) {
                context.DrawGeometry(pen.Brush, pen, pointGeometry);
            }
            using (var state = context.PushPreTransform(Matrix.CreateTranslation(fadeOut))) {
                context.DrawGeometry(pen.Brush, pen, pointGeometry);
            }
            vibrato.GetPeriodStartEnd(note, DocManager.Inst.Project, out var periodStartPos, out var periodEndPos);
            Point periodStart = viewModel.TickToneToPoint(periodStartPos);
            Point periodEnd = viewModel.TickToneToPoint(periodEndPos);
            float height = (float)TrackHeight / 3;
            periodStart = periodStart.WithY(periodStart.Y - height / 2 - 0.5f);
            double width = periodEnd.X - periodStart.X;
            periodEnd = periodEnd.WithX(periodEnd.X - 2).WithY(periodEnd.Y - height / 2 - 0.5f);
            context.DrawRectangle(null, pen, new Rect(periodStart, new Size(width, height)), 1, 1);
            context.DrawLine(pen, periodEnd, periodEnd + new Vector(0, height));
        }
    }
}

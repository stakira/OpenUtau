using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using ReactiveUI;

namespace OpenUtau.App.Controls {
    class NotesCanvas : Control {
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
        public static readonly DirectProperty<NotesCanvas, bool> ShowFinalPitchProperty =
            AvaloniaProperty.RegisterDirect<NotesCanvas, bool>(
                nameof(ShowFinalPitch),
                o => o.ShowFinalPitch,
                (o, v) => o.ShowFinalPitch = v);
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
        public bool ShowFinalPitch {
            get => showFinalPitch;
            private set => SetAndRaise(ShowFinalPitchProperty, ref showFinalPitch, value);
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
        private bool showFinalPitch = true;
        private bool showVibrato = true;
        private PolylineGeometry polylineGeometry = new PolylineGeometry();
        private Points points = new Points();

        private HashSet<UNote> selectedNotes = new HashSet<UNote>();
        private Geometry pointGeometry;

        private bool showGhostNotes = true;
        private List<UPart> otherPartsInView = new List<UPart>();

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
            MessageBus.Current.Listen<PartRefreshEvent>()
                .Subscribe(_ => RefreshGhostNotes());
            this.WhenAnyValue(x => x.Part)
                .Subscribe(_ => RefreshGhostNotes());
        }

        void RefreshGhostNotes() {
            showGhostNotes = Convert.ToBoolean(Preferences.Default.ShowGhostNotes);
            if (Part == null || !showGhostNotes) {
                return;
            }
            otherPartsInView = DocManager.Inst.Project.parts
                .Where(other => other.trackNo != Part.trackNo &&
                    other.position < Part.End &&
                    Part.position < other.End)
                .ToList();
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
            var viewModel = ((PianoRollViewModel?)DataContext)?.NotesViewModel;
            if (viewModel == null) {
                return;
            }
            DrawBackgroundForHitTest(context);
            double leftTick = TickOffset - 480;
            double rightTick = TickOffset + Bounds.Width / TickWidth + 480;
            bool hidePitch = viewModel.TickWidth <= ViewConstants.PianoRollTickWidthShowDetails * 0.5;

            if (showGhostNotes) {
                foreach (UPart otherPart in otherPartsInView) {
                    if (otherPart is UVoicePart otherVoicePart) {
                        var xOffset = otherVoicePart.position - Part.position;
                        var brush = ThemeManager.NeutralAccentBrushSemi;
                        if (otherVoicePart.trackNo >= 0) {
                            var track = DocManager.Inst.Project.tracks[otherVoicePart.trackNo];
                            brush = ThemeManager.GetTrackColor(track.TrackColor).AccentColorLightSemi;
                        }

                        foreach (var note in otherVoicePart.notes) {
                            if (note.LeftBound + xOffset >= rightTick || note.RightBound + xOffset <= leftTick) {
                                continue;
                            }
                            RenderGhostNote(note, viewModel, context, xOffset, brush);
                        }
                    }
                }
            }

            foreach (var note in Part.notes) {
                if (note.LeftBound >= rightTick || note.RightBound <= leftTick) {
                    continue;
                }
                RenderNoteBody(note, viewModel, context);
            }
            if (ShowFinalPitch && !hidePitch) {
                RenderFinalPitch(leftTick, rightTick, viewModel, context);
            }
            foreach (var note in Part.notes) {
                if (note.LeftBound >= rightTick || note.RightBound <= leftTick) {
                    continue;
                }
                if (ShowPitch && !hidePitch) {
                    RenderPitchBend(note, viewModel, context);
                }
                if ((ShowPitch || ShowVibrato) && !hidePitch) {
                    RenderVibrato(note, viewModel, context);
                }
                if (ShowVibrato && !note.Error && !hidePitch) {
                    RenderVibratoToggle(note, viewModel, context);
                    RenderVibratoControl(note, viewModel, context);
                }
            }
        }

        private void DrawBackgroundForHitTest(DrawingContext context) {
            context.DrawRectangle(Brushes.Transparent, null, Bounds.WithX(0).WithY(0));
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
            int txtsize = 12;
            var textLayout = TextLayoutCache.Get(displayLyric, Brushes.White, txtsize);
            if (txtsize > size.Height) {
                return;
            }
            if (textLayout.Height + 5 < size.Height) {
                txtsize = (int)(12 * (size.Height / textLayout.Height));
                textLayout = TextLayoutCache.Get(displayLyric, Brushes.White, txtsize);
            }
            if (textLayout.Width + 5 > size.Width) {
                displayLyric = displayLyric[0] + "..";
                textLayout = TextLayoutCache.Get(displayLyric, Brushes.White, txtsize);
                if (textLayout.Width + 5 > size.Width) {
                    return;
                }
            }
            Point textPosition = leftTop.WithX(leftTop.X + 5)
                .WithY(Math.Round(leftTop.Y + (size.Height - textLayout.Height) / 2));
            using (var state = context.PushTransform(Matrix.CreateTranslation(textPosition.X, textPosition.Y))) {
                textLayout.Draw(context, new Point());
            }
        }

        private void RenderGhostNote(UNote note, NotesViewModel viewModel, DrawingContext context, int partOffset, IBrush brush) {
            // REVIEW should ghost note be smaller?
            double relativeSize = 0.5d;
            double height = TrackHeight * relativeSize;
            double yOffset = Math.Floor(height * 0.5f);
            Point leftTop = viewModel.TickToneToPoint(partOffset + note.position, note.tone);
            leftTop = leftTop.WithX(leftTop.X + 1).WithY(Math.Round(leftTop.Y + 1 + yOffset));

            Size size = viewModel.TickToneToSize(note.duration, relativeSize);
            size = size.WithWidth(size.Width - 1).WithHeight(Math.Floor(size.Height - 2));

            Point rightBottom = new Point(leftTop.X + size.Width, leftTop.Y + size.Height);

            context.DrawRectangle(brush, null, new Rect(leftTop, rightBottom), 2, 2);
        }

        private void RenderPitchBend(UNote note, NotesViewModel viewModel, DrawingContext context) {
            var pitchExp = note.pitch;
            var pts = pitchExp.data;
            if (pts.Count < 2 || viewModel.Part == null) return;

            var project = viewModel.Project;
            double p0Tick = project.timeAxis.MsPosToTickPos(note.PositionMs + pts[0].X) - viewModel.Part.position;
            double p0Tone = note.tone + pts[0].Y / 10.0;
            Point p0 = viewModel.TickToneToPoint(p0Tick, p0Tone - 0.5);
            points.Clear();
            points.Add(p0);

            var brush = note.pitch.snapFirst ? ThemeManager.AccentBrush3 : null;
            var pen = ThemeManager.AccentPen3;
            using (var state = context.PushTransform(Matrix.CreateTranslation(p0.X, p0.Y))) {
                context.DrawGeometry(brush, pen, pointGeometry);
            }

            for (int i = 1; i < pts.Count; i++) {
                double p1Tick = project.timeAxis.MsPosToTickPos(note.PositionMs + pts[i].X) - viewModel.Part.position;
                double p1Tone = note.tone + pts[i].Y / 10.0;
                Point p1 = viewModel.TickToneToPoint(p1Tick, p1Tone - 0.5);

                // Draw arc
                double x0 = p0.X;
                double y0 = p0.Y;
                double x1 = p0.X;
                double y1 = p0.Y;
                if (p1.X - p0.X < 5) {
                    points.Add(p1);
                } else {
                    points.Add(new Point(x0, y0));
                    while (x0 < p1.X) {
                        x1 = Math.Min(x1 + 4, p1.X);
                        y1 = MusicMath.InterpolateShape(p0.X, p1.X, p0.Y, p1.Y, x1, pts[i - 1].shape);
                        points.Add(new Point(x1, y1));
                        x0 = x1;
                        y0 = y1;
                    }
                }
                p0 = p1;
                using (var state = context.PushTransform(Matrix.CreateTranslation(p0.X, p0.Y))) {
                    context.DrawGeometry(null, pen, pointGeometry);
                }
            }
            polylineGeometry.Points = points;
            context.DrawGeometry(null, pen, polylineGeometry);
        }

        private void RenderVibrato(UNote note, NotesViewModel viewModel, DrawingContext context) {
            var vibrato = note.vibrato;
            if (vibrato == null || vibrato.length == 0) {
                return;
            }

            var pen = ThemeManager.AccentPen3;
            float nPeriod = (float)viewModel.Project.timeAxis.TicksBetweenMsPos(note.PositionMs, note.PositionMs + vibrato.period) / note.duration;
            float nPos = vibrato.NormalizedStart;
            var point = vibrato.Evaluate(nPos, nPeriod, note);
            points.Clear();
            points.Add(viewModel.TickToneToPoint(point.X, point.Y - 0.5));
            while (nPos < 1) {
                nPos = Math.Min(1, nPos + nPeriod / 16);
                point = vibrato.Evaluate(nPos, nPeriod, note);
                points.Add(viewModel.TickToneToPoint(point.X, point.Y - 0.5));
            }
            polylineGeometry.Points = points;
            context.DrawGeometry(null, pen, polylineGeometry);
        }

        private readonly Geometry vibratoIcon = Geometry.Parse("M-6.5 1 L-6 1.5 L-4.5 0 L-2 2.5 L0.5 0 L3 2.5 L6.5 -1 L6 -1.5 L4.5 0 L2 -2.5 L-0.5 0 L-3 -2.5 Z");
        private void RenderVibratoToggle(UNote note, NotesViewModel viewModel, DrawingContext context) {
            var vibrato = note.vibrato;
            var togglePos = vibrato.GetToggle(note);
            Point icon = viewModel.TickToneToPoint(togglePos.X, togglePos.Y);
            var pen = ThemeManager.BarNumberPen;
            using (var state = context.PushTransform(Matrix.CreateTranslation(icon.X - 10, icon.Y))) {
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
            using (var state = context.PushTransform(Matrix.CreateTranslation(start))) {
                context.DrawGeometry(pen.Brush, pen, pointGeometry);
            }
            using (var state = context.PushTransform(Matrix.CreateTranslation(fadeIn))) {
                context.DrawGeometry(pen.Brush, pen, pointGeometry);
            }
            using (var state = context.PushTransform(Matrix.CreateTranslation(fadeOut))) {
                context.DrawGeometry(pen.Brush, pen, pointGeometry);
            }
            vibrato.GetPeriodStartEnd(DocManager.Inst.Project, note, out var periodStartPos, out var periodEndPos);
            Point periodStart = viewModel.TickToneToPoint(periodStartPos);
            Point periodEnd = viewModel.TickToneToPoint(periodEndPos);
            float height = (float)TrackHeight / 3;
            periodStart = periodStart.WithY(periodStart.Y - height / 2 - 0.5f);
            double width = periodEnd.X - periodStart.X;
            periodEnd = periodEnd.WithX(periodEnd.X - 2).WithY(periodEnd.Y - height / 2 - 0.5f);
            context.DrawRectangle(null, pen, new Rect(periodStart, new Size(width, height)), 1, 1);
            context.DrawLine(pen, periodEnd, periodEnd + new Vector(0, height));
        }

        private void RenderFinalPitch(double leftTick, double rightTick, NotesViewModel viewModel, DrawingContext context) {
            var pen = ThemeManager.FinalPitchPen!;
            lock (Part!) {
                foreach (var phrase in Part!.renderPhrases) {
                    if (phrase.position - Part.position > rightTick || phrase.end - Part.position < leftTick) {
                        continue;
                    }
                    int pitchStart = phrase.position - phrase.leading - Part.position;
                    int startIdx = (int)Math.Max(0, (leftTick - pitchStart) / 5);
                    int endIdx = (int)Math.Min(phrase.pitches.Length, (rightTick - pitchStart) / 5 + 1);
                    points.Clear();
                    for (int i = startIdx; i < endIdx; ++i) {
                        int t = pitchStart + i * 5;
                        float p = phrase.pitches[i];
                        points.Add(viewModel.TickToneToPoint(t, p / 100 - 0.5));
                    }
                    polylineGeometry.Points = points;
                    context.DrawGeometry(null, pen, polylineGeometry);
                }
            }
        }
    }
}

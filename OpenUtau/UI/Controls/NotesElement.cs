using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Media;

using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.UI.Controls {
    class NotesElement : ExpElement {
        public new double X { set { if (tTrans.X != Math.Round(value)) { tTrans.X = Math.Round(value); MarkUpdate(); } } get { return tTrans.X; } }
        public double Y { set { if (tTrans.Y != Math.Round(value)) { tTrans.Y = Math.Round(value); } } get { return tTrans.Y; } }

        double _trackHeight;
        public double TrackHeight { set { if (_trackHeight != value) { _trackHeight = value; MarkUpdate(); } } get { return _trackHeight; } }

        double _quarterWidth;
        public double QuarterWidth { set { if (_quarterWidth != value) { _quarterWidth = value; MarkUpdate(); } } get { return _quarterWidth; } }

        bool _showPitch = true;
        public bool ShowPitch { set { if (_showPitch != value) { _showPitch = value; MarkUpdate(); } } get { return _showPitch; } }
        bool _showVibrato = true;
        public bool ShowVibrato { set { if (_showVibrato != value) { _showVibrato = value; MarkUpdate(); } } get { return _showVibrato; } }

        public override UVoicePart Part { set { _part = value; ClearFormattedTextPool(); MarkUpdate(); } get { return _part; } }

        public Models.MidiViewModel midiVM;

        protected Pen penPit;
        protected Pen penVbr;

        #region FormattedText cache

        protected class FormattedTextItem {
            public FormattedText fText;
            public double width;
            public double height;
        }
        protected static Dictionary<Tuple<string, bool>, FormattedTextItem> fTextPool2 = new Dictionary<Tuple<string, bool>, FormattedTextItem>();
        protected static FormattedTextItem GetFormattedText(string text, bool white) {
            var key = Tuple.Create(text, white);
            if (!fTextPool2.TryGetValue(key, out var fTextItem)) {
                var fText = new FormattedText(
                       text,
                       System.Threading.Thread.CurrentThread.CurrentUICulture,
                       FlowDirection.LeftToRight,
                       SystemFonts.CaptionFontFamily.GetTypefaces().First(),
                       12, white ? Brushes.White : ThemeManager.ForegroundBrushNormal, 1);
                fTextItem = new FormattedTextItem {
                    fText = fText,
                    width = fText.Width,
                    height = fText.Height,
                };
                fTextPool2.Add(key, fTextItem);
            }
            return fTextItem;
        }

        #endregion

        private readonly Geometry vibratoIcon = Geometry.Parse("M-6.5 1 L-6 1.5 L-4.5 0 L-2 2.5 L0.5 0 L3 2.5 L6.5 -1 L6 -1.5 L4.5 0 L2 -2.5 L-0.5 0 L-3 -2.5 Z");

        public NotesElement() {
            penPit = new Pen(ThemeManager.PitchBrush, 1);
            penPit.Freeze();
            penVbr = new Pen(ThemeManager.VibratoBrush, 1);
            penVbr.Freeze();
            this.IsHitTestVisible = false;
        }

        public override void Redraw(DrawingContext cxt) {
            if (Part != null) {
                bool inView, lastInView = false;
                UNote lastNote = null;
                foreach (var note in Part.notes) {
                    inView = midiVM.NoteIsInView(note);

                    if (inView && !lastInView)
                        if (lastNote != null)
                            DrawNote(lastNote, cxt);

                    if (inView || !inView && lastInView)
                        DrawNote(note, cxt);

                    lastNote = note;
                    lastInView = inView;
                }
            }
        }

        private void ClearFormattedTextPool() {
            fTextPool2.Clear();
        }

        private void DrawNote(UNote note, DrawingContext cxt) {
            DrawNoteBody(note, cxt);
            if (!note.Error) {
                if (ShowPitch) {
                    DrawPitchBend(note, cxt);
                }
                if (ShowPitch || ShowVibrato) {
                    DrawVibrato(note, cxt);
                }
                if (ShowVibrato) {
                    DrawVibratoToggle(note, cxt);
                    DrawVibratoControl(note, cxt);
                }
            }
        }


        private Brush GetNoteBrush(UNote note) {
            return note.Error
                ? note.Selected
                    ? ThemeManager.NoteFillSelectedErrorBrushes
                    : ThemeManager.NoteFillErrorBrushes[0]
                : note.Selected
                    ? ThemeManager.NoteFillSelectedBrush
                    : ThemeManager.NoteFillBrushes[0];
        }

        private void DrawNoteBody(UNote note, DrawingContext cxt) {
            Point leftTop = PosToPoint(new Vector2(note.position, note.tone));
            leftTop.X += 1;
            leftTop.Y += 1;
            Point rightBottom = PosToPoint(new Vector2(note.position + note.duration, note.tone - 1));
            rightBottom.Y -= 1;
            Size size = new Size(Math.Max(1, rightBottom.X - leftTop.X), Math.Max(1, rightBottom.Y - leftTop.Y));
            cxt.DrawRoundedRectangle(GetNoteBrush(note), null, new Rect(leftTop, rightBottom), 2, 2);
            if (size.Height < 10 || note.lyric.Length == 0) {
                return;
            }
            string displayLyric = note.lyric;
            var fTextItem = GetFormattedText(displayLyric, true);
            if (fTextItem.width + 5 > size.Width) {
                displayLyric = note.lyric[0] + "..";
                fTextItem = GetFormattedText(displayLyric, true);
                if (fTextItem.width + 5 > size.Width) {
                    return;
                }
            }
            cxt.DrawText(fTextItem.fText, new Point((int)leftTop.X + 5, Math.Round(leftTop.Y + (size.Height - fTextItem.height) / 2)));
        }

        private void DrawVibrato(UNote note, DrawingContext cxt) {
            var vibrato = note.vibrato;
            if (vibrato == null || vibrato.length == 0) {
                return;
            }

            float nPeriod = (float)DocManager.Inst.Project.MillisecondToTick(vibrato.period) / note.duration;
            float nPos = vibrato.NormalizedStart;
            Point p0 = PosToPoint(vibrato.Evaluate(nPos, nPeriod, note));
            while (nPos < 1) {
                nPos = Math.Min(1, nPos + nPeriod / 16);
                var p1 = PosToPoint(vibrato.Evaluate(nPos, nPeriod, note));
                cxt.DrawLine(penPit, p0, p1);
                p0 = p1;
            }
        }

        private void DrawPitchBend(UNote note, DrawingContext cxt) {
            var _pitchExp = note.pitch;
            var _pts = _pitchExp.data;
            if (_pts.Count < 2) return;

            double pt0Tick = note.position + MusicMath.MillisecondToTick(_pts[0].X, DocManager.Inst.Project.bpm, DocManager.Inst.Project.beatUnit, DocManager.Inst.Project.resolution);
            double pt0X = midiVM.QuarterWidth * pt0Tick / DocManager.Inst.Project.resolution;
            double pt0Pit = note.tone + _pts[0].Y / 10.0;
            double pt0Y = TrackHeight * (UIConstants.MaxNoteNum - 1.0 - pt0Pit) + TrackHeight / 2;

            cxt.DrawEllipse(note.pitch.snapFirst ? penPit.Brush : null, penPit, new Point(pt0X, pt0Y), 2.5, 2.5);

            for (int i = 1; i < _pts.Count; i++) {
                double pt1Tick = note.position + MusicMath.MillisecondToTick(_pts[i].X, DocManager.Inst.Project.bpm, DocManager.Inst.Project.beatUnit, DocManager.Inst.Project.resolution);
                double pt1X = midiVM.QuarterWidth * pt1Tick / DocManager.Inst.Project.resolution;
                double pt1Pit = note.tone + _pts[i].Y / 10.0;
                double pt1Y = TrackHeight * (UIConstants.MaxNoteNum - 1.0 - pt1Pit) + TrackHeight / 2;

                // Draw arc
                double _x = pt0X;
                double _x2 = pt0X;
                double _y = pt0Y;
                double _y2 = pt0Y;
                if (pt1X - pt0X < 5) {
                    cxt.DrawLine(penPit, new Point(pt0X, pt0Y), new Point(pt1X, pt1Y));
                } else {
                    while (_x2 < pt1X) {
                        _x = Math.Min(_x + 4, pt1X);
                        _y = MusicMath.InterpolateShape(pt0X, pt1X, pt0Y, pt1Y, _x, _pts[i - 1].shape);
                        cxt.DrawLine(penPit, new Point(_x, _y), new Point(_x2, _y2));
                        _x2 = _x;
                        _y2 = _y;
                    }
                }

                pt0Tick = pt1Tick;
                pt0X = pt1X;
                pt0Pit = pt1Pit;
                pt0Y = pt1Y;
                cxt.DrawEllipse(null, penPit, new Point(pt0X, pt0Y), 2.5, 2.5);
            }
        }

        private void DrawVibratoToggle(UNote note, DrawingContext cxt) {
            var vibrato = note.vibrato;
            Point icon = PosToPoint(vibrato.GetToggle(note));
            cxt.PushTransform(new TranslateTransform(icon.X - 10, icon.Y));
            cxt.DrawGeometry(vibrato.length == 0 ? null : penVbr.Brush, penVbr, vibratoIcon);
            cxt.Pop();
        }

        private void DrawVibratoControl(UNote note, DrawingContext cxt) {
            var vibrato = note.vibrato;
            if (vibrato.length == 0) {
                return;
            }
            Point start = PosToPoint(vibrato.GetEnvelopeStart(note));
            Point fadeIn = PosToPoint(vibrato.GetEnvelopeFadeIn(note));
            Point fadeOut = PosToPoint(vibrato.GetEnvelopeFadeOut(note));
            Point end = PosToPoint(vibrato.GetEnvelopeEnd(note));
            cxt.DrawLine(penVbr, start, fadeIn);
            cxt.DrawLine(penVbr, fadeIn, fadeOut);
            cxt.DrawLine(penVbr, fadeOut, end);
            cxt.DrawEllipse(penVbr.Brush, penVbr, start, 2.5, 2.5);
            cxt.DrawEllipse(penVbr.Brush, penVbr, fadeIn, 2.5, 2.5);
            cxt.DrawEllipse(penVbr.Brush, penVbr, fadeOut, 2.5, 2.5);
            vibrato.GetPeriodStartEnd(note, DocManager.Inst.Project, out var periodStartPos, out var periodEndPos);
            Point periodStart = PosToPoint(periodStartPos);
            Point periodEnd = PosToPoint(periodEndPos);
            float height = (float)TrackHeight / 3;
            periodStart.Y -= height / 2 + 0.5f;
            double width = periodEnd.X - periodStart.X;
            periodEnd.X -= 2;
            periodEnd.Y -= height / 2 + 0.5f;
            cxt.DrawRoundedRectangle(null, penVbr, new Rect(periodStart, new Size(width, height)), 1, 1);
            cxt.DrawLine(penVbr, periodEnd, periodEnd + new System.Windows.Vector(0, height));
        }

        public Point PosToPoint(Vector2 pos) {
            float tick = pos.X;
            float noteNum = pos.Y;
            double x = tick * midiVM.QuarterWidth / DocManager.Inst.Project.resolution;
            double y = midiVM.TrackHeight * (UIConstants.MaxNoteNum - 1f - noteNum);
            return new Point(x, y);
        }

        public Vector2 PointToPos(Point point) {
            double tick = point.X / midiVM.QuarterWidth * DocManager.Inst.Project.resolution;
            double noteNum = UIConstants.MaxNoteNum - 1f - point.Y / midiVM.TrackHeight;
            return new Vector2((float)tick, (float)noteNum);
        }
    }
}

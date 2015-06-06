using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

using OpenUtau.Core;
using OpenUtau.Core.USTx;

namespace OpenUtau.UI.Controls
{
    class NotesElement : ExpElement
    {
        public new double X { set { if (tTrans.X != Math.Round(value)) { tTrans.X = Math.Round(value); MarkUpdate(); } } get { return tTrans.X; } }
        public double Y { set { if (tTrans.Y != Math.Round(value)) { tTrans.Y = Math.Round(value); } } get { return tTrans.Y; } }

        double _trackHeight;
        public double TrackHeight { set { if (_trackHeight != value) { _trackHeight = value; MarkUpdate(); } } get { return _trackHeight; } }

        double _quarterWidth;
        public double QuarterWidth { set { if (_quarterWidth != value) { _quarterWidth = value; MarkUpdate(); } } get { return _quarterWidth; } }

        bool _showPitch = true;
        public bool ShowPitch { set { if (_showPitch != value) { _showPitch = value; MarkUpdate(); } } get { return _showPitch; } }

        public override UVoicePart Part { set { _part = value; ClearFormattedTextPool(); MarkUpdate(); } get { return _part; } }

        public OpenUtau.UI.Models.MidiViewModel midiVM;

        protected Pen penPit;

        protected Dictionary<string, FormattedText> fTextPool = new Dictionary<string, FormattedText>();
        protected Dictionary<string, double> fTextWidths = new Dictionary<string, double>();
        protected Dictionary<string, double> fTextHeights = new Dictionary<string, double>();

        public NotesElement()
        {
            penPit = new Pen(ThemeManager.WhiteKeyNameBrushNormal, 1);
            penPit.Freeze();
            this.IsHitTestVisible = false;
        }

        public override void RedrawIfUpdated()
        {
            if (!_updated) return;
            DrawingContext cxt = visual.RenderOpen();
            if (Part != null)
            {
                bool inView, lastInView = false;
                UNote lastNote = null;
                foreach (var note in Part.Notes)
                {
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
            cxt.Close();
            _updated = false;
        }

        private void ClearFormattedTextPool()
        {
            fTextPool.Clear();
            fTextWidths.Clear();
            fTextHeights.Clear();
        }

        private void DrawNote(UNote note, DrawingContext cxt)
        {
            DrawNoteBody(note, cxt);
            if (!note.Error)
            {
                if (ShowPitch) DrawPitchBend(note, cxt);
                if (ShowPitch) DrawVibrato(note, cxt);
            }
        }

        private void DrawNoteBody(UNote note, DrawingContext cxt)
        {
            double left = note.PosTick * midiVM.QuarterWidth / DocManager.Inst.Project.Resolution + 1;
            double top = midiVM.TrackHeight * ((double)UIConstants.MaxNoteNum - 1 - note.NoteNum) + 1;
            double width = Math.Max(2, note.DurTick * midiVM.QuarterWidth / DocManager.Inst.Project.Resolution - 1);
            double height = Math.Max(2, midiVM.TrackHeight - 2);
            cxt.DrawRoundedRectangle(
                note.Error ?
                note.Selected ? ThemeManager.NoteFillSelectedErrorBrushes : ThemeManager.NoteFillErrorBrushes[0] :
                note.Selected ? ThemeManager.NoteFillSelectedBrush : ThemeManager.NoteFillBrushes[0],
                null, new Rect(new Point(left, top), new Size(width, height)), 2, 2);
            if (height >= 10)
            {
                if (note.Lyric.Length == 0) return;
                string displayLyric = note.Lyric;

                if (!fTextPool.ContainsKey(displayLyric)) AddToFormattedTextPool(displayLyric);
                var fText = fTextPool[displayLyric];

                if (fTextWidths[displayLyric] + 5 > width)
                {
                    displayLyric = note.Lyric[0] + "..";
                    if (!fTextPool.ContainsKey(displayLyric)) AddToFormattedTextPool(displayLyric);
                    fText = fTextPool[displayLyric];
                    if (fTextWidths[displayLyric] + 5 > width) return;
                }

                cxt.DrawText(fText, new Point((int)left + 5, Math.Round(top + (height - fTextHeights[displayLyric]) / 2)));
            }
        }

        protected virtual void AddToFormattedTextPool(string text)
        {
            var fText = new FormattedText(
                    text,
                    System.Threading.Thread.CurrentThread.CurrentUICulture,
                    FlowDirection.LeftToRight, SystemFonts.CaptionFontFamily.GetTypefaces().First(),
                    12,
                    Brushes.White);
            fTextPool.Add(text, fText);
            fTextWidths.Add(text, fText.Width);
            fTextHeights.Add(text, fText.Height);
        }

        private void DrawVibrato(UNote note, DrawingContext cxt)
        {
            if (note.Vibrato == null) return;
            var vibrato = note.Vibrato;
            double periodPix = DocManager.Inst.Project.MillisecondToTick(vibrato.Period) * midiVM.QuarterWidth / DocManager.Inst.Project.Resolution;
            double lengthPix = note.DurTick * vibrato.Length / 100 * midiVM.QuarterWidth / DocManager.Inst.Project.Resolution;

            double startX = (note.PosTick + note.DurTick * (1 - vibrato.Length / 100)) * midiVM.QuarterWidth / DocManager.Inst.Project.Resolution;
            double startY = TrackHeight * (UIConstants.MaxNoteNum - 1.0 - note.NoteNum) + TrackHeight / 2;
            double inPix = lengthPix * vibrato.In / 100;
            double outPix = lengthPix * vibrato.Out / 100;
            double depthPix = vibrato.Depth / 100 * midiVM.TrackHeight;

            /*if (vibrato.Length > 0 && vibrato.Depth > 0)
            {
                StreamGeometry g = new StreamGeometry();
                using (var gcxt = g.Open())
                {
                    gcxt.BeginFigure(new Point(startX, startY), true, true);
                    gcxt.LineTo(new Point(startX + inPix, startY + depthPix), false, false);
                    gcxt.LineTo(new Point(startX + lengthPix - outPix, startY + depthPix), false, false);
                    gcxt.LineTo(new Point(startX + lengthPix, startY), false, false);
                    gcxt.LineTo(new Point(startX + lengthPix - outPix, startY - depthPix), false, false);
                    gcxt.LineTo(new Point(startX + inPix, startY - depthPix), false, false);
                    gcxt.Close();
                }
                cxt.DrawGeometry(Brushes.White, null, g);
            }*/

            double _x0 = 0, _y0 = 0, _x1 = 0, _y1 = 0;
            while (_x1 < lengthPix)
            {
                cxt.DrawLine(penPit, new Point(startX + _x0, startY + _y0), new Point(startX + _x1, startY + _y1));
                _x0 = _x1;
                _y0 = _y1;
                _x1 += Math.Min(2, periodPix / 8);
                _y1 = -Math.Sin(2 * Math.PI * (_x1 / periodPix + vibrato.Shift / 100)) * depthPix;
                if (_x1 < inPix) _y1 = _y1 * _x1 / inPix;
                else if (_x1 > lengthPix - outPix) _y1 = _y1 * (lengthPix - _x1) / outPix;
            }
        }

        private void DrawPitchBend(UNote note, DrawingContext cxt)
        {
            var _pitchExp = note.PitchBend as PitchBendExpression;
            var _pts = _pitchExp.Data as List<PitchPoint>;
            if (_pts.Count < 2) return;

            double pt0Tick = note.PosTick + MusicMath.MillisecondToTick(_pts[0].X, DocManager.Inst.Project.BPM, DocManager.Inst.Project.BeatUnit, DocManager.Inst.Project.Resolution);
            double pt0X = midiVM.QuarterWidth * pt0Tick / DocManager.Inst.Project.Resolution;
            double pt0Pit = note.NoteNum + _pts[0].Y / 10.0;
            double pt0Y = TrackHeight * ((double)UIConstants.MaxNoteNum - 1.0 - pt0Pit) + TrackHeight / 2;

            if (note.PitchBend.SnapFirst) cxt.DrawEllipse(ThemeManager.WhiteKeyNameBrushNormal, penPit, new Point(pt0X, pt0Y), 2.5, 2.5);
            else cxt.DrawEllipse(null, penPit, new Point(pt0X, pt0Y), 2.5, 2.5);

            for (int i = 1; i < _pts.Count; i++)
            {
                double pt1Tick = note.PosTick + MusicMath.MillisecondToTick(_pts[i].X, DocManager.Inst.Project.BPM, DocManager.Inst.Project.BeatUnit, DocManager.Inst.Project.Resolution);
                double pt1X = midiVM.QuarterWidth * pt1Tick / DocManager.Inst.Project.Resolution;
                double pt1Pit = note.NoteNum + _pts[i].Y / 10.0;
                double pt1Y = TrackHeight * ((double)UIConstants.MaxNoteNum - 1.0 - pt1Pit) + TrackHeight / 2;

                // Draw arc
                double _x = pt0X;
                double _x2 = pt0X;
                double _y = pt0Y;
                double _y2 = pt0Y;
                if (pt1X - pt0X < 5)
                {
                    cxt.DrawLine(penPit, new Point(pt0X, pt0Y), new Point(pt1X, pt1Y));
                }
                else
                {
                    while (_x2 < pt1X)
                    {
                        _x = Math.Min(_x + 4, pt1X);
                        _y = MusicMath.InterpolateShape(pt0X, pt1X, pt0Y, pt1Y, _x, _pts[i - 1].Shape);
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
    }
}

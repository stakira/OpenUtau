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
    enum ExpDisMode {Hidden, Visible, Shadow};

    class ExpElement : FrameworkElement
    {
        protected DrawingVisual visual;

        protected override int VisualChildrenCount { get { return 1; } }
        protected override Visual GetVisualChild(int index) { return visual; }

        protected UVoicePart _part;
        public UVoicePart Part { set { _part = value; MarkUpdate(); } get { return _part; } }

        public string Key;
        protected TranslateTransform tTrans;

        protected double _visualHeight;
        public double VisualHeight { set { if (_visualHeight != value) { _visualHeight = value; MarkUpdate(); } } get { return _visualHeight; } }
        protected double _scaleX;
        public double ScaleX { set { if (_scaleX != value) { _scaleX = value; MarkUpdate(); } } get { return _scaleX; } }

        public ExpElement()
        {
            tTrans = new TranslateTransform();
            this.RenderTransform = tTrans;
            visual = new DrawingVisual();
            MarkUpdate();
            this.AddVisualChild(visual);
        }

        public double X { set { if (tTrans.X != Math.Round(value)) { tTrans.X = Math.Round(value); } } get { return tTrans.X; } }

        ExpDisMode displayMode;

        public ExpDisMode DisplayMode
        {
            set
            {
                if (displayMode != value)
                {
                    displayMode = value;
                    this.Opacity = displayMode == ExpDisMode.Shadow ? 0.3 : 1;
                    this.Visibility = displayMode == ExpDisMode.Hidden ? Visibility.Hidden : Visibility.Visible;
                    if (this.Parent is Canvas)
                    {
                        if (value == ExpDisMode.Visible) Canvas.SetZIndex(this, UIConstants.ExpressionVisibleZIndex);
                        else if (value == ExpDisMode.Shadow) Canvas.SetZIndex(this, UIConstants.ExpressionShadowZIndex);
                        else Canvas.SetZIndex(this, UIConstants.ExpressionHiddenZIndex);
                    }
                }
            }
            get { return displayMode; }
        }

        protected bool _updated = false;
        public void MarkUpdate() { _updated = true; }

        public virtual void RedrawIfUpdated() { }
    }

    class FloatExpElement : ExpElement
    {
        public OpenUtau.UI.Models.MidiViewModel midiVM;

        Pen pen3;
        Pen pen2;

        public FloatExpElement()
        {
            pen3 = new Pen(ThemeManager.NoteFillBrushes[0], 3);
            pen2 = new Pen(ThemeManager.NoteFillBrushes[0], 2);
            pen3.Freeze();
            pen2.Freeze();
        }

        public override void RedrawIfUpdated()
        {
            if (!_updated) return;
            DrawingContext cxt = visual.RenderOpen();
            if (Part != null)
            {
                foreach (UNote note in Part.Notes)
                {
                    if (!midiVM.NoteIsInView(note)) continue;
                    if (note.Expressions.ContainsKey(Key))
                    {
                        var _exp = note.Expressions[Key] as IntExpression;
                        var _expTemplate = DocManager.Inst.Project.ExpressionTable[Key] as IntExpression;
                        double x1 = Math.Round(ScaleX * note.PosTick);
                        double x2 = Math.Round(ScaleX * note.EndTick);
                        double valueHeight = Math.Round(VisualHeight - VisualHeight * ((int)_exp.Data - _expTemplate.Min) / (_expTemplate.Max - _expTemplate.Min));
                        double zeroHeight = Math.Round(VisualHeight - VisualHeight * (0f - _expTemplate.Min) / (_expTemplate.Max - _expTemplate.Min));
                        cxt.DrawLine(pen3, new Point(x1 + 0.5, zeroHeight + 0.5), new Point(x1 + 0.5, valueHeight + 3));
                        cxt.DrawEllipse(Brushes.White, pen2, new Point(x1 + 0.5, valueHeight), 2.5, 2.5);
                        cxt.DrawLine(pen2, new Point(x1 + 3, valueHeight), new Point(Math.Max(x1 + 3, x2 - 3), valueHeight));
                    }
                }
            }
            else
            {
                cxt.DrawRectangle(Brushes.Transparent, null, new Rect(new Point(0,0), new Point(100,100)));
            }
            cxt.Close();
            _updated = false;
        }
    }

    class PitchExpElement : ExpElement
    {
        public new double X { set { if (tTrans.X != Math.Round(value)) { tTrans.X = Math.Round(value); MarkUpdate(); } } get { return tTrans.X; } }
        public double Y { set { if (tTrans.Y != Math.Round(value)) { tTrans.Y = Math.Round(value); } } get { return tTrans.Y; } }

        double _trackHeight;
        public double TrackHeight { set { if (_trackHeight != value) { _trackHeight = value; MarkUpdate(); } } get { return _trackHeight; } }

        double _quarterWidth;
        public double QuarterWidth { set { if (_quarterWidth != value) { _quarterWidth = value; MarkUpdate(); } } get { return _quarterWidth; } }

        public OpenUtau.UI.Models.MidiViewModel midiVM;

        Pen penPit;
        Pen penEnv;
        
        public PitchExpElement()
        {
            penPit = new Pen(ThemeManager.WhiteKeyNameBrushNormal, 1);
            penPit.Freeze();
            penEnv = new Pen(Brushes.MediumPurple, 1);
            penEnv.Freeze();
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

        private void DrawNote(UNote note, DrawingContext cxt)
        {
            DrawNoteBody(note, cxt);
            if (!note.Error)
            {
                DrawEnvelope(note, cxt);
                DrawPitchBend(note, cxt);
                DrawVibrato(note, cxt);
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
                var text = new FormattedText(
                        note.Lyric,
                        System.Threading.Thread.CurrentThread.CurrentUICulture,
                        FlowDirection.LeftToRight, SystemFonts.CaptionFontFamily.GetTypefaces().First(),
                        12,
                        Brushes.White);
                if (text.Width + 5 > width && note.Lyric.Length > 0)
                    text = new FormattedText(
                        note.Lyric[0] + "..",
                        System.Threading.Thread.CurrentThread.CurrentUICulture,
                        FlowDirection.LeftToRight, SystemFonts.CaptionFontFamily.GetTypefaces().First(),
                        12,
                        Brushes.White);
                if (text.Width + 5 <= width) cxt.DrawText(text, new Point((int)left + 5, Math.Round(top + (height - text.Height) / 2)));
            }
        }

        private void DrawEnvelope(UNote note, DrawingContext cxt)
        {
            double y = midiVM.TrackHeight * ((double)UIConstants.MaxNoteNum - 1 - note.NoteNum) + 1.5;
            for (int i = 0; i < note.Phonemes.Count; i++)
            {
                var phoneme = note.Phonemes[i];
                double trackHeight = midiVM.TrackHeight - 2;
                double x0 = (note.PosTick + DocManager.Inst.Project.MillisecondToTick(phoneme.Envelope.Points[0].X))
                     * midiVM.QuarterWidth / DocManager.Inst.Project.Resolution;
                double y0 = (1 - phoneme.Envelope.Points[0].Y / 100) * trackHeight;
                double x1 = (note.PosTick + DocManager.Inst.Project.MillisecondToTick(phoneme.Envelope.Points[1].X))
                     * midiVM.QuarterWidth / DocManager.Inst.Project.Resolution;
                double y1 = (1 - phoneme.Envelope.Points[1].Y / 100) * trackHeight;
                double x2 = (note.PosTick + DocManager.Inst.Project.MillisecondToTick(phoneme.Envelope.Points[2].X))
                     * midiVM.QuarterWidth / DocManager.Inst.Project.Resolution;
                double y2 = (1 - phoneme.Envelope.Points[2].Y / 100) * trackHeight;
                double x3 = (note.PosTick + DocManager.Inst.Project.MillisecondToTick(phoneme.Envelope.Points[3].X))
                     * midiVM.QuarterWidth / DocManager.Inst.Project.Resolution;
                double y3 = (1 - phoneme.Envelope.Points[3].Y / 100) * trackHeight;
                double x4 = (note.PosTick + DocManager.Inst.Project.MillisecondToTick(phoneme.Envelope.Points[4].X))
                     * midiVM.QuarterWidth / DocManager.Inst.Project.Resolution;
                double y4 = (1 - phoneme.Envelope.Points[4].Y / 100) * trackHeight;
                cxt.DrawLine(penEnv, new Point(x0, y + y0), new Point(x1, y + y1));
                cxt.DrawLine(penEnv, new Point(x1, y + y1), new Point(x2, y + y2));
                cxt.DrawLine(penEnv, new Point(x2, y + y2), new Point(x3, y + y3));
                cxt.DrawLine(penEnv, new Point(x3, y + y3), new Point(x4, y + y4));
            }
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

            if (vibrato.Length > 0 && vibrato.Depth > 0)
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
            }

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

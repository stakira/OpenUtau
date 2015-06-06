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
    class PhonemesElement : NotesElement
    {
        public new double Y { set { } get { return 0; } }

        bool _hidePhoneme = false;
        public bool HidePhoneme { set { if (_hidePhoneme != value) { _hidePhoneme = value; MarkUpdate(); } } get { return _hidePhoneme; } }

        protected Pen penEnv;
        protected Pen penEnvSel;

        public PhonemesElement()
            : base()
        {
            penEnv = new Pen( ThemeManager.NoteFillBrushes[0] , 1);
            penEnv.Freeze();
            penEnvSel = new Pen(ThemeManager.NoteFillSelectedBrush, 1);
            penEnvSel.Freeze();
        }

        public override void RedrawIfUpdated()
        {
            if (!_updated) return;
            if (HidePhoneme) return;
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
                            DrawPhoneme(note, cxt);

                    if (inView || !inView && lastInView)
                        DrawPhoneme(note, cxt);

                    lastNote = note;
                    lastInView = inView;
                }
            }
            cxt.Close();
            _updated = false;
        }

        private void DrawPhoneme(UNote note, DrawingContext cxt)
        {
            const double y = 23.5;
            const double height = 24;
            if (note.Error) return;
            for (int i = 0; i < note.Phonemes.Count; i++)
            {
                var phoneme = note.Phonemes[i];
                double x = Math.Round(note.PosTick * midiVM.QuarterWidth / DocManager.Inst.Project.Resolution) + 0.5;
                double x0 = (note.PosTick + DocManager.Inst.Project.MillisecondToTick(phoneme.Envelope.Points[0].X))
                    * midiVM.QuarterWidth / DocManager.Inst.Project.Resolution;
                double y0 = (1 - phoneme.Envelope.Points[0].Y / 100) * height;
                double x1 = (note.PosTick + DocManager.Inst.Project.MillisecondToTick(phoneme.Envelope.Points[1].X))
                    * midiVM.QuarterWidth / DocManager.Inst.Project.Resolution;
                double y1 = (1 - phoneme.Envelope.Points[1].Y / 100) * height;
                double x2 = (note.PosTick + DocManager.Inst.Project.MillisecondToTick(phoneme.Envelope.Points[2].X))
                    * midiVM.QuarterWidth / DocManager.Inst.Project.Resolution;
                double y2 = (1 - phoneme.Envelope.Points[2].Y / 100) * height;
                double x3 = (note.PosTick + DocManager.Inst.Project.MillisecondToTick(phoneme.Envelope.Points[3].X))
                    * midiVM.QuarterWidth / DocManager.Inst.Project.Resolution;
                double y3 = (1 - phoneme.Envelope.Points[3].Y / 100) * height;
                double x4 = (note.PosTick + DocManager.Inst.Project.MillisecondToTick(phoneme.Envelope.Points[4].X))
                    * midiVM.QuarterWidth / DocManager.Inst.Project.Resolution;
                double y4 = (1 - phoneme.Envelope.Points[4].Y / 100) * height;

                Pen pen = note.Selected ? penEnvSel : penEnv;
                Brush brush = note.Selected ? ThemeManager.NoteFillSelectedErrorBrushes : ThemeManager.NoteFillErrorBrushes[0];
                
                StreamGeometry g = new StreamGeometry();
                List<Point> poly = new List<Point>() {
                    new Point(x1, y + y1),
                    new Point(x2, y + y2),
                    new Point(x3, y + y3),
                    new Point(x4, y + y4),
                    new Point(x0, y + y0)
                };

                using (var gcxt = g.Open())
                {
                    gcxt.BeginFigure(new Point(x0, y + y0), true, false);
                    gcxt.PolyLineTo(poly, true, false);
                    gcxt.Close();
                }
                cxt.DrawGeometry(brush, pen, g);

                cxt.DrawLine(penEnvSel, new Point(x, y), new Point(x, y + height));

                string text = phoneme.Phoneme;
                if (!fTextPool.ContainsKey(text)) AddToFormattedTextPool(text);
                var fText = fTextPool[text];
                if (midiVM.QuarterWidth > UIConstants.MidiQuarterMinWidthShowPhoneme)
                    cxt.DrawText(fText, new Point(Math.Round(x), 8));
            }
        }

        protected override void AddToFormattedTextPool(string text)
        {
            var fText = new FormattedText(
                    text,
                    System.Threading.Thread.CurrentThread.CurrentUICulture,
                    FlowDirection.LeftToRight, SystemFonts.CaptionFontFamily.GetTypefaces().First(),
                    12,
                    Brushes.Black);
            fTextPool.Add(text, fText);
            fTextWidths.Add(text, fText.Width);
            fTextHeights.Add(text, fText.Height);
        }
    }
}

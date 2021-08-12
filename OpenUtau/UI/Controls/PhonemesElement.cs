using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.UI.Controls {
    class PhonemesElement : NotesElement {
        public new double Y { set { } get { return 0; } }

        bool _hidePhoneme = false;
        public bool HidePhoneme { set { if (_hidePhoneme != value) { _hidePhoneme = value; MarkUpdate(); } } get { return _hidePhoneme; } }

        protected Pen penEnv;
        protected Pen penEnvSel;

        public PhonemesElement()
            : base() {
            penEnv = new Pen(ThemeManager.NoteFillBrushes[0], 1);
            penEnv.Freeze();
            penEnvSel = new Pen(ThemeManager.NoteFillSelectedBrush, 1);
            penEnvSel.Freeze();
        }

        public override void Redraw(DrawingContext cxt) {
            if (HidePhoneme) return;
            if (Part != null) {
                bool inView, lastInView = false;
                UNote lastNote = null;
                foreach (var note in Part.notes) {
                    inView = midiVM.NoteIsInView(note);

                    if (inView && !lastInView)
                        if (lastNote != null)
                            DrawPhoneme(lastNote, cxt);

                    if (inView || !inView && lastInView)
                        DrawPhoneme(note, cxt);

                    lastNote = note;
                    lastInView = inView;
                }
            }
        }

        private void DrawPhoneme(UNote note, DrawingContext cxt) {
            const double y = 23.5;
            const double height = 24;
            if (note.Error) return;
            for (int i = 0; i < note.phonemes.Count; i++) {
                var phoneme = note.phonemes[i];
                double x = Math.Round(note.position * midiVM.QuarterWidth / DocManager.Inst.Project.resolution) + 0.5;
                double x0 = (note.position + DocManager.Inst.Project.MillisecondToTick(phoneme.envelope.data[0].X))
                    * midiVM.QuarterWidth / DocManager.Inst.Project.resolution;
                double y0 = (1 - phoneme.envelope.data[0].Y / 100) * height;
                double x1 = (note.position + DocManager.Inst.Project.MillisecondToTick(phoneme.envelope.data[1].X))
                    * midiVM.QuarterWidth / DocManager.Inst.Project.resolution;
                double y1 = (1 - phoneme.envelope.data[1].Y / 100) * height;
                double x2 = (note.position + DocManager.Inst.Project.MillisecondToTick(phoneme.envelope.data[2].X))
                    * midiVM.QuarterWidth / DocManager.Inst.Project.resolution;
                double y2 = (1 - phoneme.envelope.data[2].Y / 100) * height;
                double x3 = (note.position + DocManager.Inst.Project.MillisecondToTick(phoneme.envelope.data[3].X))
                    * midiVM.QuarterWidth / DocManager.Inst.Project.resolution;
                double y3 = (1 - phoneme.envelope.data[3].Y / 100) * height;
                double x4 = (note.position + DocManager.Inst.Project.MillisecondToTick(phoneme.envelope.data[4].X))
                    * midiVM.QuarterWidth / DocManager.Inst.Project.resolution;
                double y4 = (1 - phoneme.envelope.data[4].Y / 100) * height;

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

                using (var gcxt = g.Open()) {
                    gcxt.BeginFigure(new Point(x0, y + y0), true, false);
                    gcxt.PolyLineTo(poly, true, false);
                    gcxt.Close();
                }
                cxt.DrawGeometry(brush, pen, g);

                cxt.DrawLine(penEnvSel, new Point(x, y), new Point(x, y + height));

                string text = phoneme.phoneme;
                if (!fTextPool.ContainsKey(text)) AddToFormattedTextPool(text);
                var fText = fTextPool[text];
                if (midiVM.QuarterWidth > UIConstants.MidiQuarterMinWidthShowPhoneme)
                    cxt.DrawText(fText, new Point(Math.Round(x), 8));
            }
        }

        protected override void AddToFormattedTextPool(string text) {
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

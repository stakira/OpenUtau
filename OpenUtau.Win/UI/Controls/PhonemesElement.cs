using System;
using System.Collections.Generic;
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
        protected Pen penLineThick;

        public PhonemesElement()
            : base() {
            penEnv = new Pen(ThemeManager.NoteFillBrushes[0], 1);
            penEnv.Freeze();
            penEnvSel = new Pen(ThemeManager.NoteFillSelectedBrush, 1);
            penEnvSel.Freeze();
            penLineThick = new Pen(ThemeManager.NoteFillSelectedBrush, 3);
            penLineThick.Freeze();
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
            for (int i = 0; i < note.phonemes.Count; i++) {
                var phoneme = note.phonemes[i];
                if (note.OverlapError) {
                    continue;
                }

                int position = note.position + phoneme.position;
                double x = Math.Round(position * midiVM.QuarterWidth / DocManager.Inst.Project.resolution) + 0.5;
                if (!phoneme.Error) {
                    double x0 = (position + DocManager.Inst.Project.MillisecondToTick(phoneme.envelope.data[0].X))
                        * midiVM.QuarterWidth / DocManager.Inst.Project.resolution;
                    double y0 = (1 - phoneme.envelope.data[0].Y / 100) * height;
                    double x1 = (position + DocManager.Inst.Project.MillisecondToTick(phoneme.envelope.data[1].X))
                        * midiVM.QuarterWidth / DocManager.Inst.Project.resolution;
                    double y1 = (1 - phoneme.envelope.data[1].Y / 100) * height;
                    double x2 = (position + DocManager.Inst.Project.MillisecondToTick(phoneme.envelope.data[2].X))
                        * midiVM.QuarterWidth / DocManager.Inst.Project.resolution;
                    double y2 = (1 - phoneme.envelope.data[2].Y / 100) * height;
                    double x3 = (position + DocManager.Inst.Project.MillisecondToTick(phoneme.envelope.data[3].X))
                        * midiVM.QuarterWidth / DocManager.Inst.Project.resolution;
                    double y3 = (1 - phoneme.envelope.data[3].Y / 100) * height;
                    double x4 = (position + DocManager.Inst.Project.MillisecondToTick(phoneme.envelope.data[4].X))
                        * midiVM.QuarterWidth / DocManager.Inst.Project.resolution;
                    double y4 = (1 - phoneme.envelope.data[4].Y / 100) * height;

                    Pen pen = note.Selected ? penEnvSel : penEnv;
                    Brush brush = note.Selected ? ThemeManager.NoteFillSelectedErrorBrushes : ThemeManager.NoteFillErrorBrushes[0];

                    var point0 = new Point(x0, y + y0);
                    var point1 = new Point(x1, y + y1);
                    var point2 = new Point(x2, y + y2);
                    var point3 = new Point(x3, y + y3);
                    var point4 = new Point(x4, y + y4);
                    StreamGeometry g = new StreamGeometry();
                    List<Point> poly = new List<Point>() { point0, point1, point2, point3, point4 };

                    using (var gcxt = g.Open()) {
                        gcxt.BeginFigure(point0, true, false);
                        gcxt.PolyLineTo(poly, true, false);
                        gcxt.Close();
                    }
                    cxt.DrawGeometry(brush, pen, g);
                    cxt.DrawEllipse(phoneme.preutterScale.HasValue ? pen.Brush : ThemeManager.UIBackgroundBrushNormal, pen, new Point(x0, y + y0 - 1), 2.5, 2.5);
                    cxt.DrawEllipse(phoneme.overlapScale.HasValue ? pen.Brush : ThemeManager.UIBackgroundBrushNormal, pen, point1, 2.5, 2.5);
                }

                var penPos = penEnvSel;
                if (phoneme.HasOffsetOverride) {
                    penPos = penLineThick;
                }
                cxt.DrawLine(penPos, new Point(x, y), new Point(x, y + height));

                string phonemeText = !string.IsNullOrEmpty(phoneme.phonemeMapped) ? phoneme.phonemeMapped : phoneme.phoneme;
                if (!string.IsNullOrEmpty(phonemeText)) {
                    var fText = GetFormattedText(phonemeText, false).fText;
                    if (midiVM.QuarterWidth > UIConstants.MidiQuarterMinWidthShowPhoneme) {
                        cxt.DrawText(fText, new Point(Math.Round(x), 8));
                    }
                }
            }
        }
    }
}

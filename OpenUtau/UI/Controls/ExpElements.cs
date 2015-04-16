using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

using OpenUtau.Core.USTx;

namespace OpenUtau.UI.Controls
{
    public class ExpElement : FrameworkElement
    {
        protected TranslateTransform tTrans;

        public ExpElement()
        {
            tTrans = new TranslateTransform();
            this.RenderTransform = tTrans;
        }

        public double X { set { if (tTrans.X != Math.Round(value)) { tTrans.X = Math.Round(value); } } get { return tTrans.X; } }
    }

    public class NoteExpElement : ExpElement
    {
        protected DrawingVisual visual;

        protected double _visualHeight;
        public double VisualHeight { set { if (_visualHeight != value) { _visualHeight = value; Redraw(); } } get { return _visualHeight; } }
        protected double _scaleX;
        public double ScaleX { set { if (_scaleX != value) { _scaleX = value; Redraw(); } } get { return _scaleX; } }

        UVoicePart _part;
        public UVoicePart Part { set { _part = value; Redraw(); } get { return _part; } }
        public string Key;

        Pen pen3;
        Pen pen2;

        public NoteExpElement()
        {
            pen3 = new Pen(OpenUtau.UI.Models.ThemeManager.NoteFillBrushes[0], 3);
            pen2 = new Pen(OpenUtau.UI.Models.ThemeManager.NoteFillBrushes[0], 2);
            pen3.Freeze();
            pen2.Freeze();
            visual = new DrawingVisual();
            Redraw();
            this.AddVisualChild(visual);
        }

        protected override int VisualChildrenCount
        {
            //get { if (Part == null) return 0; else return 0; }
            get { return 1; }
        }

        protected override Visual GetVisualChild(int index)
        {
            return visual;
        }

        public void Redraw()
        {
            DrawingContext cxt = visual.RenderOpen();
            if (Part != null)
            {
                foreach (UNote note in Part.Notes)
                {
                    if (note.styles.ContainsKey(Key))
                    {
                        double x1 = Math.Round(ScaleX * note.PosTick);
                        double x2 = Math.Round(ScaleX * note.EndTick);
                        double valueHeight = Math.Round(VisualHeight - VisualHeight * (int)note.styles[Key] / 127);
                        cxt.DrawLine(pen3, new Point(x1 + 0.5, VisualHeight + 0.5), new Point(x1 + 0.5, valueHeight + 3));
                        cxt.DrawEllipse(Brushes.White, pen2, new Point(x1 + 0.5, valueHeight), 2.5, 2.5);
                        cxt.DrawLine(pen2, new Point(x1 + 3, valueHeight), new Point(Math.Max(x1 + 3, x2 - 3), valueHeight));
                    }
                }
            }
            else
            {
                cxt.DrawLine(pen2, new Point(0,0), new Point(0,0));
            }
            cxt.Close();
        }
    }
}

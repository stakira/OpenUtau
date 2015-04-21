using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

using OpenUtau.Core.USTx;

namespace OpenUtau.UI.Controls
{
    public enum ExpDisMode {Hidden, Visible, Shadow};

    public class ExpElement : FrameworkElement
    {
        protected TranslateTransform tTrans;

        public ExpElement()
        {
            tTrans = new TranslateTransform();
            this.RenderTransform = tTrans;
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
    }

    public class FloatExpElement : ExpElement
    {
        protected DrawingVisual visual;

        protected double _visualHeight;
        public double VisualHeight { set { if (_visualHeight != value) { _visualHeight = value; MarkUpdate(); } } get { return _visualHeight; } }
        protected double _scaleX;
        public double ScaleX { set { if (_scaleX != value) { _scaleX = value; MarkUpdate(); } } get { return _scaleX; } }

        UVoicePart _part;
        public UVoicePart Part { set { _part = value; MarkUpdate(); } get { return _part; } }
        public string Key;

        Pen pen3;
        Pen pen2;

        public FloatExpElement()
        {
            pen3 = new Pen(OpenUtau.UI.Models.ThemeManager.NoteFillBrushes[0], 3);
            pen2 = new Pen(OpenUtau.UI.Models.ThemeManager.NoteFillBrushes[0], 2);
            pen3.Freeze();
            pen2.Freeze();
            visual = new DrawingVisual();
            MarkUpdate();
            this.AddVisualChild(visual);
        }

        protected override int VisualChildrenCount { get { return 1; } }
        protected override Visual GetVisualChild(int index) { return visual; }

        public void RedrawIfUpdated()
        {
            if (!_updated) return;
            DrawingContext cxt = visual.RenderOpen();
            if (Part != null)
            {
                foreach (UNote note in Part.Notes)
                {
                    if (note.Expressions.ContainsKey(Key))
                    {
                        var _exp = note.Expressions[Key] as FloatExpression;
                        var _expTemplate = DocManager.Inst.Project.ExpressionTable[Key] as FloatExpression;
                        double x1 = Math.Round(ScaleX * note.PosTick);
                        double x2 = Math.Round(ScaleX * note.EndTick);
                        double valueHeight = Math.Round(VisualHeight - VisualHeight * (float)_exp.Data / (_expTemplate.Max - _expTemplate.Min));
                        cxt.DrawLine(pen3, new Point(x1 + 0.5, VisualHeight + 0.5), new Point(x1 + 0.5, valueHeight + 3));
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
}

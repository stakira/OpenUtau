using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using OpenUtau.Core;
using OpenUtau.Core.USTx;

namespace OpenUtau.UI.Controls {
    enum ExpDisMode { Hidden, Visible, Shadow };

    class ExpElement : FrameworkElement {
        protected DrawingVisual visual;

        protected override int VisualChildrenCount { get { return 1; } }
        protected override Visual GetVisualChild(int index) { return visual; }

        protected UVoicePart _part;
        public virtual UVoicePart Part { set { _part = value; MarkUpdate(); } get { return _part; } }

        public string Key;
        protected TranslateTransform tTrans;

        protected double _visualHeight;
        public double VisualHeight { set { if (_visualHeight != value) { _visualHeight = value; MarkUpdate(); } } get { return _visualHeight; } }
        protected double _scaleX;
        public double ScaleX { set { if (_scaleX != value) { _scaleX = value; MarkUpdate(); } } get { return _scaleX; } }

        public ExpElement() {
            tTrans = new TranslateTransform();
            this.RenderTransform = tTrans;
            visual = new DrawingVisual();
            MarkUpdate();
            this.AddVisualChild(visual);
        }

        public double X { set { if (tTrans.X != Math.Round(value)) { tTrans.X = Math.Round(value); } } get { return tTrans.X; } }

        ExpDisMode displayMode;

        public ExpDisMode DisplayMode {
            set {
                if (displayMode != value) {
                    displayMode = value;
                    this.Opacity = displayMode == ExpDisMode.Shadow ? 0.3 : 1;
                    this.Visibility = displayMode == ExpDisMode.Hidden ? Visibility.Hidden : Visibility.Visible;
                    if (this.Parent is Canvas) {
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

        public void RedrawIfUpdated() {
            if (!_updated) return;
            DrawingContext cxt = visual.RenderOpen();
            Redraw(cxt);
            cxt.Close();
            _updated = false;
        }

        public virtual void Redraw(DrawingContext cxt) { }
    }

    class FloatExpElement : ExpElement {
        public OpenUtau.UI.Models.MidiViewModel midiVM;

        Pen pen3;
        Pen pen2;

        public FloatExpElement() {
            pen3 = new Pen(ThemeManager.NoteFillBrushes[0], 3);
            pen2 = new Pen(ThemeManager.NoteFillBrushes[0], 2);
            pen3.Freeze();
            pen2.Freeze();
        }

        public override void Redraw(DrawingContext cxt) {
            if (Part != null) {
                foreach (UNote note in Part.Notes) {
                    if (!midiVM.NoteIsInView(note)) continue;
                    if (note.Expressions.ContainsKey(Key)) {
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
            } else {
                cxt.DrawRectangle(Brushes.Transparent, null, new Rect(new Point(0, 0), new Point(100, 100)));
            }
        }
    }
}

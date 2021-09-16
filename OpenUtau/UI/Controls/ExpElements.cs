using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using OpenUtau.Core;
using OpenUtau.Core.Ustx;

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
        readonly Pen pen3;
        readonly Pen pen2;

        public FloatExpElement() {
            pen3 = new Pen(ThemeManager.NoteFillBrushes[0], 3);
            pen2 = new Pen(ThemeManager.NoteFillBrushes[0], 2);
            pen3.Freeze();
            pen2.Freeze();
        }

        public override void Redraw(DrawingContext cxt) {
            var project = DocManager.Inst.Project;
            if (Part != null) {
                foreach (UNote note in Part.notes) {
                    if (!midiVM.NoteIsInView(note)) {
                        continue;
                    }
                    foreach (var phoneme in note.phonemes) {
                        if (phoneme.Error) {
                            continue;
                        }
                        var (value, overriden) = phoneme.GetExpression(project, Key);
                        var descriptor = project.expressions[Key];
                        double x1 = Math.Round(ScaleX * (note.position + phoneme.position));
                        double x2 = Math.Round(ScaleX * (note.position + phoneme.End));
                        double valueHeight = Math.Round(VisualHeight - VisualHeight * (value - descriptor.min) / (descriptor.max - descriptor.min));
                        double zeroHeight = Math.Round(VisualHeight - VisualHeight * (0f - descriptor.min) / (descriptor.max - descriptor.min));
                        cxt.DrawLine(pen3, new Point(x1 + 0.5, zeroHeight + 0.5), new Point(x1 + 0.5, valueHeight + 3));
                        cxt.DrawLine(pen2, new Point(x1 + 3, valueHeight), new Point(Math.Max(x1 + 3, x2 - 3), valueHeight));
                        cxt.DrawEllipse(overriden ? pen2.Brush : Brushes.White, pen2, new Point(x1 + 0.5, valueHeight), 2.5, 2.5);
                    }
                }
            } else {
                cxt.DrawRectangle(Brushes.Transparent, null, new Rect(new Point(0, 0), new Point(100, 100)));
            }
        }
    }
}

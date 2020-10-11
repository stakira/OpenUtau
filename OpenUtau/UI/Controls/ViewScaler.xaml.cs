using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using OpenUtau.UI;
using OpenUtau.UI.Models;

namespace OpenUtau.UI.Controls
{
    /// <summary>
    /// Interaction logic for ViewScaler.xaml
    /// </summary>
    public class ViewScaledEventArgs : EventArgs
    {
        public double Value { set; get; }
        public ViewScaledEventArgs(double value)
        {
            Value = value;
        }
    }

    public partial class ViewScaler : UserControl
    {
        public event EventHandler ViewScaled;

        bool dragging = false;
        double dragLastX;
        double dragLastY;

        public double Min { set { SetValue(MinProperty, value); } get { return (double)GetValue(MinProperty); } }
        public double Max { set { SetValue(MaxProperty, value); } get { return (double)GetValue(MaxProperty); } }
        public double Value { set { SetValue(ValueProperty, value); } get { return (double)GetValue(ValueProperty); } }
        public Geometry PathData { set { SetValue(PathDataProperty, value); } get { return (Geometry)GetValue(PathDataProperty); } }

        public static readonly DependencyProperty MinProperty = DependencyProperty.Register("Min", typeof(double), typeof(ViewScaler), new PropertyMetadata(0.0, UpdatePathCallBack));
        public static readonly DependencyProperty MaxProperty = DependencyProperty.Register("Max", typeof(double), typeof(ViewScaler), new PropertyMetadata(0.0, UpdatePathCallBack));
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(double), typeof(ViewScaler), new PropertyMetadata(0.0, UpdatePathCallBack, CoerceValueCallBack));
        public static readonly DependencyProperty PathDataProperty = DependencyProperty.Register("PathData", typeof(Geometry), typeof(ViewScaler), new PropertyMetadata(new LineGeometry()));

        public static void UpdatePathCallBack(DependencyObject source, DependencyPropertyChangedEventArgs e) { ((ViewScaler)source).UpdatePath(); }
        public static object CoerceValueCallBack(DependencyObject source, object value) { ViewScaler vs = source as ViewScaler; return Math.Max(vs.Min, Math.Min(vs.Max, (double)value)); }

        public ViewScaler()
        {
            InitializeComponent();
            this.Foreground = ThemeManager.UINeutralBrushNormal;
            this.Background = Brushes.Transparent;
            UpdatePath();
        }

        private void UpdatePath()
        {
            double offset = 7 * Math.Log(Max / Value, 2) / Math.Log(Max / Min, 2);
            double size = offset < 4 ? 4 : 8 - offset;
            if (double.IsNaN(offset) || double.IsNaN(size) ||
                double.IsInfinity(offset) || double.IsInfinity(size)) return;
            PathData = Geometry.Parse(FormattableString.Invariant(
                $"M {8 - size} {offset + size} L 8 {offset} L {8 + size} {offset + size} M {8 - size} {16 - size - offset} L 8 {16 - offset} L {8 + size} {16 - size - offset}"));
        }

        private void Control_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            const double zoomSpeed = 0.001;

            Value *= 1 + zoomSpeed * e.Delta;
            EventHandler handler = ViewScaled;
            if (handler != null) handler(this, new ViewScaledEventArgs(Value));
        }

        private void Control_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            UIElement el = (UIElement)sender;
            el.CaptureMouse();
            dragging = true;
            dragLastX = e.GetPosition(el).X;
            dragLastY = e.GetPosition(el).Y;
            Mouse.OverrideCursor = Cursors.None;
        }

        private void Control_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dragging)
            {
                ((UIElement)sender).ReleaseMouseCapture();
                Mouse.OverrideCursor = null;
                dragging = false;
            }
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                const double zoomSpeed = 0.002;

                bool cursorWarpped = false;
                Control el = (Control)sender;

                Value *= 1 + zoomSpeed * (dragLastY - e.GetPosition(el).Y);
                EventHandler handler = ViewScaled;
                if (handler != null) handler(this, new ViewScaledEventArgs(Value));

                dragLastX = e.GetPosition(el).X;
                dragLastY = e.GetPosition(el).Y;

                // Restrict mouse position
                if (e.GetPosition(el).X < 0)
                {
                    cursorWarpped = true;
                    dragLastX += el.ActualWidth;
                }
                else if (e.GetPosition(el).X > el.ActualWidth)
                {
                    cursorWarpped = true;
                    dragLastX -= el.ActualWidth;
                }

                if (e.GetPosition(el).Y < 0)
                {
                    cursorWarpped = true;
                    dragLastY += el.ActualHeight;
                }
                else if (e.GetPosition(el).Y > el.ActualHeight)
                {
                    cursorWarpped = true;
                    dragLastY -= el.ActualHeight;
                }

                if (cursorWarpped)
                {
                    setCursorPos(el.TransformToAncestor(this).Transform(new Point(dragLastX, dragLastY)));
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        public void setCursorPos(Point point)
        {
            SetCursorPos((int)(PointToScreen(point).X), (int)(PointToScreen(point).Y));
        }

        private void ControlStack_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Foreground = ThemeManager.UINeutralBrushActive;
        }

        private void ControlStack_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Foreground = ThemeManager.UINeutralBrushNormal;
        }

    }
}

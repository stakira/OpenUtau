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

        double _value = 0.5;
        double _min = 1;
        double _max = 10;

        public double Min { set { _min = value; _value = Math.Max(Min, _value); } get { return _min; } }
        public double Max { set { _max = value; _value = Math.Min(Max, _value); } get { return _max; } }
        public double Value {
            set { _value = Math.Max(Min, Math.Min(Max, value)); }
            get { return _value; }
        }
        public Geometry PathData
        {
            set { SetValue(PathDataProperty, value); }
            get { return (Geometry)GetValue(PathDataProperty); }
        }

        public static readonly DependencyProperty PathDataProperty = DependencyProperty.Register("PathData", typeof(Geometry), typeof(ViewScaler), new PropertyMetadata(new LineGeometry()));

        public ViewScaler()
        {
            InitializeComponent();
            this.Foreground = ThemeManager.UINeutralBrushNormal;
            this.Background = Brushes.Transparent;
            Redraw();
        }

        private void Redraw()
        {
            double offset = 7 * Math.Log(Max / Value, 2) / Math.Log(Max / Min, 2);
            double size = offset < 4 ? 4 : 8 - offset;

            PathData = Geometry.Parse(
                " M " + (8 - size) + " " + (offset + size) +
                " L 8 " + (offset).ToString() +
                " L " + (8 + size) + " " + (offset + size) +
                " M " + (8 - size) + " " + (16 - size - offset) +
                " L 8 " + (16 - offset) +
                " L " + (8 + size) + " " + (16 - size - offset));
        }

        private void Control_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            const double zoomSpeed = 0.001;

            Value += zoomSpeed * e.Delta * (Max - Min);
            Redraw();
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

                Value += zoomSpeed * (dragLastY - e.GetPosition(el).Y) * (Max - Min);
                Redraw();
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

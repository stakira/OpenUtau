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

using OpenUtau.UI.Models;

namespace OpenUtau.UI.Controls
{
    /// <summary>
    /// Interaction logic for NavigateDrag.xaml
    /// </summary>
    public class NavDragEventArgs : EventArgs
    {
        public double X { set; get; }
        public double Y { set; get; }
        public NavDragEventArgs(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public partial class NavigateDrag : UserControl
    {
        public event EventHandler NavDrag;

        bool dragging = false;
        double dragLastX;
        double dragLastY;

        const double navigateSpeedX = 0.05;
        const double navigateSpeedY = 0.05;

        public NavigateDrag()
        {
            InitializeComponent();
            this.Foreground = ThemeManager.UINeutralBrushNormal;
            this.Background = Brushes.Transparent;
        }

        private void NavigateDrag_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.None;
            Control el = (Control)sender;
            el.CaptureMouse();
            dragging = true;
            dragLastX = e.GetPosition(el).X;
            dragLastY = e.GetPosition(el).Y;
        }

        private void NavigateDrag_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dragging = false;
            ((Control)sender).ReleaseMouseCapture();
            Mouse.OverrideCursor = null;
        }

        private void NavigateDrag_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                bool cursorWarpped = false;
                Control el = (Control)sender;

                double deltaX = (e.GetPosition(el).X - dragLastX) * navigateSpeedX;
                double deltaY = (e.GetPosition(el).Y - dragLastY) * navigateSpeedY;

                EventHandler handler = NavDrag;
                if (handler != null) handler(this, new NavDragEventArgs(deltaX, deltaY));

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

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Foreground = ThemeManager.UINeutralBrushActive;
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Foreground = ThemeManager.UINeutralBrushNormal;
        }

    }
}

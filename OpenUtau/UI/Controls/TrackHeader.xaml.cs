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

using OpenUtau.Core.USTx;

namespace OpenUtau.UI.Controls
{
    /// <summary>
    /// Interaction logic for TrackHeader.xaml
    /// </summary>
    public partial class TrackHeader : UserControl
    {
        public UTrack Track;

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        public void setCursorPos(Point point)
        {
            SetCursorPos((int)(PointToScreen(point).X), (int)(PointToScreen(point).Y));
        }

        public TrackHeader()
        {
            InitializeComponent();
        }

        long clickTimeMs = 0;
        private void faderSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var slider = sender as Slider;
            int thumbWidth = slider == this.faderSlider ? 33 : 11;
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                slider.Value = 0;
            }
            else
            {
                double x = (slider.Value - slider.Minimum) / (slider.Maximum - slider.Minimum) * (slider.ActualWidth - thumbWidth) + (thumbWidth - 1) / 2;
                double y = e.GetPosition(slider).Y;
                setCursorPos(slider.TransformToAncestor(this).Transform(new Point(x, y)));
                clickTimeMs = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                slider.CaptureMouse();
            }
            e.Handled = true;
        }

        private void faderSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var slider = sender as Slider;
            slider.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void faderSlider_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var slider = sender as Slider;
            int thumbWidth = slider == this.faderSlider ? 33 : 11;
            if (slider.IsMouseCaptured && clickTimeMs + 100 < DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond)
                slider.Value = slider.Minimum + (e.GetPosition(slider).X - (thumbWidth - 1) / 2) / (slider.ActualWidth - thumbWidth) * (slider.Maximum - slider.Minimum);
        }

        private void faderSlider_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var slider = sender as Slider;
            slider.Value += e.Delta / 120 * (slider.Maximum - slider.Minimum) / 50;
        }
    }
}

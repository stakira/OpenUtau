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
        //public int TrackNo { set { SetValue(TrackNoProperty, value); } get { return (int)GetValue(TrackNoProperty); } }
        //public bool Mute { set { SetValue(MuteProperty, value); } get { return (bool)GetValue(MuteProperty); } }
        //public bool Solo { set { SetValue(SoloProperty, value); } get { return (bool)GetValue(SoloProperty); } }
        //public double Volume { set { SetValue(VolumeProperty, value); } get { return (double)GetValue(VolumeProperty); } }
        //public double Pan { set { SetValue(PanProperty, value); } get { return (double)GetValue(PanProperty); } }

        //public static readonly DependencyProperty TrackNoProperty = DependencyProperty.Register("TrackNo", typeof(int), typeof(TrackHeader), new PropertyMetadata(0));
        //public static readonly DependencyProperty MuteProperty = DependencyProperty.Register("Mute", typeof(bool), typeof(TrackHeader), new PropertyMetadata(false));
        //public static readonly DependencyProperty SoloProperty = DependencyProperty.Register("Solo", typeof(bool), typeof(TrackHeader), new PropertyMetadata(false));
        //public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register("Volume", typeof(double), typeof(TrackHeader), new PropertyMetadata(0.0));
        //public static readonly DependencyProperty PanProperty = DependencyProperty.Register("Pan", typeof(double), typeof(TrackHeader), new PropertyMetadata(0.0));

        UTrack _track;
        public UTrack Track { set { _track = value; this.DataContext = value; } get { return _track; } }

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
            if (e.ChangedButton == MouseButton.Right)
            {
                slider.Value = 0;
            }
            else if (e.ChangedButton == MouseButton.Left)
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

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

using OpenUtau.Core;
using OpenUtau.Core.USTx;

namespace OpenUtau.UI.Controls
{
    /// <summary>
    /// Interaction logic for TrackHeader.xaml
    /// </summary>
    public partial class TrackHeader : UserControl
    {
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

        ContextMenu changeSingerMenu;
        private void singerNameButton_Click(object sender, RoutedEventArgs e)
        {
            if (changeSingerMenu == null)
            {
                changeSingerMenu = new ContextMenu();
                changeSingerMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                changeSingerMenu.PlacementTarget = (Button)sender;
                changeSingerMenu.HorizontalOffset = -10;
                foreach (var pair in DocManager.Inst.Singers)
                {
                    var menuItem = new MenuItem() { Header = pair.Value.Name };
                    menuItem.Click += (_o, _e) =>
                    {
                        if (this.Track.Singer != pair.Value)
                        {
                            DocManager.Inst.StartUndoGroup();
                            DocManager.Inst.ExecuteCmd(new TrackChangeSingerCommand(DocManager.Inst.Project, this.Track, pair.Value));
                            DocManager.Inst.EndUndoGroup();
                        }
                    };
                    changeSingerMenu.Items.Add(menuItem);
                }
            }
            changeSingerMenu.IsOpen = true;
            e.Handled = true;
        }

        public void UpdateSingerName()
        {
            this.singerNameButton.GetBindingExpression(Button.ContentProperty).UpdateTarget();
        }

        ContextMenu headerMenu;
        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (headerMenu == null)
                {
                    headerMenu = new ContextMenu();
                    var item = new MenuItem() { Header = "Remove track" };
                    item.Click += (_o, _e) =>
                    {
                        DocManager.Inst.StartUndoGroup();
                        DocManager.Inst.ExecuteCmd(new RemoveTrackCommand(DocManager.Inst.Project, this.Track));
                        DocManager.Inst.EndUndoGroup();
                    };
                    headerMenu.Items.Add(item);
                }
                headerMenu.IsOpen = true;
            }
            e.Handled = true;
        }
    }
}

using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OpenUtau.Api;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.UI.Controls {
    /// <summary>
    /// Interaction logic for TrackHeader.xaml
    /// </summary>
    public partial class TrackHeader : UserControl, INotifyPropertyChanged {

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        UTrack _track;
        public UTrack Track { set { _track = value; this.DataContext = value; } get { return _track; } }

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        public string PhonemizerName => Track.Phonemizer.Name;

        public void setCursorPos(Point point) {
            SetCursorPos((int)(PointToScreen(point).X), (int)(PointToScreen(point).Y));
        }

        public TrackHeader() {
            InitializeComponent();
        }

        long clickTimeMs = 0;
        private void faderSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
            var slider = sender as Slider;
            int thumbWidth = slider == this.faderSlider ? 33 : 11;
            if (e.ChangedButton == MouseButton.Right) {
                slider.Value = 0;
            } else if (e.ChangedButton == MouseButton.Left) {
                double x = (slider.Value - slider.Minimum) / (slider.Maximum - slider.Minimum) * (slider.ActualWidth - thumbWidth) + (thumbWidth - 1) / 2;
                double y = e.GetPosition(slider).Y;
                setCursorPos(slider.TransformToAncestor(this).Transform(new Point(x, y)));
                clickTimeMs = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                slider.CaptureMouse();
            }
            e.Handled = true;
        }

        private void faderSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
            var slider = sender as Slider;
            slider.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void faderSlider_PreviewMouseMove(object sender, MouseEventArgs e) {
            var slider = sender as Slider;
            int thumbWidth = slider == this.faderSlider ? 33 : 11;
            if (slider.IsMouseCaptured && clickTimeMs + 100 < DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond)
                slider.Value = slider.Minimum + (e.GetPosition(slider).X - (thumbWidth - 1) / 2) / (slider.ActualWidth - thumbWidth) * (slider.Maximum - slider.Minimum);
        }

        private void faderSlider_MouseWheel(object sender, MouseWheelEventArgs e) {
            var slider = sender as Slider;
            slider.Value += e.Delta / 120 * (slider.Maximum - slider.Minimum) / 50;
        }

        private void buildChangeSingerMenuItems() {
            changeSingerMenu.Items.Clear();
            foreach (var singer in DocManager.Inst.Singers.Values.OrderBy(singer => singer.Name)) {
                var menuItem = new MenuItem() { Header = singer.Name };
                menuItem.Click += (_o, _e) => {
                    if (Track.Singer != singer) {
                        DocManager.Inst.StartUndoGroup();
                        DocManager.Inst.ExecuteCmd(new TrackChangeSingerCommand(DocManager.Inst.Project, Track, singer));
                        DocManager.Inst.EndUndoGroup();
                    }
                };
                changeSingerMenu.Items.Add(menuItem);
            }
        }

        public void UpdateTrackNo() {
            trackNoText.GetBindingExpression(TextBlock.TextProperty).UpdateTarget();
        }

        ContextMenu changeSingerMenu;
        private void singerNameButton_Click(object sender, RoutedEventArgs e) {
            if (changeSingerMenu == null) {
                changeSingerMenu = new ContextMenu();
                changeSingerMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                changeSingerMenu.PlacementTarget = (Button)sender;
                changeSingerMenu.HorizontalOffset = -10;
            }
            if (DocManager.Inst.Singers.Count != 0) {
                buildChangeSingerMenuItems();
                changeSingerMenu.IsOpen = true;
            }
            e.Handled = true;
        }

        public void UpdateSingerName() {
            this.singerNameButton.GetBindingExpression(Button.ContentProperty).UpdateTarget();
        }

        private void buildChangePhonemizerMenuItems() {
            changePhonemizerMenu.Items.Clear();
            foreach (var factory in DocManager.Inst.PhonemizerFactories) {
                var menuItem = new MenuItem() {
                    Header = factory.ToString(),
                    IsChecked = factory.type == Track.Phonemizer.GetType(),
                };
                menuItem.Click += (_o, _e) => {
                    if (Track.Phonemizer.GetType() != factory.type) {
                        Phonemizer newPhonemizer;
                        try {
                            newPhonemizer = factory.Create();
                        } catch (Exception e) {
                            Log.Error(e, $"Failed to create {factory}");
                            DocManager.Inst.ExecuteCmd(new UserMessageNotification(
                                $"Failed to create {factory}\n\n" + e.ToString()));
                            newPhonemizer = null;
                        }
                        if (newPhonemizer == null) {
                            return;
                        }
                        DocManager.Inst.StartUndoGroup();
                        DocManager.Inst.ExecuteCmd(new TrackChangePhonemizerCommand(DocManager.Inst.Project, Track, newPhonemizer));
                        DocManager.Inst.EndUndoGroup();
                        UpdatePhonemizerName();
                    }
                };
                changePhonemizerMenu.Items.Add(menuItem);
            }
        }

        public void UpdatePhonemizerName() {
            phonemizerButton.GetBindingExpression(Button.ContentProperty).UpdateTarget();
        }

        ContextMenu changePhonemizerMenu;
        private void phonemizerButton_Click(object sender, RoutedEventArgs e) {
            if (changePhonemizerMenu == null) {
                changePhonemizerMenu = new ContextMenu();
                changePhonemizerMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                changePhonemizerMenu.PlacementTarget = (Button)sender;
                changePhonemizerMenu.HorizontalOffset = -10;
            }
            buildChangePhonemizerMenuItems();
            changePhonemizerMenu.IsOpen = true;
            e.Handled = true;
        }

        private void faderSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            DocManager.Inst.ExecuteCmd(new VolumeChangeNotification(Track.TrackNo, ((Slider)sender).Value));
        }

        private void RemoveTrackClicked(object sender, RoutedEventArgs e) {
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new WillRemoveTrackNotification(Track.TrackNo));
            DocManager.Inst.ExecuteCmd(new RemoveTrackCommand(DocManager.Inst.Project, Track));
            DocManager.Inst.EndUndoGroup();
        }

        private void MoveUpClicked(object sender, RoutedEventArgs e) {
            if (Track == DocManager.Inst.Project.tracks.First()) {
                return;
            }
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new MoveTrackCommand(DocManager.Inst.Project, Track, true));
            DocManager.Inst.EndUndoGroup();
        }

        private void MoveDownClicked(object sender, RoutedEventArgs e) {
            if (Track == DocManager.Inst.Project.tracks.Last()) {
                return;
            }
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new MoveTrackCommand(DocManager.Inst.Project, Track, false));
            DocManager.Inst.EndUndoGroup();
        }
    }
}

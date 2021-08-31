using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;

namespace OpenUtau.App.Views {
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        void OnEditTempo(object sender, PointerPressedEventArgs args) {

        }

        void OnMenuSingers(object sender, RoutedEventArgs args) {
            var dialog = new SingersDialog() {
                DataContext = new SingersViewModel(),
            };
            dialog.ShowDialog(this);
        }

        void OnMenuPreferences(object sender, RoutedEventArgs args) {
            var dialog = new PreferencesDialog() {
                DataContext = new PreferencesViewModel(),
            };
            dialog.ShowDialog(this);
        }

        void OnPlayOrStop(object sender, RoutedEventArgs args) {
            var vm = DataContext as MainWindowViewModel;
            if (vm == null) {
                return;
            }
            if (!vm.PlayOrStop()) {
                MessageBox.Show(
                   this,
                   "dialogs.noresampler.message",
                   "dialogs.noresampler.caption",
                   MessageBox.MessageBoxButtons.Ok);
            }
        }

        public void TimelinePointerWheelChanged(object sender, PointerWheelEventArgs args) {
            var canvas = (Canvas)sender;
            var position = args.GetCurrentPoint((IVisual)sender).Position;
            var size = canvas.Bounds.Size;
            position = position.WithX(position.X / size.Width).WithY(position.Y / size.Height);
            (DataContext as MainWindowViewModel)?.OnXZoomed(position, 0.05 * args.Delta.Y);
        }

        public void ZoomerPointerWheelChanged(object sender, PointerWheelEventArgs args) {
            (DataContext as MainWindowViewModel)?.OnYZoomed(new Point(0, 0.5), 0.05 * args.Delta.Y);
        }
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
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
            DocManager.Inst.SearchAllSingers();
            var dialog = new SingersDialog() {
                DataContext = new SingersViewModel(),
            };
            dialog.ShowDialog(this);
        }

        void OnMenuPreferences(object sender, RoutedEventArgs args) {
            var dialog = new PreferencesDialog();
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
    }
}

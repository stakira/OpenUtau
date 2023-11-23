using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class PreferencesDialog : Window {
        public PreferencesDialog() {
            InitializeComponent();
        }

        async void SelectAddlSingersPath(object sender, RoutedEventArgs e) {
            var dialog = new SingerLocationDialog();
            await dialog.ShowDialog(this);
        }

        void ResetVLabelerPath(object sender, RoutedEventArgs e) {
            ((PreferencesViewModel)DataContext!).SetVLabelerPath(string.Empty);
        }

        async void SelectVLabelerPath(object sender, RoutedEventArgs e) {
            var type = OS.IsWindows() ? FilePicker.EXE : OS.IsMacOS() ? FilePicker.APP : FilePickerFileTypes.All;
            var path = await FilePicker.OpenFile(this, "prefs.advanced.vlabelerpath", type);
            if (string.IsNullOrEmpty(path)) {
                return;
            }
            if (OS.AppExists(path)) {
                ((PreferencesViewModel)DataContext!).SetVLabelerPath(path);
            }
        }
    }
}

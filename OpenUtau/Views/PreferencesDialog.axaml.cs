using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class PreferencesDialog : Window {
        public PreferencesDialog() {
            InitializeComponent();
        }

        void ResetAddlSingersPath(object sender, RoutedEventArgs e) {
            ((PreferencesViewModel)DataContext!).SetAddlSingersPath(string.Empty);
        }

        async void SelectAddlSingersPath(object sender, RoutedEventArgs e) {
            var path = await FilePicker.OpenFolder(this, "prefs.paths.addlsinger");
            if (string.IsNullOrEmpty(path)) {
                return;
            }
            if (Directory.Exists(path)) {
                ((PreferencesViewModel)DataContext!).SetAddlSingersPath(path);
            }
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

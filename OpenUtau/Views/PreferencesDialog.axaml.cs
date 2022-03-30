using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class PreferencesDialog : Window {
        public PreferencesDialog() {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        void ResetAddlSingersPath(object sender, RoutedEventArgs e) {
            ((PreferencesViewModel)DataContext!).SetAddlSingersPath(string.Empty);
        }

        async void SelectAddlSingersPath(object sender, RoutedEventArgs e) {
            var dialog = new OpenFolderDialog();
            var path = await dialog.ShowAsync(this);
            if (string.IsNullOrEmpty(path)) {
                return;
            }
            if (Directory.Exists(path)) {
                ((PreferencesViewModel)DataContext!).SetAddlSingersPath(path);
            }
        }
    }
}

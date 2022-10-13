using System.Collections.Generic;
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

        void ResetVLabelerPath(object sender, RoutedEventArgs e) {
            ((PreferencesViewModel)DataContext!).SetVLabelerPath(string.Empty);
        }

        async void SelectVLabelerPath(object sender, RoutedEventArgs e) {
            var extension = OS.IsWindows() ? "exe" : OS.IsMacOS() ? "app" : "*";
            var dialog = new OpenFileDialog() {
                AllowMultiple = false,
                Filters = new List<FileDialogFilter>() {
                    new FileDialogFilter() {
                         Name = "vLabeler",
                         Extensions = new List<string>() { extension },
                    }
                }
            };
            var paths = await dialog.ShowAsync(this);
            if (paths == null || paths.Length != 1 || string.IsNullOrEmpty(paths[0])) {
                return;
            }
            if (OS.AppExists(paths[0])) {
                ((PreferencesViewModel)DataContext!).SetVLabelerPath(paths[0]);
            }
        }
    }
}

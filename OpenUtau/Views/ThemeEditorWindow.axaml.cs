using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;
using ReactiveUI;

namespace OpenUtau.App.Views {
    public partial class ThemeEditorWindow : Window {
        
        public static bool IsOpen { get; private set; }

        public ThemeEditorWindow(string customThemePath) {
            InitializeComponent();
            DataContext = new ThemeEditorViewModel(customThemePath);
            IsOpen = true;
        }

        void OnCancel(object? sender, RoutedEventArgs e) {
            Close();
        }

        void OnSave(object? sender, RoutedEventArgs e) {
            (DataContext as ThemeEditorViewModel)!.Save();
            Close();
        }

        public void WindowClosing(object? sender, WindowClosingEventArgs e) {
            IsOpen = false;
            EnableThemeSelect();
            App.SetTheme();
        }

        private static void EnableThemeSelect() {
            if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime) {
                var openWindows = desktopLifetime.Windows.ToList();
                foreach (var window in openWindows) {
                    if (window is PreferencesDialog prefDialog) {
                        var dataCtx = (PreferencesViewModel) prefDialog.DataContext!;
                        dataCtx.RaisePropertyChanged(nameof(dataCtx.IsThemeEditorOpen));
                    }
                }
            }
        }

    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class LyricsDialog : Window {
        public LyricsDialog() {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        void OnCancel(object? sender, RoutedEventArgs e) {
            Close();
        }

        void OnApply(object? sender, RoutedEventArgs e) {
            (DataContext as LyricsViewModel)!.Apply();
            Close();
        }
    }
}

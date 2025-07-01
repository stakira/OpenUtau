using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class LyricsReplaceDialog : Window {
        public LyricsReplaceDialog() {
            InitializeComponent();
        }

        void OnCancel(object? sender, RoutedEventArgs e) {
            Close();
        }

        void OnFinish(object? sender, RoutedEventArgs e) {
            if((DataContext as LyricsReplaceViewModel)!.Finish()) {
                Close();
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Enter:
                    OnFinish(sender, e);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    OnCancel(sender, e);
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }
    }
}

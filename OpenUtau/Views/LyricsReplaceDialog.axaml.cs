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
<<<<<<< HEAD
=======
            KeyboardDevice.Instance.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None);
>>>>>>> parent of d60f4037 (upgrade to avalonia 11 and fix compilation)
            Close();
        }

        void OnFinish(object? sender, RoutedEventArgs e) {
            (DataContext as LyricsReplaceViewModel)!.Finish();
<<<<<<< HEAD
=======
            KeyboardDevice.Instance.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None);
>>>>>>> parent of d60f4037 (upgrade to avalonia 11 and fix compilation)
            Close();
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

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class LyricsDialog : Window {
        public LyricsDialog() {
            InitializeComponent();
        }

        void OnOpened(object? sender, EventArgs e) {
            DIALOG_Box.Focus();
        }

        void OnReset(object? sender, RoutedEventArgs e) {
            (DataContext as LyricsViewModel)!.Reset();
        }

        void OnCancel(object? sender, RoutedEventArgs e) {
            (DataContext as LyricsViewModel)!.Cancel();
            //KeyboardDevice.Instance.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None);
            Close();
        }

        void OnFinish(object? sender, RoutedEventArgs e) {
            (DataContext as LyricsViewModel)!.Finish();
            //KeyboardDevice.Instance.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None);
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

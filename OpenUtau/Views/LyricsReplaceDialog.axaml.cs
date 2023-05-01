using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class LyricsReplaceDialog : Window {

        public LyricsReplaceDialog() {
            InitializeComponent();

#if DEBUG
            this.AttachDevTools();
#endif
        }
        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        void OnCancel(object? sender, RoutedEventArgs e) {
            (DataContext as LyricsViewModel)!.Cancel();
            KeyboardDevice.Instance.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None);
            Close();
        }

        void OnFinish(object? sender, RoutedEventArgs e) {
            (DataContext as LyricsViewModel)!.Finish();
            KeyboardDevice.Instance.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None);
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

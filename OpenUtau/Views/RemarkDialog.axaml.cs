using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class RemarkDialog : Window {

        public RemarkDialog() {
            InitializeComponent();
        }

        void OnOpened(object? sender, EventArgs e) {
            DIALOG_Box.Focus();
        }
        void OnCancel(object? sender, RoutedEventArgs e) {
            (DataContext as RemarkViewModel)!.Cancel();
            Close();
        }

        void OnFinish(object? sender, RoutedEventArgs e) {
            (DataContext as RemarkViewModel)!.Finish();
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

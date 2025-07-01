using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using Org.BouncyCastle.Tls;

namespace OpenUtau.App.Views {
    public partial class TrackColorDialog : Window {
        public TrackColorDialog() {
            InitializeComponent();
        }

        void OnCancel(object? sender, RoutedEventArgs e) {
            Close();
        }

        void OnFinish(object? sender, RoutedEventArgs e) {
            (DataContext as TrackColorViewModel)!.Finish();
            Close();
        }

        private void OnKeyDown(object? sender, KeyEventArgs e) {
            if (e.Key == Key.Escape) {
                e.Handled = true;
                Close();
            } else if (e.Key == Key.Enter) {
                e.Handled = true;
                OnFinish(sender, e);
            } else {
                base.OnKeyDown(e);
            }
        }
    }
}

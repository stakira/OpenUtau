using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class ExpressionsDialog : Window {
        public ExpressionsDialog() {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        private void ApplyButtonClicked(object sender, RoutedEventArgs _) {
            try {
                (DataContext as ExpressionsViewModel)?.Apply();
                Close();
            } catch (ArgumentException e) {
                MessageBox.Show(this, e.ToString(), "Error", MessageBox.MessageBoxButtons.Ok);
            }
        }
    }
}

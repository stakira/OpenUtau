using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class DebugWindow : Window {
        DebugViewModel viewModel;

        public DebugWindow() {
            DataContext = viewModel = new DebugViewModel();
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            viewModel.Attach();
        }

        void OnClosed(object? sender, EventArgs e) {
            viewModel.Detach();
        }
    }
}

using System;
using Avalonia.Controls;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class DebugWindow : Window {
        DebugViewModel viewModel;

        public DebugWindow() {
            InitializeComponent();
            DataContext = viewModel = new DebugViewModel();
            viewModel.SetWindow(this);
            viewModel.Attach();
        }
        public void CopyLogText() {
            CopyTextBox.Text = DebugViewModel.Sink.Inst.ToString();
            CopyTextBox.SelectAll();
            CopyTextBox.Copy();
            CopyTextBox.Clear();
        }

        void OnClosed(object? sender, EventArgs e) {
            viewModel.Detach();
        }
    }
}

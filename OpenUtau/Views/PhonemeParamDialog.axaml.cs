using Avalonia.Controls;
using Avalonia.Input;

namespace OpenUtau.App.Views {
    public partial class PhonemeParamDialog : Window {
        public PhonemeParamDialog() {
            InitializeComponent();
        }

        private void TextBox_KeyDown(object? sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                TopLevel.GetTopLevel(this)?.FocusManager?.ClearFocus();
            }
        }
    }
}

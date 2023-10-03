using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace OpenUtau.App.Views {
    public partial class VoiceColorMappingDialog : Window {
        public Action? onFinish;
        public bool Apply { get; private set; } = false;

        public VoiceColorMappingDialog() {
            InitializeComponent();
        }

        private void Ok_OnClick(object sender, RoutedEventArgs e) => Finish();

        private void Cancel_OnClick(object sender, RoutedEventArgs e) => Close();

        private void Finish() {
            Apply = true;
            if (onFinish != null) {
                onFinish.Invoke();
            }
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            if (e.Key == Key.Escape) {
                e.Handled = true;
                Close();
            } else if (e.Key == Key.Enter) {
                e.Handled = true;
                Finish();
            } else {
                base.OnKeyDown(e);
            }
        }
    }
}

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class TypeInDialog : Window {
        public Action<string>? onFinish;

        private Button button;
        private TextBox textBox;

        public TypeInDialog() {
            InitializeComponent();
            button = this.FindControl<Button>("OkButton");
            button.Click += OkButtonClick;
            textBox = this.FindControl<TextBox>("TextBox");
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        public void SetText(string text) {
            textBox.Text = text;
        }

        private void OkButtonClick(object? sender, RoutedEventArgs e) {
            Finish();
        }

        private void Finish() {
            if (onFinish != null) {
                onFinish.Invoke(textBox.Text);
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

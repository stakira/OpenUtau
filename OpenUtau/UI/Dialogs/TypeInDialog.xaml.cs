using System;
using System.Windows;
using System.Windows.Input;

namespace OpenUtau.UI.Dialogs {
    /// <summary>
    /// Interaction logic for TypeInDialog.xaml
    /// </summary>
    public partial class TypeInDialog : Window {
        public TypeInDialog() {
            InitializeComponent();
        }

        public Action<string> onFinish;

        private void Button_Click(object sender, RoutedEventArgs e) {
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
                Close();
            } else if (e.Key == Key.Enter) {
                Finish();
            } else {
                base.OnKeyDown(e);
            }
        }
    }
}

using System;
using System.Windows;

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
            if (onFinish != null) {
                onFinish.Invoke(textBox.Text);
            }
            Close();
        }
    }
}

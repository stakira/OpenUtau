using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OpenUtau.App.ViewModels;
using OpenUtau.Core.Ustx;

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
                MessageBox.Show(
                    this,
                    e.Message,
                    ThemeManager.GetString("errors.caption"),
                    MessageBox.MessageBoxButtons.Ok);
            } catch (Exception e) {
                MessageBox.Show(
                    this,
                    e.ToString(),
                    ThemeManager.GetString("errors.caption"),
                    MessageBox.MessageBoxButtons.Ok);
            }
        }

        private void OnExpressionTypeChanged(object sender, SelectionChangedEventArgs e) {
            var comboBox = (ComboBox)sender;
            var vm = DataContext as ExpressionsViewModel;
            if (vm?.Expression != null) {
                vm.Expression!.ExpressionType = (UExpressionType)comboBox.SelectedIndex;
            }
        }
    }
}

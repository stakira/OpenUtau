using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class PhoneticAssistant : Window {
        PhoneticAssistantViewModel viewModel;
        public PhoneticAssistant() {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            DataContext = viewModel = new PhoneticAssistantViewModel();
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        public void OnCopy(object sender, RoutedEventArgs e) {
            Application.Current?.Clipboard?.SetTextAsync(viewModel.Phonemes);
        }
    }
}

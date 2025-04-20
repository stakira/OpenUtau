using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenUtau.App;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;

namespace OpenUtau.App.Controls {
    public partial class WelcomePage : UserControl {
        private WelcomePageViewModel viewModel;
        public WelcomePage() {
            InitializeComponent();
            DataContext = viewModel = new WelcomePageViewModel();
        }

        public void Show() {
            viewModel.IsVisible = true;
        }
        public void OnNew(object sender, RoutedEventArgs e) {
        
        }
    }
}
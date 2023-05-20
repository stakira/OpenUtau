using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Classic;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class ExeSetupDialog : Window {
        public ExeSetupDialog() {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        public void InstallAsResampler(object sender, RoutedEventArgs arg) {
            var viewModel = DataContext as ExeSetupViewModel;
            if (viewModel == null) {
                return;
            }
            ExeInstaller.Install(viewModel.filePath, ExeType.resampler);
            Close();
        }

        public void InstallAsWavtool(object sender, RoutedEventArgs arg) {
            var viewModel = DataContext as ExeSetupViewModel;
            if (viewModel == null) {
                return;
            }
            ExeInstaller.Install(viewModel.filePath, ExeType.wavtool);
            Close();
        }
    }
}

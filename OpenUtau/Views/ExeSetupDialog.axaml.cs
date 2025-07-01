using Avalonia.Controls;
using Avalonia.Interactivity;
using Classic;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class ExeSetupDialog : Window {
        public ExeSetupDialog() {
            InitializeComponent();
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

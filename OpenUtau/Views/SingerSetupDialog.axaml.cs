using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;
using Serilog;

namespace OpenUtau.App.Views {
    public partial class SingerSetupDialog : Window {
        public SingerSetupDialog() {
            InitializeComponent();
        }

        void InstallClicked(object sender, RoutedEventArgs arg) {
            var viewModel = DataContext as SingerSetupViewModel;
            if (viewModel == null) {
                return;
            }
            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            var task = viewModel.Install();
            task.ContinueWith((task) => {
                if (task.IsFaulted) {
                    Log.Error(task.Exception, "Failed to install singer");
                    if (Parent is Window window) {
                        MessageBox.ShowError(window, task.Exception);
                    }
                }
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, scheduler);
            Close();
        }
    }
}

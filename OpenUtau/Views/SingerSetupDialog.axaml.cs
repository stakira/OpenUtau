using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using Serilog;

namespace OpenUtau.App.Views {
    public partial class SingerSetupDialog : Window {
        public SingerSetupDialog() {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
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
                    MessageBox.ShowError((Window)Parent, task.Exception);
                }
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, scheduler);
            Close();
        }
    }
}

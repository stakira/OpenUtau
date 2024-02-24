using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Serilog;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class SingerPublishDialog : Window {
        public SingerPublishDialog() {
            InitializeComponent();
        }

        async void PublishClicked(object sender, RoutedEventArgs arg){
            //var outputFile = "C:/users/lin/desktop/1.zip";
            var outputFile = await FilePicker.SaveFile(
                this, "singers.publish", FilePicker.ZIP);
            if (outputFile == null) {
                return;
            }
            Publish(outputFile);
        }

        void Publish(string outputFile){
            var viewModel = DataContext as SingerPublishViewModel;
            if (viewModel == null) {
                return;
            }
            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            viewModel.Publish(outputFile).ContinueWith((task) => {
                if (task.IsFaulted) {
                    Log.Error(task.Exception, "Failed to publish singer");
                    if (Parent is Window window) {
                        MessageBox.ShowError(window, task.Exception);
                    }
                }
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, scheduler);
            Close();
        }
    }
}
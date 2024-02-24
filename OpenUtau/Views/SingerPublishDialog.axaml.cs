using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Serilog;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class SingerPublishDialog : Window {
        public SingerPublishDialog() {
            InitializeComponent();
        }

        async void PublishClicked(object sender, RoutedEventArgs arg){
            var viewModel = DataContext as SingerPublishViewModel;
            if (viewModel == null) {
                return;
            }
            var singer = viewModel.singer;
            if(singer == null){
                return;
            }
            var types = FilePicker.ZIP;
            if(File.Exists(singer.Location)){
                var suffix = Path.GetExtension(singer.Location);
                types = new FilePickerFileType(suffix.ToUpper()) {
                    Patterns = new[] { "*" + suffix },
                };
            }
            var outputFile = await FilePicker.SaveFile(
                this, "singers.publish", types);
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
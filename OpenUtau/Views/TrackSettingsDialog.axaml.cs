using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.App.Views {
    public partial class TrackSettingsDialog : Window {

        TrackSettingsViewModel viewModel;

        public TrackSettingsDialog() : this(new UTrack(DocManager.Inst.Project)) { }

        public TrackSettingsDialog(UTrack track) {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            DataContext = viewModel = new TrackSettingsViewModel(track);
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        public void OnOkClicked(object sender, RoutedEventArgs e) {
            viewModel.Finish();
            Close();
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using NetSparkleUpdater.Enums;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.App.Views {
    public partial class DawIntegrationTerminalDialog : Window {
        public readonly DawIntegrationTerminalViewModel ViewModel;
        public DawIntegrationTerminalDialog() {
            InitializeComponent();
            DataContext = ViewModel = new DawIntegrationTerminalViewModel();
        }

        void OnClosing(object sender, WindowClosingEventArgs e) {
        }

        private void OnDownloadClick(object sender, RoutedEventArgs args) {
            try {
                OS.OpenWeb("https://example.com");
            } catch (Exception e) {
                DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(e));
            }
        }
    }
}

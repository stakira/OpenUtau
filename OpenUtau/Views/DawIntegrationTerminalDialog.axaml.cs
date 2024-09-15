using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using NetSparkleUpdater.Enums;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using OpenUtau.Core.DawIntegration;
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

        async void OnConnect(object sender, RoutedEventArgs args) {
            try {
                await ViewModel.Connect();

                DocManager.Inst.ExecuteCmd(new DawConnectedNotification());
                Close();
            } catch (Exception e) {
                DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(e));
            }
        }

        void OnDownloadClick(object sender, RoutedEventArgs args) {
            try {
                OS.OpenWeb("https://example.com");
            } catch (Exception e) {
                DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(e));
            }
        }
    }
}

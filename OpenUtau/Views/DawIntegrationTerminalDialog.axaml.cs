using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Avalonia.Controls;
using Avalonia.Interactivity;
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

        void FormatServerList(object sender, ListControlConvertEventArgs e) {
            var server = (Server)e.ListItem!;
            e.Value = $"{server.Name} (${server.Port})";
        }

        void OnRefresh(object sender, RoutedEventArgs args) {
            Task.Run(() => ViewModel.RefreshServerList());
        }

        void OnClosing(object sender, WindowClosingEventArgs e) {
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

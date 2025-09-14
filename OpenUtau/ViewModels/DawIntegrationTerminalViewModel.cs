using DynamicData.Binding;
using OpenUtau.Core;
using OpenUtau.Core.DawIntegration;
using ReactiveUI.Fody.Helpers;
using System.Threading.Tasks;

namespace OpenUtau.App.ViewModels {
    public class DawIntegrationTerminalViewModel : ViewModelBase {
        [Reactive] public DawServer? SelectedServer { get; set; }
        [Reactive] public bool CanConnect { get; set; } = true;
        public ObservableCollectionExtended<DawServer> ServerList { get; set; } = new ObservableCollectionExtended<DawServer>();

        public DawIntegrationTerminalViewModel() {
            Task.Run(() => RefreshServerList());
        }

        public async Task RefreshServerList() {
            var servers = await DawServerFinder.FindServers();

            ServerList.Load(servers);
            if (servers.Count == 0) {
                SelectedServer = null;
            } else {
                SelectedServer = ServerList[0];
            }
        }

        public async Task Connect() {
            if (SelectedServer == null) {
                return;
            }
            try {
                CanConnect = false;
                var (client, ustx) = await DawClient.Connect(SelectedServer);

                if (ustx.Length > 0) {
                    DocManager.Inst.ExecuteCmd(new LoadProjectNotification(Core.Format.Ustx.LoadText(ustx)));
                }
                DawManager.Inst.dawClient = client;
            } finally {
                CanConnect = true;
            }
        }

    }
}

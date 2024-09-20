using DynamicData.Binding;
using OpenUtau.Core;
using OpenUtau.Core.DawIntegration;
using ReactiveUI.Fody.Helpers;
using System.Threading.Tasks;

namespace OpenUtau.App.ViewModels {
    public class DawIntegrationTerminalViewModel : ViewModelBase {
        [Reactive] public Server? SelectedServer { get; set; } = null;
        [Reactive] public bool CanConnect { get; set; } = true;
        public ObservableCollectionExtended<Server> ServerList { get; set; } = new ObservableCollectionExtended<Server>();

        public DawIntegrationTerminalViewModel() {
            Task.Run(() => RefreshServerList());
        }

        public async Task RefreshServerList() {
            var servers = await ServerFinder.FindServers();

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
                var (client, ustx) = await Client.Connect(SelectedServer.Port);

                if (ustx.Length > 0) {
                    DocManager.Inst.ExecuteCmd(new LoadProjectNotification(Core.Format.Ustx.Load(ustx)));
                }
                DocManager.Inst.dawClient = client;
            } finally {
                CanConnect = true;
            }
        }

    }
}

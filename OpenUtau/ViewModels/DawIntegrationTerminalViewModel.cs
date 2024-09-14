using DynamicData.Binding;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenUtau.Core.DawIntegration;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.App.ViewModels {
    public class DawIntegrationTerminalViewModel : ViewModelBase {
        [Reactive] public Server? SelectedServer { get; set; } = null;
        public ObservableCollectionExtended<Server> ServerList { get; set; } = new ObservableCollectionExtended<Server>();

        public DawIntegrationTerminalViewModel() {
            Task.Run(() => RefreshServerList());
        }

        public async Task RefreshServerList() {
            var servers = await ServerFinder.FindServers();

            if (servers.Count == 0) {
                SelectedServer = null;
            } else {
                ServerList.Load(servers);
                SelectedServer = ServerList[0];
            }
        }

    }
}

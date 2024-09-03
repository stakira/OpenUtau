using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenUtau.App.ViewModels {
    public class DawIntegrationTerminalViewModel : ViewModelBase {
        [Reactive] public DawIntegrationServer? SelectedServer { get; set; } = null;
        public List<DawIntegrationServer> DawIntegrationServers = new List<DawIntegrationServer>();
    }

    public class DawIntegrationServer {
        public int Port;
        public string Name;

        DawIntegrationServer(int port, string name) {
            Port = port;
            Name = name;
        }

        public override string ToString() {
            return Name;
        }
    }


}

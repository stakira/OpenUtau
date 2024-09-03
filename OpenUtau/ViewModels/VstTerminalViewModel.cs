using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenUtau.App.ViewModels {
    public class VstTerminalViewModel : ViewModelBase {
        [Reactive] public VstServer? SelectedServer { get; set; } = null;
        public List<VstServer> VstServers = new List<VstServer>();
    }

    public class VstServer {
        public int Port;
        public string Name;

        VstServer(int port, string name) {
            Port = port;
            Name = name;
        }

        public override string ToString() {
            return Name;
        }
    }


}

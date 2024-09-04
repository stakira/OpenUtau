using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OpenUtau.Core.DawIntegration {
    public class ServerFinder {
        private static string getServerPath() {
            var temp = Path.GetTempPath();

            return $"{temp}/OpenUtau/PluginServers";
        }
        public static async Task<List<Server>> FindServers() {
            var path = getServerPath();
            if (!Directory.Exists(path)) {
                return new List<Server>();
            }

            var di = new DirectoryInfo(path);
            var files = di.GetFiles("*.json");

            var servers = new List<Server>();
            foreach (FileInfo file in files) {
                var json = await File.ReadAllTextAsync(file.FullName);
                var server = JsonConvert.DeserializeObject<Server>(json);
                if (server != null) {
                    servers.Add(server);
                }
            }

            return servers;
        }
    }

    public class Server {
        public int Port { get; }
        public string Name { get; }
        Server(int port, string name) {
            Port = port;
            Name = name;
        }

        public override string ToString() {
            return Name;
        }
    }
}

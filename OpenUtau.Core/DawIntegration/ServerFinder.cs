using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace OpenUtau.Core.DawIntegration {
    public class ServerFinder {
        private static string getServerPath() {
            string temp = Path.GetTempPath();

            return $"{temp}/OpenUtau/VstServers";
        }
        public static List<Server> FindServers() {
            string path = getServerPath();
            if (!Directory.Exists(path)) {
                return new List<Server>();
            }

            DirectoryInfo di = new DirectoryInfo(path);
            FileInfo[] files = di.GetFiles("*.json");

            List<Server> servers = new List<Server>();
            foreach (FileInfo file in files) {
                string json = File.ReadAllText(file.FullName);
                Server? server = JsonConvert.DeserializeObject<Server>(json);
                if (server != null) {
                    servers.Add(server);
                }
            }

            return servers;
        }
    }

    public class Server {
        public int Port { get; set; }
        public string Name { get; set; }
        Server(int port, string name) {
            Port = port;
            Name = name;
        }

        public override string ToString() {
            return Name;
        }
    }
}

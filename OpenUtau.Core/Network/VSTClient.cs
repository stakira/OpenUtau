using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenUtau.Core.Network {
    public class VSTClient {
        public VSTClient(string host,int port = 1556) {
            this.port = port;
            this.host = host;
            Console.WriteLine("Created VST client");
        }

        public async Task Connect() {
            try {
                using TcpClient tcpClient = new TcpClient();
                Console.WriteLine("Connecting to the tcp server");
                if (!tcpClient.ConnectAsync(host, port).Wait(5000)) {
                    Console.WriteLine("Timed out connecting to the server");
                    return;
                }
                Console.WriteLine("Connected to the server");
                byte[] buffer = new byte[128];
                var stream = tcpClient.GetStream();
                stream.ReadTimeout = 1000;
                stream.WriteTimeout = 1000;
                await stream.ReadAsync(buffer);
                Console.WriteLine(Encoding.ASCII.GetString(buffer));

            } catch (Exception ex) {
                Console.WriteLine($@"An exception occurred : {ex.Message}");
            }
        }

        private int port;
        private string host;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vortice.Direct3D;
using WanaKanaNet.Helpers;

namespace OpenUtau.Core.Vst {
    public class Client {
        static int VERSION = 1;
        private readonly int port;
        private TcpClient tcpClient;
        private Task? receiver;
        private CancellationTokenSource? cancellationTokenSource;
        private Dictionary<string, Action<string>> handlers = new Dictionary<string, Action<string>>();

        private Client(int port) {
            this.port = port;
            this.tcpClient = new TcpClient();
        }

        ~Client() {
            tcpClient.Close();
            cancellationTokenSource?.Cancel();
        }

        private async Task StartReceiver(CancellationToken token) {
            var stream = tcpClient.GetStream();

            await Task.Run(async () => {
                byte[] currentMessageBuffer = new byte[0];
                while (true) {
                    byte[] buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                    currentMessageBuffer = currentMessageBuffer.Concat(buffer).ToArray();
                    while (currentMessageBuffer.Contains((byte)'\n')) {
                        int index = Array.IndexOf(currentMessageBuffer, (byte)'\n');
                        string message = Encoding.UTF8.GetString(currentMessageBuffer.Take(index).ToArray());
                        currentMessageBuffer = currentMessageBuffer.Skip(index + 1).ToArray();
                        string[] parts = message.Split(' ', 2);
                        var kind = parts[0];
                        var content = parts[1];
                        if (handlers.ContainsKey(kind)) {
                            handlers[kind](content);
                        } else {
                            Console.WriteLine($"Unhandled message: {kind}");
                        }
                    }
                }
            });
        }

        public static async Task Connect(int port) {
            Client client = new Client(port);
            await client.tcpClient.ConnectAsync("127.0.0.1", port);

            client.cancellationTokenSource = new CancellationTokenSource();
            client.receiver = client.StartReceiver(client.cancellationTokenSource.Token);
        }

        public void RegisterListener(string kind, Action<string> handler) {
            handlers[kind] = handler;
        }
    }
}

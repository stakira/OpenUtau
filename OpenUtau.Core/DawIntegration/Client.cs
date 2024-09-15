using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenUtau.Core.SignalChain;
using Vortice.Direct3D;
using WanaKanaNet.Helpers;
using OpenUtau.Core.Ustx;
using NAudio.Wave;
using Newtonsoft.Json;
using System.IO.Compression;

namespace OpenUtau.Core.DawIntegration {
    public class Client {
        static int VERSION = 1;
        private readonly int port;
        private TcpClient tcpClient;
        private Stream? stream;
        private Task? receiver;
        private CancellationTokenSource? cancellationTokenSource;
        private Dictionary<string, Action<string>> handlers = new Dictionary<string, Action<string>>();
        private Dictionary<string, Action<string>> onetimeHandlers = new Dictionary<string, Action<string>>();

        private Client(int port) {
            this.port = port;
            tcpClient = new TcpClient();
        }

        ~Client() {
            tcpClient.Close();
            cancellationTokenSource?.Cancel();
        }

        private async Task StartReceiver(CancellationToken token) {
            if (stream == null) {
                throw new Exception("stream is null");
            }
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
                        } else if (onetimeHandlers.ContainsKey(kind)) {
                            onetimeHandlers[kind](content);
                            onetimeHandlers.Remove(kind);
                        } else {
                            Console.WriteLine($"Unhandled message: {kind}");
                        }
                    }
                }
            });
        }

        public static async Task<Client> Connect(int port) {
            var client = new Client(port);
            await client.tcpClient.ConnectAsync("127.0.0.1", port);
            client.stream = client.tcpClient.GetStream();

            client.cancellationTokenSource = new CancellationTokenSource();
            client.receiver = client.StartReceiver(client.cancellationTokenSource.Token);

            var tcs = new TaskCompletionSource<object?>();
            client.RegisterOnetimeListener("init", (string message) => {
                tcs.SetResult(null);
            });

            var timeoutCanceller = new CancellationTokenSource();
            timeoutCanceller.CancelAfter(TimeSpan.FromSeconds(5));
            timeoutCanceller.Token.Register(() => tcs.TrySetCanceled());
            await tcs.Task;
            return client;
        }

        public async Task SendStatus(UProject project, List<WaveMix> mixes) {
            var ustx = Format.Ustx.CreateUstx(project);
            var base64Mixes = mixes.Select(mixSource => {
                if (mixSource == null) {
                    return "";
                }
                var mix = new ExportAdapter(mixSource).ToWaveProvider();
                using (var ms = new MemoryStream())
                using (var compressor = new GZipStream(ms, CompressionMode.Compress)) {
                    var buffer = new byte[mix.WaveFormat.AverageBytesPerSecond];
                    int bytesRead;
                    while ((bytesRead = mix.Read(buffer, 0, buffer.Length)) > 0) {
                        compressor.Write(buffer, 0, bytesRead);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            });

            var message = new UpdateStatusMessage(
                ustx,
                base64Mixes.ToList()
            );

            await SendMessage("status", message);
        }

        private async Task SendMessage(string kind, object json) {
            if (stream == null) {
                throw new Exception("stream is null");
            }
            await stream.WriteAsync(Encoding.UTF8.GetBytes($"{kind} {JsonConvert.SerializeObject(json)}\n"));
        }

        public void RegisterListener(string kind, Action<string> handler) {
            handlers[kind] = handler;
        }
        public void RegisterOnetimeListener(string kind, Action<string> handler) {
            onetimeHandlers[kind] = handler;
        }
    }
}

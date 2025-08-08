using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using Newtonsoft.Json;
using NumSharp.Utilities;
using Serilog;

namespace OpenUtau.Core.DawIntegration {
    public class DawClient {
        static int VERSION = 1;
        public readonly DawServer server;
        private readonly TcpClient tcpClient;
        private Stream? stream;
        private Task? receiver;
        private CancellationTokenSource? cancellationTokenSource;
        private Dictionary<string, Action<string>> handlers = new Dictionary<string, Action<string>>();
        private Dictionary<string, Action<string>> onetimeHandlers = new Dictionary<string, Action<string>>();
        readonly SemaphoreSlim writerSemaphore = new SemaphoreSlim(1, 1);

        private DawClient(DawServer server) {
            this.server = server;
            tcpClient = new TcpClient();
        }

        ~DawClient() {
            tcpClient.Close();
            cancellationTokenSource?.Cancel();
        }

        private async Task StartReceiver(CancellationToken token) {
            if (stream == null) {
                throw new Exception("stream is null");
            }
            await Task.Run(async () => {
                using (var currentMessageBuffer = new MemoryStream()) {
                    while (true) {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        try {
                            bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                        } catch (SocketException) {
                            break;
                        }
                        currentMessageBuffer.Write(buffer, 0, bytesRead);
                        while (true) {
                            // Search for newline in the buffer
                            var buf = currentMessageBuffer.GetBuffer();
                            int length = (int)currentMessageBuffer.Length;
                            int index = Array.IndexOf(buf, (byte)'\n', 0, length);
                            if (index < 0) {
                                break;
                            }
                            // Extract message up to newline
                            string message = Encoding.UTF8.GetString(buf, 0, index);
                            // Remove processed message from buffer
                            int remaining = length - (index + 1);
                            // Shift remaining bytes to start
                            Array.Copy(buf, index + 1, buf, 0, remaining);
                            currentMessageBuffer.SetLength(remaining);
                            string[] parts = message.Split(' ', 2);
                            var kind = parts[0];
                            var content = parts.Length > 1 ? parts[1] : "";
                            if (handlers.ContainsKey(kind)) {
                                handlers[kind](content);
                            } else if (onetimeHandlers.ContainsKey(kind)) {
                                onetimeHandlers[kind](content);
                                onetimeHandlers.Remove(kind);
                            } else {
                                Log.Warning($"Unhandled message: {kind}");
                            }
                        }

                        if (cancellationTokenSource != null && cancellationTokenSource.IsCancellationRequested) {
                            break;
                        }
                    }
                }

                OnDisconnect();
            });
        }

        public static async Task<(DawClient, string)> Connect(DawServer server) {
            var client = new DawClient(server);
            await client.tcpClient.ConnectAsync("127.0.0.1", server.Port);
            client.stream = client.tcpClient.GetStream();

            client.cancellationTokenSource = new CancellationTokenSource();
            client.receiver = client.StartReceiver(client.cancellationTokenSource.Token);

            var timeoutCanceller = new CancellationTokenSource();
            timeoutCanceller.CancelAfter(TimeSpan.FromSeconds(5));
            var initMessage = await client.SendRequest<InitResponse>(new InitRequest(), timeoutCanceller.Token);

            client.RegisterNotification<DawOuNotification>("ping", (_) => { });
            return (client, initMessage.ustx);
        }

        private async Task SendMessage(string header, DawMessage data) {
            if (stream == null) {
                throw new Exception("stream is null");
            }
            await writerSemaphore.WaitAsync();
            var disconnected = false;
            try {
                await stream.WriteAsync(Encoding.UTF8.GetBytes($"{header} {JsonConvert.SerializeObject(data)}\n"));
            } catch (SocketException) {
                disconnected = true;
            } finally {
                writerSemaphore.Release();
            }

            if (disconnected) {
                Disconnect();
            }
        }
        public async Task<T> SendRequest<T>(DawDawRequest data,
            CancellationToken? token = null) where T : DawDawResponse {
            if (stream == null) {
                throw new Exception("stream is null");
            }
            var uuid = Guid.NewGuid().ToString();

            var tcs = new TaskCompletionSource<DawResult<T>>();
            token?.Register(() => tcs.TrySetCanceled());
            RegisterOnetimeListener($"response:{uuid}", (string message) => {
                tcs.SetResult(
                    JsonConvert.DeserializeObject<DawResult<T>>(message)!
                );
            });
            await SendMessage($"request:{uuid}:{data.kind}", data);

            var result = await tcs.Task;
            if (!result.success) {
                throw new Exception($"DAW returned error to request {data.kind}: {result.error!}");
            }
            if (result.data == null) {
                throw new Exception("Unreachable: result.success && result.data == null");
            }

            return result.data;
        }
        public async Task SendNotification(DawDawNotification data,
            CancellationToken? token = null) {
            if (stream == null) {
                throw new Exception("stream is null");
            }
            await SendMessage($"notification:{data.kind}", data);
        }

        public void RegisterNotification<T>(string kind, Action<T> handler) where T : DawOuNotification {
            handlers[kind] = (string message) => {
                handler(JsonConvert.DeserializeObject<T>(message)!);
            };
        }

        private void RegisterListener(string kind, Action<string> handler) {
            handlers[kind] = handler;
        }
        private void RegisterOnetimeListener(string kind, Action<string> handler) {
            onetimeHandlers[kind] = handler;
        }

        public void Disconnect() {
            tcpClient.Close();
            cancellationTokenSource?.Cancel();
            OnDisconnect();
        }

        private void OnDisconnect() {
            foreach ((var key, var handler) in onetimeHandlers) {
                if (key.StartsWith("response")) {
                    handler(
                        JsonConvert.SerializeObject(new DawResult<DawDawResponse>(
                          false, null, "Disconnected"
                        ))
                    );
                }
            }

            DocManager.Inst.ExecuteCmd(new DawDisconnectedNotification());
        }
    }
}

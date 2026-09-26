using System;
using NetMQ;
using NetMQ.Sockets;
using Serilog;

namespace OpenUtau.Core.Enunu {
    class EnunuClient : Util.SingletonBase<EnunuClient> {
        internal T SendRequest<T>(string[] args) {
            return SendRequest<T>(args, "15555");
        }
        internal T SendRequest<T>(object[] args, string port, int second = 300) {
            string? message = Send(args, port, second);
            if (string.IsNullOrEmpty(message)) {
                return (T)Activator.CreateInstance(typeof(T))!;
            }
            return Json.Deserialize<T>(message)!;
        }

        /// <summary>
        /// Sends a request and returns the raw response, or null when the server does not respond in time.
        /// </summary>
        internal string? Send(object[] args, string port, int second) {
            using (var client = new RequestSocket()) {
                client.Connect($"tcp://localhost:{port}");
                string request = Json.Serialize(args);
                Log.Information($"EnunuProcess sending {Abbreviate(request)}");
                client.SendFrame(request);
                client.TryReceiveFrameString(TimeSpan.FromSeconds(second), out string? message);
                Log.Information($"EnunuProcess received {message}");
                return message;
            }
        }

        // acoustic_f0 carries the whole f0 array; keep the log readable.
        static string Abbreviate(string request) {
            return request.Length <= 1000 ? request : request.Substring(0, 1000) + $"... ({request.Length} chars)";
        }
    }
}

using System;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Serilog;

namespace OpenUtau.Core.Enunu {
    class EnunuClient : Util.SingletonBase<EnunuClient> {
        internal T SendRequest<T>(string[] args) {
            using (var client = new RequestSocket()) {
                client.Connect("tcp://localhost:15555");
                string request = JsonConvert.SerializeObject(args);
                Log.Information($"EnunuProcess sending {request}");
                client.SendFrame(request);
                client.TryReceiveFrameString(TimeSpan.FromSeconds(300), out string? message);
                Log.Information($"EnunuProcess received {message}");
                return JsonConvert.DeserializeObject<T>(message ?? string.Empty)!;
            }
        }
    }
}

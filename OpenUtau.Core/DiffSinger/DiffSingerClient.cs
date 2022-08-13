using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Serilog;

namespace OpenUtau.Core.DiffSinger {
    class DiffSingerClient {
        private static volatile DiffSingerClient instance;
        private static readonly object lockObj = new object();

        private DiffSingerClient() { }

        public static DiffSingerClient Inst {
            get {
                if (instance == null) {
                    lock (lockObj) {
                        if (instance == null) {
                            instance = new DiffSingerClient();
                        }
                    }
                }
                return instance;
            }
        }

        internal T SendRequest<T>(string[] args) {
            using (var client = new RequestSocket()) {
                client.Connect("tcp://localhost:38442");
                string request = JsonConvert.SerializeObject(args);
                Log.Information($"DiffSingerProcess sending {request}");
                client.SendFrame(request);
                var message = client.ReceiveFrameString();
                Log.Information($"DiffSingerProcess received {message}");
                return JsonConvert.DeserializeObject<T>(message);
            }
        }
    }
}

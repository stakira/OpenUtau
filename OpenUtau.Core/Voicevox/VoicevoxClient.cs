using System;
using System.Collections.Generic;
using System.Text;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Serilog;

namespace OpenUtau.Core.Voicevox {
    class VoicevoxClient: Util.SingletonBase<VoicevoxClient> {
        internal T SendRequest<T>(VoicevoxURL voicevoxURL) {
            using (var client = new RequestSocket()) {
                client.Connect("tcp://127.0.0.1:50021");
                string request = this.RequestURL(voicevoxURL);
                Log.Information($"VoicevoxProcess sending {request}");
                client.SendFrame(request);
                client.TryReceiveFrameString(TimeSpan.FromSeconds(300), out string? message);
                Log.Information($"VoicevoxProcess received {message}");
                return JsonConvert.DeserializeObject<T>(message ?? string.Empty)!;
            }
        }

        public string RequestURL(VoicevoxURL voicevoxURL) {
            StringBuilder queryStringBuilder = new StringBuilder();
            foreach (var parameter in voicevoxURL.query) {
                queryStringBuilder.Append($"{parameter.Key}={parameter.Value}&");
            }

            // 末尾の余分な "&" を削除
            string queryString = "?"+queryStringBuilder.ToString().TrimEnd('&');

            string str = $"{voicevoxURL.method.ToUpper()} {voicevoxURL.path}{queryString} {voicevoxURL.protocol.ToUpper()}\r\n";
            str += $"HOST: {voicevoxURL.host}\r\n\r\n";
            return str ;
        }
    }
    public class VoicevoxURL {
        public string method = string.Empty;
        public string protocol = "HTTP/1.1";
        public string host = "127.0.0.1:50021";
        public string path = string.Empty;
        public Dictionary<string, string> query = new Dictionary<string, string>();
    }
}

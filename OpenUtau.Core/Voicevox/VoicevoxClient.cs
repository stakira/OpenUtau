using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;
using Serilog;

namespace OpenUtau.Core.Voicevox {
    class VoicevoxClient: Util.SingletonBase<VoicevoxClient> {
        public JObject jObj;
        public byte[] bytes;

        internal async void SendRequest(VoicevoxURL voicevoxURL) {
            try {
                using (var client = new HttpClient()) {
                    using (var request = new HttpRequestMessage(new HttpMethod(voicevoxURL.method.ToUpper()), this.RequestURL(voicevoxURL))) {
                        request.Headers.TryAddWithoutValidation("accept", voicevoxURL.accept);

                        request.Content = new StringContent(voicevoxURL.body);
                        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                        Log.Information($"VoicevoxProcess sending {request}");
                        var response = client.SendAsync(request);
                        var message = response.Result.Content.ReadAsStringAsync().Result;
                        Log.Information($"VoicevoxProcess received");
                        string contentType = response.Result.Content.Headers.ContentType.MediaType;
                        if (contentType.Equals("application/json")) {
                            jObj = JObject.Parse(message);
                        } else if (contentType.Equals("audio/wav")) {
                            bytes = response.Result.Content.ReadAsByteArrayAsync().Result;
                        } else {
                            jObj = JObject.Parse("{" + message + "}");
                        }
                    }
                }
            } catch (Exception ex) {
                Log.Error(@"{ex}");
            }
        }

        public string RequestURL(VoicevoxURL voicevoxURL) {
            StringBuilder queryStringBuilder = new StringBuilder();
            foreach (var parameter in voicevoxURL.query) {
                queryStringBuilder.Append($"{parameter.Key}={parameter.Value}&");
            }

            // 末尾の余分な "&" を削除
            string queryString = "?"+queryStringBuilder.ToString().TrimEnd('&');

            string str = $"{voicevoxURL.protocol}{voicevoxURL.host}{voicevoxURL.path}{queryString}";
            return str ;
        }
    }
    public class VoicevoxURL {
        public string method = string.Empty;
        public string protocol = "http://";
        public string host = "127.0.0.1:50021";
        public string path = string.Empty;
        public Dictionary<string, string> query = new Dictionary<string, string>();
        public string body = string.Empty;
        public string accept = "application/json";
    }
}

using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenUtau.Core {

    internal static class UpdateChecker {
        private const string VersionUrl = "https://ci.appveyor.com/api/projects/stakira/openutau";

        public static bool Check() {
            try {
                using (var client = new WebClient()) {
                    var result = client.DownloadString(VersionUrl);
                    var dict = JsonConvert.DeserializeObject<JObject>(result);
                    var lastest = new Version(dict["build"]["version"].ToString());
                    var info = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
                    var current = new Version(info.ProductVersion);
                    Console.WriteLine("current version {0}, latest version {1}", current, lastest);
                    return current.CompareTo(lastest) < 0;
                }
            } catch {
                return false;
            }
        }

        public static string GetVersion() {
            return FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
        }
    }
}

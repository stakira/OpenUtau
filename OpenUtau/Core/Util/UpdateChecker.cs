using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Text.RegularExpressions;

namespace OpenUtau.Core
{
    static class UpdateChecker
    {
        const string VersionUrl = "https://raw.githubusercontent.com/stakira/OpenUtau/master/README.md";
        public static bool Check()
        {
            string result;
            
            try
            {
                using (WebClient client = new WebClient())
                {
                    result = client.DownloadString(VersionUrl);
                }
            }
            catch
            {
                return false;
            }

            Regex regex = new Regex(@"Current stage: (\w+)");
            Match match = regex.Match(result);
            if (match.Success)
            {
                string thisVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                string latestVer = match.Groups[1].ToString();
                System.Diagnostics.Debug.WriteLine("Latest version: {0}, this version: {1}", latestVer, thisVer);
                return latestVer != thisVer;
            }
            else return false;
        }
    }
}

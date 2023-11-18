using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace OpenUtau.Core.Util {
    public static class WebFileDownloader {
        /// <summary>
        /// downloads file from [address] in web, and saves as [filename] in "Cache" folder.
        /// returns true if file download completed successfully, otherwise false.
        /// it's async process, so must call .Wait() when use.
        /// </summary>
        public static async Task DownLoadFileAsyncInCache(string address, string filename)
        {
            WebClient client = new WebClient();
            Uri uri = new Uri(address);
            bool isSuccessed = false;
            bool isCompleted = false;

            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback4);

            await client.DownloadFileTaskAsync(uri, Path.Combine(PathManager.Inst.CachePath, filename)).ConfigureAwait(false);

        }
        

        private static void DownloadProgressCallback4(object sender, DownloadProgressChangedEventArgs e)
        {
            // Displays the operation identifier, and the transfer progress.
            DocManager.Inst.ExecuteCmd(new ProgressBarNotification(e.ProgressPercentage, $"Please Wait... OU is Downloading Dependency from web: {Math.Round(e.BytesReceived / Math.Pow(1024, 2), 2)}MB / {Math.Round(e.TotalBytesToReceive / Math.Pow(1024, 2), 2)}MB."));
            
        }
    }

}
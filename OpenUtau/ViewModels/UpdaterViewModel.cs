using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Threading.Tasks;
using Avalonia.Media;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.Interfaces;
using NetSparkleUpdater.SignatureVerifiers;
using OpenUtau.Core;
using OpenUtau.Core.Util;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace OpenUtau.App.ViewModels {
    public class UpdaterViewModel : ViewModelBase {
        public string AppVersion => $"v{System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version}";
        public bool IsDarkMode => ThemeManager.IsDarkMode;
        [Reactive] public string UpdaterStatus { get; set; }
        [Reactive] public bool UpdateAvailable { get; set; }
        [Reactive] public FontWeight UpdateButtonFontWeight { get; set; }
        public Action? CloseApplication { get; set; }

        private SparkleUpdater? sparkle;
        private UpdateInfo? updateInfo;
        private bool updateAccepted;

        public UpdaterViewModel() {
            UpdaterStatus = string.Empty;
            UpdateAvailable = false;
            UpdateButtonFontWeight = FontWeight.Normal;
            Init();
        }

        private static readonly string[] domains = new[] {
            "https://github.com",
            "https://hub.fastgit.xyz",
        };

        public static async Task<SparkleUpdater> NewUpdaterAsync() {
            string rid = OS.GetUpdaterRid();
            string domain = await ChooseDomainAsync();
            string url = $"{domain}/stakira/OpenUtau/releases/latest/download/appcast.{rid}.xml";
            Log.Information($"Checking update at: {url}");
            return new ZipUpdater(url, new Ed25519Checker(SecurityMode.Unsafe)) {
                UIFactory = null,
                CheckServerFileName = false,
                RelaunchAfterUpdate = true,
                RelaunchAfterUpdateCommandPrefix = OS.IsLinux() ? "./" : string.Empty,
            };
        }

        static async Task<string> ChooseDomainAsync() {
            TimeSpan bestTime = TimeSpan.FromDays(1);
            string bestDomain = domains[0];
            var stopWatch = new Stopwatch();
            foreach (var domain in domains) {
                var request = WebRequest.Create(domain);
                request.Method = "HEAD";
                request.Timeout = 5000;
                request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
                stopWatch.Start();
                try {
                    var response = await request.GetResponseAsync();
                    stopWatch.Stop();
                    bool ok = ((HttpWebResponse)response).StatusCode == HttpStatusCode.OK;
                    if (ok && bestTime > stopWatch.Elapsed) {
                        bestDomain = domain;
                        bestTime = stopWatch.Elapsed;
                        if (bestTime.TotalMilliseconds < 500) {
                            break;
                        }
                    }
                    Log.Information($"Domain {domain} {stopWatch.Elapsed}");
                } catch (Exception e) {
                    Log.Error(e, $"Failed to connect domain {domain}");
                }
            }
            return bestDomain;
        }

        async void Init() {
            UpdaterStatus = ThemeManager.GetString("updater.status.checking");
            sparkle = await NewUpdaterAsync();
            updateInfo = await sparkle.CheckForUpdatesQuietly();
            if (updateInfo == null) {
                return;
            }
            switch (updateInfo.Status) {
                case UpdateStatus.UpdateAvailable:
                case UpdateStatus.UserSkipped:
                    UpdaterStatus = string.Format(ThemeManager.GetString("updater.status.available"), updateInfo.Updates[0].Version);
                    UpdateAvailable = true;
                    UpdateButtonFontWeight = FontWeight.Bold;
                    break;
                case UpdateStatus.UpdateNotAvailable:
                    UpdaterStatus = ThemeManager.GetString("updater.status.notavailable");
                    break;
                case UpdateStatus.CouldNotDetermine:
                    UpdaterStatus = ThemeManager.GetString("updater.status.unknown");
                    break;
            }
        }

        public void OnGithub() {
            try {
                OS.OpenWeb("https://github.com/stakira/OpenUtau/wiki");
            } catch (Exception e) {
                DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(e));
            }
        }

        public async void OnUpdate() {
            if (sparkle == null || updateInfo == null || updateInfo.Updates.Count == 0) {
                return;
            }
            UpdateAvailable = false;
            updateAccepted = true;

            sparkle.DownloadStarted += (item, path) => {
                Log.Information($"download started {path}");
            };
            sparkle.DownloadFinished += (item, path) => {
                Log.Information($"download finished {path}");
                sparkle.CloseApplication += () => {
                    Log.Information($"shutting down for update");
                    CloseApplication?.Invoke();
                    Log.Information($"shut down for update");
                };
                sparkle.InstallUpdate(item, path);
            };
            sparkle.DownloadHadError += (item, path, e) => {
                Log.Error(e, $"download error {path}");
            };
            sparkle.DownloadMadeProgress += (sender, item, e) => {
                UpdaterStatus = $"{e.ProgressPercentage}%";
            };

            await sparkle.InitAndBeginDownload(updateInfo.Updates.First());
        }

        public void OnClosing() {
            if (!updateAccepted && updateInfo != null &&
                (updateInfo.Status == UpdateStatus.UpdateAvailable ||
                updateInfo.Status == UpdateStatus.UserSkipped) &&
                updateInfo.Updates.Count > 0) {
                Log.Information($"Skipping update {updateInfo.Updates[0].Version}");
                Preferences.Default.SkipUpdate = updateInfo.Updates[0].Version.ToString();
                Preferences.Save();
            }
        }
    }

    public class ZipUpdater : SparkleUpdater {
        public ZipUpdater(string appcastUrl, ISignatureVerifier signatureVerifier) :
            base(appcastUrl, signatureVerifier) { }
        public ZipUpdater(string appcastUrl, ISignatureVerifier signatureVerifier, string referenceAssembly) :
            base(appcastUrl, signatureVerifier, referenceAssembly) { }
        public ZipUpdater(string appcastUrl, ISignatureVerifier signatureVerifier, string referenceAssembly, IUIFactory factory) :
            base(appcastUrl, signatureVerifier, referenceAssembly, factory) { }

        protected override string GetWindowsInstallerCommand(string downloadFilePath) {
            string installerExt = Path.GetExtension(downloadFilePath);
            if (DoExtensionsMatch(installerExt, ".exe")) {
                return $"\"{downloadFilePath}\"";
            }
            if (DoExtensionsMatch(installerExt, ".msi")) {
                return $"msiexec /i \"{downloadFilePath}\"";
            }
            if (DoExtensionsMatch(installerExt, ".msp")) {
                return $"msiexec /p \"{downloadFilePath}\"";
            }
            if (DoExtensionsMatch(installerExt, ".zip")) {
                string restart = RestartExecutablePath.TrimEnd('\\', '/');
                if (Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 17063) {
                    Log.Information("Starting update with tar.");
                    return $"tar -x -f \"{downloadFilePath}\" -C \"{restart}\"";
                }
                var unzipperPath = Path.Combine(Path.GetDirectoryName(downloadFilePath) ?? Path.GetTempPath(), "Unzipper.exe");
                File.WriteAllBytes(unzipperPath, Resources.Resources.Unzipper);
                Log.Information("Starting update with unzipper.");
                return $"{unzipperPath} \"{downloadFilePath}\" \"{restart}\"";
            }
            return downloadFilePath;
        }
    }
}

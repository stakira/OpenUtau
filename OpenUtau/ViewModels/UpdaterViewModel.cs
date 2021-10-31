using System.IO;
using System.Linq;
using NetSparkleUpdater;
using NetSparkleUpdater.Interfaces;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.SignatureVerifiers;
using ReactiveUI.Fody.Helpers;
using Serilog;
using OpenUtau.Core.Util;
using Avalonia.Media;
using System;

namespace OpenUtau.App.ViewModels {
    public class UpdaterViewModel : ViewModelBase {
        public string AppVersion => $"v{System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version}";
        public bool IsDarkMode => ThemeManager.IsDarkMode;
        [Reactive] public string UpdaterStatus { get; set; }
        [Reactive] public bool UpdateAvailable { get; set; }
        [Reactive] public FontWeight UpdateButtonFontWeight { get; set; }
        public Action? CloseApplication { get; set; }

        private SparkleUpdater sparkle;
        private UpdateInfo? updateInfo;
        private bool updateAccepted;

        public UpdaterViewModel() {
            UpdaterStatus = string.Empty;
            UpdateAvailable = false;
            UpdateButtonFontWeight = FontWeight.Normal;
            sparkle = NewUpdater();
            Init();
        }

        public static SparkleUpdater NewUpdater() {
            string os = OS.IsWindows() ? "win" : OS.IsMacOS() ? "macos" : "linux";
            string url = $"https://github.com/stakira/OpenUtau/releases/download/OpenUtau-Latest/appcast.{os}.xml";
            return new ZipUpdater(url, new Ed25519Checker(SecurityMode.Unsafe)) {
                UIFactory = null,
                CheckServerFileName = false,
                RelaunchAfterUpdate = true,
            };
        }

        async void Init() {
            UpdaterStatus = ThemeManager.GetString("updater.status.checking");
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
            OS.OpenWeb("https://github.com/stakira/OpenUtau/wiki");
        }

        public async void OnUpdate() {
            if (updateInfo == null || updateInfo.Updates.Count == 0) {
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
            if (DoExtensionsMatch(installerExt, ".zip")) {
                var unzipperPath = Path.Combine(Path.GetDirectoryName(downloadFilePath) ?? Path.GetTempPath(), "Unzipper.exe");
                File.WriteAllBytes(unzipperPath, Resources.Resources.Unzipper);
                return string.Format($"{unzipperPath} \"{downloadFilePath}\" \"{RestartExecutablePath.TrimEnd('\\', '/')}\"");
                //return string.Format("tar -x -f {0} -C \"{1}\"", downloadFilePath, RestartExecutablePath.TrimEnd('\\'));
            }
            return downloadFilePath;
        }
    }
}

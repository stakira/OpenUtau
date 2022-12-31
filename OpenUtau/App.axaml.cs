using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using OpenUtau.App.Views;
using OpenUtau.Classic;
using OpenUtau.Core;
using Serilog;

namespace OpenUtau.App {
    public class App : Application {
        public override void Initialize() {
            Log.Information("Initializing application.");
            AvaloniaXamlLoader.Load(this);
            InitializeCulture();
            InitializeTheme();
            InitOpenUtau();
            InitAudio();
            Log.Information("Initialized application.");
        }

        public override void OnFrameworkInitializationCompleted() {
            Log.Information("Framework initialization completed.");
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        public void InitializeCulture() {
            Log.Information("Initializing culture.");
            var language = CultureInfo.InstalledUICulture.Name;
            if (!string.IsNullOrEmpty(Core.Util.Preferences.Default.Language)) {
                language = Core.Util.Preferences.Default.Language;
            }
            SetLanguage(language);

            // Force using InvariantCulture to prevent issues caused by culture dependent string conversion, especially for floating point numbers.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            Log.Information("Initialized culture.");
        }

        public static void SetLanguage(string language) {
            var dictionaryList = Current.Resources.MergedDictionaries
                .Select(res => (ResourceInclude)res)
                .ToList();
            var resDictName = string.Format(@"Strings.{0}.axaml", language);
            var resDict = dictionaryList
                .FirstOrDefault(d => d.Source!.OriginalString.Contains(resDictName));
            if (resDict == null) {
                resDict = dictionaryList.FirstOrDefault(d => d.Source!.OriginalString.Contains("Strings.axaml"));
            }
            if (resDict != null) {
                Current.Resources.MergedDictionaries.Remove(resDict);
                Current.Resources.MergedDictionaries.Add(resDict);
            }
        }

        static void InitializeTheme() {
            Log.Information("Initializing theme.");
            SetTheme();
            Log.Information("Initialized theme.");
        }

        public static void SetTheme() {
            var light = Current.Resources.MergedDictionaries
                .Select(res => (ResourceInclude)res)
                .FirstOrDefault(d => d.Source!.OriginalString.Contains("LightTheme"));
            var dark = Current.Resources.MergedDictionaries
                .Select(res => (ResourceInclude)res)
                .FirstOrDefault(d => d.Source!.OriginalString.Contains("DarkTheme"));
            if (Core.Util.Preferences.Default.Theme == 0) {
                Current.Resources.MergedDictionaries.Remove(light);
                Current.Resources.MergedDictionaries.Add(light);
            } else {
                Current.Resources.MergedDictionaries.Remove(dark);
                Current.Resources.MergedDictionaries.Add(dark);
            }
            ThemeManager.LoadTheme();
        }

        public static void InitOpenUtau() {
            Log.Information("Initializing OpenUtau.");
            ToolsManager.Inst.Initialize();
            SingerManager.Inst.Initialize();
            DocManager.Inst.Initialize();
            DocManager.Inst.PostOnUIThread = action => Avalonia.Threading.Dispatcher.UIThread.Post(action);
            Log.Information("Initialized OpenUtau.");
        }

        public static void InitAudio() {
            Log.Information("Initializing audio.");
            if (!OS.IsWindows() || Core.Util.Preferences.Default.PreferPortAudio) {
                try {
                    PlaybackManager.Inst.AudioOutput = new Audio.PortAudioOutput();
                } catch (Exception e1) {
                    Log.Error(e1, "Failed to init PortAudio");
                }
            } else {
                try {
                    PlaybackManager.Inst.AudioOutput = new Audio.NAudioOutput();
                } catch (Exception e2) {
                    Log.Error(e2, "Failed to init NAudio");
                }
            }
            Log.Information("Initialized audio.");
        }
    }
}

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using Serilog;

namespace OpenUtau {

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        private App() {
            InitializeComponent();
            InitializeCulture();
            InitializeTheme();
        }

        public static void InitializeCulture() {
            var culture = CultureInfo.InstalledUICulture.Name;
            var dictionaryList = Current.Resources.MergedDictionaries.ToList();
            var resDictName = string.Format(@"UI\Strings.{0}.xaml", culture);
            var resDict = dictionaryList.
                FirstOrDefault(d => d.Source.OriginalString == resDictName);
            if (resDict != null) {
                Current.Resources.MergedDictionaries.Remove(resDict);
                Current.Resources.MergedDictionaries.Add(resDict);
            }
            // Force using InvariantCulture to prevent issues caused by culture dependent string conversion, especially for floating point numbers.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        }

        public static void InitializeTheme() {
            var light = Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source.OriginalString == @"UI\Colors\LightTheme.xaml");
            var dark = Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source.OriginalString == @"UI\Colors\DarkTheme.xaml");
            Current.Resources.MergedDictionaries.Remove(light);
            Current.Resources.MergedDictionaries.Remove(dark);
            if (Core.Util.Preferences.Default.theme == 0) {
                Current.Resources.MergedDictionaries.Add(light);
            } else {
                Current.Resources.MergedDictionaries.Add(dark);
            }
        }

        [STAThread]
        private static void Main() {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day, encoding: System.Text.Encoding.UTF8)
                .CreateLogger();
            NBug.Settings.ReleaseMode = true;
            NBug.Settings.StoragePath = NBug.Enums.StoragePath.CurrentDirectory;
            NBug.Settings.UIMode = NBug.Enums.UIMode.Full;

            Core.DocManager.Inst.Initialize();

            var app = new App();
            if (!Debugger.IsAttached) {
                AppDomain.CurrentDomain.UnhandledException += NBug.Handler.UnhandledException;
                app.DispatcherUnhandledException += NBug.Handler.DispatcherUnhandledException;
            }
            var window = new UI.MainWindow();
            app.Run(window);
        }
    }
}

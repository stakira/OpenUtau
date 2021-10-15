using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Globalization;
using System.Threading;
using OpenUtau.App.Views;
using Avalonia.Markup.Xaml.MarkupExtensions;
using System.Linq;

namespace OpenUtau.App {
    public class App : Application {
        public override void Initialize() {
            AvaloniaXamlLoader.Load(this);
            InitializeCulture();
            InitializeTheme();
        }

        public override void OnFrameworkInitializationCompleted() {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        public void InitializeCulture() {
            var language = CultureInfo.InstalledUICulture.Name;
            if (!string.IsNullOrEmpty(Core.Util.Preferences.Default.Language)) {
                language = Core.Util.Preferences.Default.Language;
            }
            SetLanguage(language);

            // Force using InvariantCulture to prevent issues caused by culture dependent string conversion, especially for floating point numbers.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
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
            SetTheme();
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
    }
}

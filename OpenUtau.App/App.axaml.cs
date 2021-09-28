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
            var culture = CultureInfo.InstalledUICulture.Name;
            var dictionaryList = Resources.MergedDictionaries
                .Select(res => (ResourceInclude)res)
                .ToList();
            var resDict = dictionaryList.
                FirstOrDefault(d => d.Source.OriginalString.Contains(culture.ToString()));
            if (resDict != null) {
                Resources.MergedDictionaries.Remove(resDict);
                Resources.MergedDictionaries.Add(resDict);
            }

            // Force using InvariantCulture to prevent issues caused by culture dependent string conversion, especially for floating point numbers.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        }

        public void InitializeTheme() {
            var light = Current.Resources.MergedDictionaries
                .Select(res => (ResourceInclude)res)
                .FirstOrDefault(d => d.Source.OriginalString.Contains("LightTheme"));
            var dark = Current.Resources.MergedDictionaries
                .Select(res => (ResourceInclude)res)
                .FirstOrDefault(d => d.Source.OriginalString.Contains("DarkTheme"));
            Current.Resources.MergedDictionaries.Remove(light);
            Current.Resources.MergedDictionaries.Remove(dark);
            if (Core.Util.Preferences.Default.Theme == 0) {
                Current.Resources.MergedDictionaries.Add(light);
            } else {
                Current.Resources.MergedDictionaries.Add(dark);
            }
        }
    }
}

using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;

namespace OpenUtau {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class WpfApp : Application {
        public WpfApp() {
            InitializeComponent();
            InitializeCulture();
            InitializeTheme();
        }

        public static void InitializeCulture() {
            var culture = CultureInfo.InstalledUICulture.Name;
            if (!string.IsNullOrEmpty(Core.Util.Preferences.Default.Language)) {
                culture = Core.Util.Preferences.Default.Language;
            }
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
            if (Core.Util.Preferences.Default.Theme == 0) {
                Current.Resources.MergedDictionaries.Add(light);
            } else {
                Current.Resources.MergedDictionaries.Add(dark);
            }
        }
    }
}

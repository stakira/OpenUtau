using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;

namespace OpenUtau {

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        private App() {
            InitializeComponent();
            SelectCulture(CultureInfo.InstalledUICulture.Name);
        }

        public static void SelectCulture(string culture) {
            if (string.IsNullOrEmpty(culture)) {
                return;
            }
            var dictionaryList = Current.Resources.MergedDictionaries.ToList();
            var resDictName = string.Format(@"UI\Strings.{0}.xaml", culture);
            var resDict = dictionaryList.
                FirstOrDefault(d => d.Source.OriginalString == resDictName);
            if (resDict != null) {
                Current.Resources.MergedDictionaries.Remove(resDict);
                Current.Resources.MergedDictionaries.Add(resDict);
            }
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
        }

        [STAThread]
        private static void Main() {
            NBug.Settings.ReleaseMode = true;
            NBug.Settings.StoragePath = NBug.Enums.StoragePath.CurrentDirectory;
            NBug.Settings.UIMode = NBug.Enums.UIMode.Full;
            AppDomain.CurrentDomain.UnhandledException += NBug.Handler.UnhandledException;
            Core.DocManager.Inst.SearchAllSingers();
            var pm = new Core.PartManager();
            var app = new App();
            app.DispatcherUnhandledException += NBug.Handler.DispatcherUnhandledException;
            var window = new UI.MainWindow();
            app.Run(window);
        }
    }
}

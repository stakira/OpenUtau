using System;
using System.Windows;

namespace OpenUtau
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        App()
        {
            InitializeComponent();
        }

        [STAThread]
        static void Main()
        {
            NBug.Settings.ReleaseMode = true;
            NBug.Settings.StoragePath = NBug.Enums.StoragePath.CurrentDirectory;
            NBug.Settings.UIMode = NBug.Enums.UIMode.Full;
            AppDomain.CurrentDomain.UnhandledException += NBug.Handler.UnhandledException;
            Core.DocManager.Inst.SearchAllSingers();
            var pm = new Core.PartManager();
            App app = new App();
            app.DispatcherUnhandledException += NBug.Handler.DispatcherUnhandledException;
            UI.MainWindow window = new UI.MainWindow();
            app.Run(window);
        }
    }
}

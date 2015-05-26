using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            // Merge settings after update
            if (OpenUtau.Properties.Settings.Default.UpgradeRequired)
            {
                OpenUtau.Properties.Settings.Default.Upgrade();
                OpenUtau.Properties.Settings.Default.UpgradeRequired = false;
                OpenUtau.Properties.Settings.Default.Save();
            }
            //Thread backgroundThread = new Thread(new ThreadStart(() => { }));
            //backgroundThread.Start();

            Core.DocManager.Inst.SearchAllSingers();
            var pm = new OpenUtau.Core.PartManager();
            App app = new App();
            UI.MainWindow window = new UI.MainWindow();
            app.Run(window);
        }
    }
}

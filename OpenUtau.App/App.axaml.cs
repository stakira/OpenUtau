using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Globalization;
using System.Text;
using System.Threading;
using Serilog;
using OpenUtau.App.ViewModels;
using OpenUtau.App.Views;
using OpenUtau.Core;

namespace OpenUtau.App {
    public class App : Application {
        public override void Initialize() {
            AvaloniaXamlLoader.Load(this);
            InitializeCulture();
            InitializeLogging();
            InitializeDocument();
        }

        public override void OnFrameworkInitializationCompleted() {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        public void InitializeCulture() {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            // Force using InvariantCulture to prevent issues caused by culture dependent string conversion, especially for floating point numbers.
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("es");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("es");
        }

        public void InitializeLogging() {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Debug()
                .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day, encoding: Encoding.UTF8)
                .CreateLogger();
        }

        public void InitializeDocument() {
            DocManager.Inst.Initialize();
        }
    }
}

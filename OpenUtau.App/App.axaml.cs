using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Globalization;
using System.Threading;
using OpenUtau.App.Views;

namespace OpenUtau.App {
    public class App : Application {
        public override void Initialize() {
            AvaloniaXamlLoader.Load(this);
            InitializeCulture();
        }

        public override void OnFrameworkInitializationCompleted() {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        public void InitializeCulture() {
            // Force using InvariantCulture to prevent issues caused by culture dependent string conversion, especially for floating point numbers.
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("es");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("es");
        }
    }
}

using Avalonia;
using Avalonia.iOS;
using Avalonia.ReactiveUI;

using Foundation;
using OpenUtau.App;

namespace OpenUtau.iOS;

// The UIApplicationDelegate for the application. This class is responsible for launching the 
// User Interface of the application, as well as listening (and optionally responding) to 
// application events from iOS.
[Register("AppDelegate")]
public partial class AppDelegate : AvaloniaAppDelegate<OpenUtau.App.App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();
    }
}

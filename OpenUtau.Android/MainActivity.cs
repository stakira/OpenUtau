using Avalonia;
using Avalonia.Android;
using Avalonia.ReactiveUI;
using Android.Content.PM;
using Android.App;
using OpenUtau.App;

namespace OpenUtau.Android;

[Activity(
    Label = "OpenUtau.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<OpenUtau.App.App> {
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) {
        return base.CustomizeAppBuilder(builder)
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();
    }
}

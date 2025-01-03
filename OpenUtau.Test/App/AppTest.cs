using Xunit;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Styling;
using OpenUtau.App;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

public class TestAppBuilder {
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

namespace OpenUtau.App {
    public class AppTest {
        [Fact]
        public void BuildTest() {
            Assert.False(typeof(App).IsAbstract);
            Assert.False(typeof(Program).IsAbstract);
        }

        [Fact]
        public void StringsTest() {
            var appBuilder = TestAppBuilder.BuildAvaloniaApp()
                .SetupWithoutStarting();
            var app = appBuilder.Instance as App;
            Assert.NotNull(app);

            var languages = App.GetLanguages();
            Assert.True(languages.Count > 1);
            Assert.Contains("en-US", languages.Keys);
            Assert.Contains("zh-CN", languages.Keys);
            Assert.Contains("ja-JP", languages.Keys);
            foreach (var pair in languages) {
                Assert.NotNull(pair.Value);
            }
        }
    }
}

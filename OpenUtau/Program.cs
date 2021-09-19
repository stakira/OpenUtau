using System;
using System.Text;
using Serilog;

namespace OpenUtau {
    class Program {

        [STAThread]
        private static void Main() {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day, encoding: System.Text.Encoding.UTF8)
                .CreateLogger();
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((sender, args) => {
                Log.Error((Exception)args.ExceptionObject, "Unhandled exception");
            });

            Core.DocManager.Inst.Initialize();
            Core.PlaybackManager.Inst.AudioOutput = new Audio.WaveOutAudioOutput();
            Core.AudioFileUtilsProvider.Utils = new Audio.NAudioFileUtils();

            App.Program.InitInterop();
            var app = new WpfApp();
            var window = new UI.MainWindow();
            app.Run(window);
        }
    }
}

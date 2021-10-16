using System;
using System.Text;
using AutoUpdaterDotNET;

namespace OpenUtau {
    class Program {

        [STAThread]
        private static void Main(string[] args) {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            App.Program.InitLogging();
            App.Program.InitOpenUtau();
            InitAudio();

            if (Core.Util.Preferences.Default.Beta == 0) {
                App.Program.InitInterop();
                new WpfApp().Run(new UI.MainWindow());
            } else {
                App.Program.AutoUpdate = () => AutoUpdater.Start("https://github.com/stakira/OpenUtau/releases/download/OpenUtau-Latest/release.xml");
                App.Program.Run(args);
            }
        }

        private static void InitAudio() {
            if (OS.IsWindows()) {
                Core.PlaybackManager.Inst.AudioOutput = new Audio.WaveOutAudioOutput();
            } else {
                Core.PlaybackManager.Inst.AudioOutput = new Audio.AudioOutput();
            }
            Core.Formats.Wave.OverrideMp3Reader = filepath => new NAudio.Wave.AudioFileReader(filepath);
        }
    }
}

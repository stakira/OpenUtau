using System;
using System.Text;
using Serilog;

namespace OpenUtau {
    class Program {

        [STAThread]
        private static void Main() {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            App.Program.InitLogging();
            App.Program.InitOpenUtau();
            InitAudio();

            App.Program.InitInterop();
            new WpfApp().Run(new UI.MainWindow());
        }

        private static void InitAudio() {
            Core.PlaybackManager.Inst.AudioOutput = new Audio.WaveOutAudioOutput();
            Core.Formats.Wave.OverrideMp3Reader = filepath => new NAudio.Wave.AudioFileReader(filepath);
        }
    }
}

using System.Diagnostics;
using System.IO;

namespace OpenUtau.Classic {
    public class Plugin : IPlugin {
        public string Name;
        public string Executable;
        public bool AllNotes;
        public bool UseShell;
        private string encoding = "shift_jis";
        public string Shortcut;

        public string Encoding { get => encoding; set => encoding = value; }

        public void Run(string tempFile) {
            if (!File.Exists(Executable)) {
                throw new FileNotFoundException($"Executable {Executable} not found.");
            }
            var startInfo = new ProcessStartInfo() {
                FileName = Executable,
                Arguments = $"\"{tempFile}\"",
                WorkingDirectory = Path.GetDirectoryName(Executable),
                UseShellExecute = UseShell,
            };
            using (var process = Process.Start(startInfo)) {
                process.WaitForExit();
            }
        }

        public override string ToString() => Name;
    }
}

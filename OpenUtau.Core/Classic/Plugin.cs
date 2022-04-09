using System.Diagnostics;
using System.IO;

namespace OpenUtau.Classic {
    public class Plugin {
        public string Name;
        public string Executable;
        public bool AllNotes;
        public bool UseShell;

        public void Run(string tempFile) {
            if (!File.Exists(Executable)) {
                throw new FileNotFoundException($"Executable {Executable} not found.");
            }
            var startInfo = UseShell
                 ? new ProcessStartInfo() {
                     FileName = "cmd.exe",
                     Arguments = $"/K \"{Executable}\" \"{tempFile}\"",
                     UseShellExecute = true,
                 }
                 : new ProcessStartInfo() {
                     FileName = Executable,
                     Arguments = tempFile,
                     WorkingDirectory = Path.GetDirectoryName(Executable),
                 };
            using (var process = Process.Start(startInfo)) {
                process.WaitForExit();
            }
        }

        public override string ToString() => Name;
    }
}

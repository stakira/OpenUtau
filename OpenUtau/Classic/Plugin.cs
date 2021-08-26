using System.Diagnostics;

namespace OpenUtau.Classic {
    class Plugin {
        public string Name;
        public string Executable;
        public bool AllNotes;
        public bool UseShell;

        public void Run(string tempFile) {
            var startInfo = UseShell
                 ? new ProcessStartInfo() {
                     FileName = "cmd.exe",
                     Arguments = $"/K {Executable} {tempFile}",
                     UseShellExecute = true,
                 }
                 : new ProcessStartInfo() {
                     FileName = Executable,
                     Arguments = tempFile,
                 };
            using (var process = Process.Start(startInfo)) {
                process.WaitForExit();
            }
        }

        public override string ToString() => Name;
    }
}

﻿using System.Diagnostics;
using System.IO;
using OpenUtau.Core.Util;

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
            string winePath = Preferences.Default.WinePath;
            bool useWine = !OS.IsWindows() && !string.IsNullOrEmpty(winePath);
            var startInfo = new ProcessStartInfo() {
                FileName = useWine ? winePath : Executable,
                Arguments = useWine ? $"\"{Executable}\" \"{tempFile}\"" : $"\"{tempFile}\"",
                Environment = {{"LANG", "ja_JP.utf8"}},
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

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenUtau {
    public static class OS {
        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsMacOS() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        public static void OpenFolder(string path) {
            if (Directory.Exists(path)) {
                Process.Start(new ProcessStartInfo {
                    FileName = IsWindows() ? "explorer.exe" : IsMacOS() ? "open" : "mimeopen",
                    Arguments = path,
                });
            }
        }

        public static void OpenWeb(string url) {
            Process.Start(new ProcessStartInfo {
                FileName = IsWindows() ? "explorer.exe" : IsMacOS() ? "open" : "mimeopen",
                Arguments = url,
            });
        }
    }
}

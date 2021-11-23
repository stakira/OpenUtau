using System;
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
                    FileName = IsWindows() ? "explorer.exe" : IsMacOS() ? "open" : "xdg-open",
                    Arguments = path,
                });
            }
        }

        public static void OpenWeb(string url) {
            Process.Start(new ProcessStartInfo {
                FileName = IsWindows() ? "explorer.exe" : IsMacOS() ? "open" : "xdg-open",
                Arguments = url,
            });
        }

        public static string GetUpdaterRid() {
            if (IsWindows()) {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X86) {
                    return "win-x86";
                }
                return "win-x64";
            } else if (IsMacOS()) {
                return "osx-x64";
            } else if (IsLinux()) {
                return "linux-x64";
            }
            throw new NotSupportedException();
        }
    }
}

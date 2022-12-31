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
                    FileName = GetOpener(),
                    Arguments = path,
                });
            }
        }

        public static void GotoFile(string path) {
            if (File.Exists(path)) {
                if (IsWindows()) {
                    Process.Start(new ProcessStartInfo {
                        FileName = GetOpener(),
                        Arguments = $"/select, {path}",
                    });
                } else if (IsMacOS()) {
                    Process.Start(new ProcessStartInfo {
                        FileName = GetOpener(),
                        Arguments = $" -R {path}",
                    });
                } else {
                    OpenFolder(Path.GetDirectoryName(path));
                }
            }
        }

        public static void OpenWeb(string url) {
            Process.Start(new ProcessStartInfo {
                FileName = GetOpener(),
                Arguments = url,
            });
        }

        public static bool AppExists(string path) {
            if (IsMacOS()) {
                return Directory.Exists(path) && path.EndsWith(".app");
            } else {
                return File.Exists(path);
            }
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

        public static string WhereIs(string filename) {
            if (File.Exists(filename)) {
                return Path.GetFullPath(filename);
            }
            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(Path.PathSeparator)) {
                var fullPath = Path.Combine(path, filename);
                if (File.Exists(fullPath)) {
                    return fullPath;
                }
            }
            return null;
        }

        private static readonly string[] linuxOpeners = { "xdg-open", "mimeopen", "gnome-open", "open" };
        private static string GetOpener() {
            if (IsWindows()) {
                return "explorer.exe";
            }
            if (IsMacOS()) {
                return "open";
            }
            foreach (var opener in linuxOpeners) {
                string fullPath = WhereIs(opener);
                if (!string.IsNullOrEmpty(fullPath)) {
                    return fullPath;
                }
            }
            throw new IOException($"None of {string.Join(", ", linuxOpeners)} found.");
        }
    }
}

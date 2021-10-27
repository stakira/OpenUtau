using System;
using System.IO;
using System.Runtime.InteropServices;
using Serilog;

namespace OpenUtau {
    internal sealed class LibraryLoader : IDisposable {
        private const int RTLD_NOW = 2;
        private readonly IntPtr _handle;
        private bool _disposed;

        public LibraryLoader(string path, string library) {
            Log.Information($"Is64BitOperatingSystem {Environment.Is64BitOperatingSystem} Is64BitProcess {Environment.Is64BitProcess}");
            if (OS.IsWindows()) {
                string attempt1 = Environment.Is64BitProcess ? "win-x64" : "win-x86";
                string attempt2 = Environment.Is64BitProcess ? "win-x86" : "win-x64";
                _handle = LoadLibrary(Path.Combine(path, attempt1, $"{library}.dll"));
                if (_handle == IntPtr.Zero) {
                    Log.Error($"Error loading {attempt1}: {Marshal.GetLastWin32Error()}");
                    _handle = LoadLibrary(Path.Combine(path, attempt2, $"{library}.dll"));
                }
                if (_handle == IntPtr.Zero) {
                    Log.Error($"Error loading {attempt2}: {Marshal.GetLastWin32Error()}");
                }
            } else if (OS.IsLinux()) {
                string lib = Path.Combine(path, "linux-x64", $"lib{library}.so");
                Log.Information($"Loading {lib}");
                _handle = dlopen(lib, RTLD_NOW);
            } else if (OS.IsMacOS()) {
                string lib = Path.Combine(path, "osx-x64", $"lib{library}.dylib");
                Log.Information($"Loading {lib}");
                _handle = dlopen(lib, RTLD_NOW);
            } else {
                throw new NotSupportedException("Platform not supported.");
            }
            if (_handle == IntPtr.Zero) {
                throw new Exception($"Could not load libary {library}.");
            }
        }

        public TDelegate LoadFunc<TDelegate>(string name) {
            IntPtr ptr = IntPtr.Zero;
            if (OS.IsWindows()) {
                ptr = GetProcAddress(_handle, name);
            } else if (OS.IsLinux() || OS.IsMacOS()) {
                ptr = dlsym(_handle, name);
            }
            if (_handle == IntPtr.Zero) {
                throw new Exception($"Could not load function name: {name}.");
            }
            return Marshal.GetDelegateForFunctionPointer<TDelegate>(ptr);
        }

        public void Dispose() {
            if (_disposed) {
                return;
            }
            if (OS.IsWindows()) {
                FreeLibrary(_handle);
            } else {
                dlclose(_handle);
            }
            _disposed = true;
        }

        [DllImport("kernel32", SetLastError = true)] static extern IntPtr LoadLibrary(string fileName);
        [DllImport("kernel32")] static extern IntPtr GetProcAddress(IntPtr module, string procName);
        [DllImport("kernel32")] static extern int FreeLibrary(IntPtr module);
        [DllImport("libdl")] static extern IntPtr dlopen(string fileName, int flags);
        [DllImport("libdl")] static extern IntPtr dlsym(IntPtr handle, string symbol);
        [DllImport("libdl")] static extern int dlclose(IntPtr handle);
    }
}

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
                string attempt1 = OS.IsIntel64 ? "win-x64" : "win-x86";
                string attempt2 = OS.IsIntel64 ? "win-x86" : "win-x64";
                _handle = LoadLibrary(Path.Combine(path, attempt1, $"{library}.dll"));
                if (_handle == IntPtr.Zero) {
                    Log.Error($"Error loading {attempt1}: {Marshal.GetLastWin32Error()}");
                    _handle = LoadLibrary(Path.Combine(path, attempt2, $"{library}.dll"));
                }
                if (_handle == IntPtr.Zero) {
                    Log.Error($"Error loading {attempt2}: {Marshal.GetLastWin32Error()}");
                }
            } else if (OS.IsLinux()) {
		string lib = "";
		if(OS.IsArm64)
		{
		    lib = Path.Combine(path, "linux-arm64", $"lib{library}.so");
		}
		//else if(OS.IsArm32)
		//{
		//    lib = Path.Combine(path, "linux-armhf", $"lib{library}.so");
		//}else
		else
		{
	            lib = Path.Combine(path, "linux-x64", $"lib{library}.so");
		}
		string[] FindLib = OS.WhereIsLib($"lib{library}.so.2");
		if(FindLib.Length>1){lib = FindLib[1];}
		else
		{
		    FindLib = OS.WhereIsLib($"lib{library}.so");
   		    if(FindLib.Length>1){lib = FindLib[1];}
        	}
                Log.Information($"Loading {lib}");
                _handle = dlopenL(lib, RTLD_NOW);
            } else if (OS.IsMacOS()) {
		string lib = "";
		//if(OS.IsArm64)
		//{
		//    lib = Path.Combine(path, "osx-arm64", $"lib{library}.so");
		//}else
		//{
                    lib = Path.Combine(path, "osx-x64", $"lib{library}.dylib");
		//}
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
            } else if (OS.IsLinux()) {
                ptr = dlsymL(_handle, name);
            } else if (OS.IsMacOS()) {
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
            } else if (OS.IsLinux()) {
                dlcloseL(_handle);
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
        [DllImport("libdl.so.2",EntryPoint="dlopen")] static extern IntPtr dlopenL(string fileName, int flags);
        [DllImport("libdl.so.2",EntryPoint="dlsym")] static extern IntPtr dlsymL(IntPtr handle, string symbol);
        [DllImport("libdl.so.2",EntryPoint="dlclose")] static extern int dlcloseL(IntPtr handle);

        // https://stackoverflow.com/a/15608028
        public static bool IsManagedAssembly(string fileName) {
            using var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            using var binaryReader = new BinaryReader(fileStream);
            if (fileStream.Length < 64) {
                return false;
            }

            //PE Header starts @ 0x3C (60). Its a 4 byte header.
            fileStream.Position = 0x3C;
            uint peHeaderPointer = binaryReader.ReadUInt32();
            if (peHeaderPointer == 0) {
                peHeaderPointer = 0x80;
            }

            // Ensure there is at least enough room for the following structures:
            //     24 byte PE Signature & Header
            //     28 byte Standard Fields         (24 bytes for PE32+)
            //     68 byte NT Fields               (88 bytes for PE32+)
            // >= 128 byte Data Dictionary Table
            if (peHeaderPointer > fileStream.Length - 256) {
                return false;
            }

            // Check the PE signature.  Should equal 'PE\0\0'.
            fileStream.Position = peHeaderPointer;
            uint peHeaderSignature = binaryReader.ReadUInt32();
            if (peHeaderSignature != 0x00004550) {
                return false;
            }

            // skip over the PEHeader fields
            fileStream.Position += 20;

            const ushort PE32 = 0x10b;
            const ushort PE32Plus = 0x20b;

            // Read PE magic number from Standard Fields to determine format.
            var peFormat = binaryReader.ReadUInt16();
            if (peFormat != PE32 && peFormat != PE32Plus) {
                return false;
            }

            // Read the 15th Data Dictionary RVA field which contains the CLI header RVA.
            // When this is non-zero then the file contains CLI data otherwise not.
            ushort dataDictionaryStart = (ushort)(peHeaderPointer + (peFormat == PE32 ? 232 : 248));
            fileStream.Position = dataDictionaryStart;

            uint cliHeaderRva = binaryReader.ReadUInt32();
            if (cliHeaderRva == 0) {
                return false;
            }

            return true;
        }
    }
}

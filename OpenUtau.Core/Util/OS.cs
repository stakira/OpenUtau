using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenUtau {
    public static class OS {
        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsMacOS() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
	public static bool IsIntel32 = (RuntimeInformation.ProcessArchitecture == Architecture.X86);
	public static bool IsIntel64 = (RuntimeInformation.ProcessArchitecture == Architecture.X64);
	public static bool IsArm32 = (RuntimeInformation.ProcessArchitecture == Architecture.Arm);
	public static bool IsArm64 = (RuntimeInformation.ProcessArchitecture == Architecture.Arm64);

        public static void OpenFolder(string path) {
            if (Directory.Exists(path)) {
                Process.Start(new ProcessStartInfo {
                    FileName = GetOpener(),
                    Arguments = path,
                });
            }
        }

        public static void OpenWeb(string url) {
            Process.Start(new ProcessStartInfo {
                FileName = GetOpener(),
                Arguments = url,
            });
        }

        public static string GetUpdaterRid() {
            if (IsWindows()) {
		if (IsIntel32)
		{
                    return "win-x86";
                }else
		{
                    return "win-x64";
		}
            } else if (IsMacOS()) {
		//if (IsArm64)
		//{
		//    return "osx-arm64"; //M1
		//}
                return "osx-x64";
            } else if (IsLinux()) {
 		//if (IsIntel32)
		//{
                //    return "linux-x86";
                //}else 
		if(IsIntel64)
		{
                    return "linux-x64";
		}
		//else if(IsArm32)
		//{
		//    return "linux-armhf";
		//}
		else if(IsArm64)
		{
		    return "linux-arm64";
		}
            }
            throw new NotSupportedException();
        }

	// This Function is to findout where is the target lib in system. Just for Linux and Mac
	public static string[] WhereIsLib(string libname) {
	    System.Collections.Generic.List<string> tmp=new System.Collections.Generic.List<string>();
	    if(IsWindows())
	    {
		    return tmp.ToArray();
	    }
	    using(Process proc=new Process())
	    {
		    proc.StartInfo.FileName = "whereis";
		    proc.StartInfo.Arguments = libname;
		    proc.StartInfo.UseShellExecute = false;
		    proc.StartInfo.RedirectStandardOutput = true;
		    proc.Start();
		    string responseLine=proc.StandardOutput.ReadToEnd().Replace("\n","").Trim();
		    proc.WaitForExit();
                    string[] responseHeadArray=responseLine.Split(':');
		    if(responseHeadArray.Length>1) //Response is correct!
		    {
			string defaultName = responseHeadArray[0].Trim(); //get the default name of library
                        tmp.Add(defaultName);
 		        string[] ContentArray=responseHeadArray[1].Trim().Split(' ');
                        for(int i=0;i<ContentArray.Length;i++)
                        {
				    string Kpath=ContentArray[i];
				    if(!Kpath.EndsWith(".a"))
					tmp.Add(Kpath);
                        }
                    }
            }
            return tmp.ToArray();
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

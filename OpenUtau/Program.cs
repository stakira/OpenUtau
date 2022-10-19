using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using Serilog;

namespace OpenUtau.App {
    public class Program {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) {
            // file association WIP
            // if OS is windows
            if (OS.IsWindows()) {
                //if file extension doesn't already exist, create it
                // (this requires administrator access at the moment)
                if (!FileAssociation.IsAssociated(".ustx"))
                    //associate file extension as ".ustx"
                    //associate icon, point icon to this app's icon resource
                    FileAssociation.Associate(".ustx", "ClassID.ProgID", "OpenUTAU sequence", "ustx.ico", System.AppDomain.CurrentDomain.BaseDirectory+"\\OpenUTAU.exe");
            }

            //else if OS is macOS
            if (OS.IsMacOS()) {
                //if file extension doesn't already exist, create it

                //associate file extension as ".ustx"
                //associate icon, point icon to this app's icon resource
            }

            //else if OS is linux/ubuntu
            if (OS.IsLinux()) {
                //if file extension doesn't already exist, create it

                //associate file extension as ".ustx"
                //associate icon, point icon to this app's icon resource
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            //start logging
            InitLogging();

            //get current process name
            string processName = Process.GetCurrentProcess().ProcessName;
            if (processName != "dotnet") {
                var exists = Process.GetProcessesByName(processName).Count() > 1;
                //if process already exists (one process only)
                if (exists) {
                    Log.Information($"Process {processName} already open. Exiting.");
                    return;
                }
            }

            //logging stuff
            Log.Information($"{Environment.OSVersion}");
            Log.Information($"{RuntimeInformation.OSDescription} " +
                $"{RuntimeInformation.OSArchitecture} " +
                $"{RuntimeInformation.ProcessArchitecture}");
            Log.Information($"OpenUtau v{Assembly.GetEntryAssembly()?.GetName().Version} " +
                $"{RuntimeInformation.RuntimeIdentifier}");

            //try running the app
            try {
                Run(args);
                Log.Information($"Exiting.");
            } finally {
                if (!OS.IsMacOS()) {
                    NetMQ.NetMQConfig.Cleanup(/*block=*/false);
                    // Cleanup() hangs on macOS https://github.com/zeromq/netmq/issues/1018
                }
            }
            Log.Information($"Exited.");
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();

        public static void Run(string[] args)
            => BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(
                    args, ShutdownMode.OnMainWindowClose);

        public static void InitLogging() {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Debug()
                .WriteTo.Logger(lc => lc
                    .MinimumLevel.Information()
                    .WriteTo.File(PathManager.Inst.LogFilePath, rollingInterval: RollingInterval.Day, encoding: Encoding.UTF8))
                .WriteTo.Logger(lc => lc
                    .MinimumLevel.ControlledBy(DebugViewModel.Sink.Inst.LevelSwitch)
                    .WriteTo.Sink(DebugViewModel.Sink.Inst))
                .CreateLogger();
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((sender, args) => {
                Log.Error((Exception)args.ExceptionObject, "Unhandled exception");
            });
            Log.Information("Logging initialized.");
        }
    }

    //set file association
    public class FileAssociation {
        // Associate file extension with progID, description, icon and application
        public static void Associate(string extension, string progID, string description, string icon, string application) {
            Registry.ClassesRoot.CreateSubKey(extension).SetValue("", progID);
            if (progID != null && progID.Length > 0)
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(progID)) {
                    if (description != null)
                        key.SetValue("", description);
                    if (icon != null)
                        key.CreateSubKey("DefaultIcon").SetValue("", ToShortPathName(icon));
                    if (application != null)
                        key.CreateSubKey(@"Shell\Open\Command").SetValue("",
                                    ToShortPathName(application) + " \"%1\"");
                }
        }

        // Return true if extension already associated in registry
        public static bool IsAssociated(string extension) {
            return (Registry.ClassesRoot.OpenSubKey(extension, false) != null);
        }

        [DllImport("Kernel32.dll")]
        private static extern uint GetShortPathName(string lpszLongPath,
            [Out] StringBuilder lpszShortPath, uint cchBuffer);

        // Return short path format of a file name
        private static string ToShortPathName(string longName) {
            StringBuilder s = new StringBuilder(1000);
            uint iSize = (uint)s.Capacity;
            uint iRet = GetShortPathName(longName, s, iSize);
            return s.ToString();
        }
    }
}

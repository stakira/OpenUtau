// Made And Checked By DELTA SYNTH & Gemini AI
// ต้นฉบับโดย OpenUtau Team (https://github.com/stakira/OpenUtau)

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using Serilog;

namespace OpenUtau.App {
    public class Program {
        // โค้ดเริ่มต้นการทำงาน ห้ามเรียกใช้ Avalonia หรือ API ภายนอกก่อนที่ AppMain จะถูกเรียก
        [STAThread]
        public static void Main(string[] args) {
            // ลงทะเบียนผู้ให้บริการ Encoding เพื่อรองรับรหัสภาษาที่หลากหลาย
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            InitLogging();

            string processName = Process.GetCurrentProcess().ProcessName;
            if (processName != "dotnet") {
                // ตรวจสอบว่าโปรแกรมถูกเปิดซ้ำซ้อนหรือไม่
                var exists = Process.GetProcessesByName(processName).Count() > 1;
                if (exists) {
                    Log.Information($"โปรเซส {processName} กำลังทำงานอยู่แล้ว ระบบกำลังดำเนินการปิดการทำงานที่ซ้ำซ้อน");
                    return;
                }
            }

            // บันทึกข้อมูลระบบลงใน Log เพื่อการตรวจสอบปัญหา
            Log.Information($"ระบบปฏิบัติการ: {Environment.OSVersion}");
            Log.Information($"{RuntimeInformation.OSDescription} " +
                $"{RuntimeInformation.OSArchitecture} " +
                $"{RuntimeInformation.ProcessArchitecture}");
            Log.Information($"OpenUtau เวอร์ชั่น: v{Assembly.GetEntryAssembly()?.GetName().Version} " +
                $"{RuntimeInformation.RuntimeIdentifier}");
            Log.Information($"ที่อยู่ข้อมูล (Data Path): {PathManager.Inst.DataPath}");
            Log.Information($"ที่อยู่แคช (Cache Path): {PathManager.Inst.CachePath}");
            Log.Information($"การเข้ารหัสระบบ: {Encoding.GetEncoding(0)?.WebName ?? "null"}");

            try {
                Run(args);
                Log.Information($"กำลังปิดโปรแกรมอย่างปกติ");
            } catch (Exception ex) {
                Log.Fatal(ex, "เกิดข้อผิดพลาดร้ายแรงขณะรันโปรแกรม");
            } finally {
                if (!OS.IsMacOS()) {
                    // ทำความสะอาดระบบเครือข่ายสำหรับ Windows/Linux
                    NetMQ.NetMQConfig.Cleanup(/*block=*/false);
                }
            }
            Log.Information($"ปิดการทำงานเรียบร้อย");
        }

        // การตั้งค่า Avalonia สำหรับส่วนติดต่อผู้ใช้ (UI)
        public static AppBuilder BuildAvaloniaApp() {
            FontManagerOptions fontOptions = new();
            
            // ปรับสมดุลการแสดงผลฟอนต์ภาษาไทยให้ครอบคลุมทุก OS
            string thaiFonts = "Leelawadee UI, Tahoma, Sarabun, Ayuthaya, Thonburi, FreeSans";

            if (OS.IsLinux()) {
                using Process process = Process.Start(new ProcessStartInfo("fc-match")
                {
                    ArgumentList = { "-f", "%{family}" },
                    RedirectStandardOutput = true
                })!;
                process.WaitForExit();

                string fontFamily = process.StandardOutput.ReadToEnd();
                if (!string.IsNullOrEmpty(fontFamily)) {
                    string[] fontFamilies = fontFamily.Split(',');
                    fontOptions.DefaultFamilyName = $"{fontFamilies[0]}, {thaiFonts}";
                }
            } else if (OS.IsMacOS()) {
                // สำหรับ macOS เน้นฟอนต์ที่แสดงผลภาษาไทยและญี่ปุ่นได้ชัดเจน
                fontOptions.DefaultFamilyName = $"Hiragino Sans, {thaiFonts}, San Francisco, Helvetica Neue";
            } else if (OS.IsWindows()) {
                // สำหรับ Windows เน้น Leelawadee UI ซึ่งเป็นมาตรฐานของภาษาไทย
                fontOptions.DefaultFamilyName = $"Segoe UI, {thaiFonts}";
            }

            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI()
                .With(fontOptions) // ใช้การตั้งค่าฟอนต์ที่เราปรับปรุงแล้ว
                .With(new X11PlatformOptions { EnableIme = true }); // รองรับการพิมพ์ภาษาไทย (IME) บน Linux
        }

        public static void Run(string[] args)
            => BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(
                    args, ShutdownMode.OnMainWindowClose);

        // ระบบบันทึก Log และการจัดการข้อผิดพลาดที่ไม่คาดคิด
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
                Log.Error((Exception)args.ExceptionObject, "พบข้อผิดพลาดที่ไม่สามารถจัดการได้ (Unhandled Exception)");
            });
            Log.Information("เริ่มต้นระบบบันทึกข้อมูลเรียบร้อยแล้ว");
        }
    }
}

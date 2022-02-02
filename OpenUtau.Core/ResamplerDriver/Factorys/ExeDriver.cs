using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Core.ResamplerDriver.Factorys {
    internal class ExeDriver : DriverModels, IResamplerDriver {
        public string Name { get; private set; }
        public string FilePath { get; private set; }
        public bool isLegalPlugin => _isLegalPlugin;

        readonly bool _isLegalPlugin = false;

        public ExeDriver(string filePath, string basePath) {
            if (File.Exists(filePath)) {
                FilePath = filePath;
                Name = Path.GetRelativePath(basePath, filePath);
                _isLegalPlugin = true;
            }
        }

        public byte[] DoResampler(EngineInput Args, ILogger logger) {
            string tmpFile = DoResamplerReturnsFile(Args, logger);
            byte[] data = new byte[0];
            if (string.IsNullOrEmpty(tmpFile) || File.Exists(tmpFile)) {
                data = File.ReadAllBytes(tmpFile);
                File.Delete(tmpFile);
            }
            return data;
        }

        public string DoResamplerReturnsFile(EngineInput Args, ILogger logger) {
            bool resamplerLogging = Preferences.Default.ResamplerLogging;
            if (!_isLegalPlugin) {
                return null;
            }
            var threadId = Thread.CurrentThread.ManagedThreadId;
            string tmpFile = Path.GetTempFileName();
            string ArgParam = FormattableString.Invariant(
                $"\"{Args.inputWaveFile}\" \"{tmpFile}\" {Args.NoteString} {Args.Velocity} \"{Args.StrFlags}\" {Args.Offset} {Args.RequiredLength} {Args.Consonant} {Args.Cutoff} {Args.Volume} {Args.Modulation} !{Args.Tempo} {Base64.Base64EncodeInt12(Args.pitchBend)}");
            logger.Information($" > [thread-{threadId}] {FilePath} {ArgParam}");
            using (var proc = new Process()) {
                proc.StartInfo = new ProcessStartInfo(FilePath, ArgParam) {
                    UseShellExecute = false,
                    RedirectStandardOutput = resamplerLogging,
                    RedirectStandardError = resamplerLogging,
                    CreateNoWindow = true,
                };
                if (resamplerLogging) {
                    proc.OutputDataReceived += (o, e) => logger.Information($" >>> [thread-{threadId}] {e.Data}");
                    proc.ErrorDataReceived += (o, e) => logger.Error($" >>> [thread-{threadId}] {e.Data}");
                }
                proc.Start();
                if (resamplerLogging) {
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                }
                if (!proc.WaitForExit(60000)) {
                    logger.Warning($"[thread-{threadId}] Timeout, killing...");
                    try {
                        proc.Kill();
                        logger.Warning($"[thread-{threadId}] Killed.");
                    } catch (Exception e) {
                        logger.Error(e, $"[thread-{threadId}] Failed to kill");
                    }
                }
            }
            return tmpFile;
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int chmod(string pathname, int mode);

        public void CheckPermissions() {
            if (OS.IsWindows() || !File.Exists(FilePath)) {
                return;
            }
            int mode = (7 << 6) | (5 << 3) | 5;
            chmod(FilePath, mode);
        }

        public override string ToString() => Name;
    }
}

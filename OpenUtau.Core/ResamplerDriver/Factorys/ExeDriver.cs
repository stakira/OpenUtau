using System;
using System.Diagnostics;
using System.IO;
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
                if (Path.GetExtension(filePath).ToLower() == ".exe" ||
                    Path.GetExtension(filePath).ToLower() == ".sh") {
                    FilePath = filePath;
                    Name = Path.GetRelativePath(basePath, filePath);
                    _isLegalPlugin = true;
                }
            }
        }

        public byte[] DoResampler(EngineInput Args, ILogger logger) {
            const bool debugResampler = true;
            byte[] data = new byte[0];
            if (!_isLegalPlugin) {
                return data;
            }
            var threadId = Thread.CurrentThread.ManagedThreadId;
            string tmpFile = Path.GetTempFileName();
            string ArgParam = FormattableString.Invariant(
                $"\"{Args.inputWaveFile}\" \"{tmpFile}\" {Args.NoteString} {Args.Velocity} \"{Args.StrFlags}\" {Args.Offset} {Args.RequiredLength} {Args.Consonant} {Args.Cutoff} {Args.Volume} {Args.Modulation} !{Args.Tempo} {Base64.Base64EncodeInt12(Args.pitchBend)}");
            logger.Information($" > [thread-{threadId}] {FilePath} {ArgParam}");
            using (var proc = new Process()) {
                proc.StartInfo = new ProcessStartInfo(FilePath, ArgParam) {
                    UseShellExecute = false,
                    RedirectStandardOutput = debugResampler,
                    RedirectStandardError = debugResampler,
                    CreateNoWindow = true,
                };
#pragma warning disable CS0162 // Unreachable code detected
                if (debugResampler) {
                    proc.OutputDataReceived += (o, e) => logger.Information($" >>> [thread-{threadId}] {e.Data}");
                    proc.ErrorDataReceived += (o, e) => logger.Error($" >>> [thread-{threadId}] {e.Data}");
                }
                proc.Start();
                if (debugResampler) {
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                }
#pragma warning restore CS0162 // Unreachable code detected
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
            if (File.Exists(tmpFile)) {
                data = File.ReadAllBytes(tmpFile);
                File.Delete(tmpFile);
            }
            return data;
        }

        public override string ToString() => Name;
    }
}

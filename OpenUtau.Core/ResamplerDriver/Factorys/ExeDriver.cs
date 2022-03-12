using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using NAudio.Wave;
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

        public float[] DoResampler(EngineInput args, ILogger logger) {
            string tmpFile = DoResamplerReturnsFile(args, logger);
            if (string.IsNullOrEmpty(tmpFile) || File.Exists(tmpFile)) {
                using (var waveStream = Formats.Wave.OpenFile(tmpFile)) {
                    return Formats.Wave.GetSamples(waveStream.ToSampleProvider().ToMono(1, 0));
                }
            }
            return new float[0];
        }

        public string DoResamplerReturnsFile(EngineInput args, ILogger logger) {
            bool resamplerLogging = Preferences.Default.ResamplerLogging;
            if (!_isLegalPlugin) {
                return null;
            }
            var threadId = Thread.CurrentThread.ManagedThreadId;
            string tmpFile = args.outputWaveFile;
            string ArgParam = FormattableString.Invariant(
                $"\"{args.inputWaveFile}\" \"{tmpFile}\" {args.NoteString} {args.Velocity} \"{args.StrFlags}\" {args.Offset} {args.RequiredLength} {args.Consonant} {args.Cutoff} {args.Volume} {args.Modulation} !{args.Tempo} {Base64.Base64EncodeInt12(args.pitchBend)}");
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

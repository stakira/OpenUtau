using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Neutrino {
    public class NeutrinoRenderer : IRenderer {
        static readonly SemaphoreSlim gate = new SemaphoreSlim(1);
        static Process? ownedServer;
        public USingerType SingerType => USingerType.Neutrino;
        public bool SupportsPhonemeEnvelope => false;
        public override string ToString() => Renderers.NEUTRINO;
        public bool SupportsExpression(UExpressionDescriptor descriptor) => descriptor.abbr == Format.Ustx.DYN;
        public RenderPitchResult LoadRenderedPitch(RenderPhrase phrase) => null!;
        public UExpressionDescriptor[] GetSuggestedExpressions(USinger singer, URenderSettings settings) => Array.Empty<UExpressionDescriptor>();
        public (double HeadMs, double TailMs) PhrasePadding(USinger singer, IEnumerable<UPhoneme> phonemes)
            => (NeutrinoScore.PaddingMs, NeutrinoScore.PaddingMs);

        public RenderResult Layout(RenderPhrase phrase) => new RenderResult {
            positionMs = phrase.notes[0].positionMs,
            leadingMs = NeutrinoScore.PaddingMs,
            estimatedLengthMs = phrase.notes[^1].endMs - phrase.notes[0].positionMs + 2 * NeutrinoScore.PaddingMs,
        };

        public async Task<RenderResult> Render(RenderPhrase phrase, Progress progress, int trackNo,
                CancellationTokenSource cancellation, bool isPreRender = false, RenderPhraseEvents? renderEvents = null) {
            if (!OperatingSystem.IsWindows()) { throw new PlatformNotSupportedException("NEUTRINO Tau integration currently requires Windows."); }
            var token = cancellation.Token;
            await gate.WaitAsync(token);
            try {
                var singer = (NeutrinoSinger)phrase.singer;
                var config = singer.ReadConfig();
                string info = $"Track {trackNo + 1}: NEUTRINO {singer.Name}";
                progress.Complete(0, info);
                if (phrase.notes.All(n => NeutrinoScore.IsRestLyric(n.lyric))) {
                    var silence = Layout(phrase);
                    silence.samples = new float[(int)Math.Ceiling(silence.estimatedLengthMs * 44.1)];
                    progress.Complete(phrase.phones.Length, info);
                    return silence;
                }
                var score = NeutrinoScore.Build(phrase);
                // Include executable/model identity: changing a wrapper must invalidate the cache.
                string modelInfo = File.ReadAllText(Path.Combine(config.ModelPath, "info.toml"));
                var exe = new FileInfo(Path.Combine(config.EnginePath, "bin", "neutrino_server.exe"));
                string identity = $"neutrino-tau-v2\n{config.EnginePath}\n{config.ModelPath}\n{modelInfo}\n{exe.Length}\n{exe.LastWriteTimeUtc.Ticks}\n{score}";
                string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
                string prefix = Path.Combine(PathManager.Inst.CachePath, $"neutrino-{hash}");
                Directory.CreateDirectory(PathManager.Inst.CachePath);
                string wav = prefix + ".wav";
                phrase.AddCacheFile(wav);
                if (!File.Exists(wav)) {
                    string xml = prefix + ".musicxml", full = prefix + ".full.lab", timing = prefix + ".timing.lab";
                    string f0 = prefix + ".f0", melspec = prefix + ".melspec", temporaryWav = prefix + ".tmp.wav";
                    var intermediates = new[] { xml, full, timing, f0, melspec, temporaryWav };
                    try {
                        score.Save(xml);
                        string conversion = await Run(config, "musicXMLtoLabel.exe", new[] { xml, full, timing }, token);
                        if (conversion.Contains("Undefined lyric", StringComparison.OrdinalIgnoreCase)) {
                            throw new InvalidDataException("O NEUTRINO encontrou uma letra que não reconhece. Use sílabas japonesas em hiragana ou katakana. " + conversion);
                        }
                        if (!File.Exists(full) || new FileInfo(full).Length == 0) {
                            throw new InvalidDataException("NEUTRINO could not convert the score. Use Japanese kana lyrics on every note.");
                        }
                        await EnsureServer(config, token);
                        await Run(config, "neutrino_client.exe", new[] { full, timing, f0, melspec, temporaryWav, config.ModelPath }, token);
                        if (!File.Exists(temporaryWav) || new FileInfo(temporaryWav).Length <= 44) {
                            throw new InvalidDataException("NEUTRINO returned no audio. Check the model and the engine's output in the log.");
                        }
                        using (var reader = new WaveFileReader(temporaryWav)) {
                            if (reader.Length == 0) { throw new InvalidDataException("NEUTRINO returned an empty WAV."); }
                        }
                        token.ThrowIfCancellationRequested();
                        File.Move(temporaryWav, wav, true);
                    } finally {
                        foreach (string path in intermediates) { if (File.Exists(path)) { File.Delete(path); } }
                    }
                }
                token.ThrowIfCancellationRequested();
                var result = Layout(phrase);
                using (var reader = new WaveFileReader(wav)) {
                    result.samples = Format.Wave.GetSamples(reader.ToSampleProvider().ToMono(1, 0));
                }
                // The XML ends on a measure boundary; remove the extra trailing rest.
                int length = (int)Math.Ceiling(result.estimatedLengthMs * 44.1);
                if (result.samples.Length > length) { Array.Resize(ref result.samples, length); }
                Renderers.ApplyDynamics(phrase, result);
                progress.Complete(phrase.phones.Length, info);
                return result;
            } catch (OperationCanceledException) {
                // Killing the client alone leaves inference running in the daemon.
                if (ownedServer != null && !ownedServer.HasExited) { ownedServer.Kill(true); }
                ownedServer?.Dispose();
                ownedServer = null;
                throw;
            } finally { gate.Release(); }
        }

        static async Task EnsureServer(NeutrinoConfig config, CancellationToken token) {
            var owner = new[] { "--owner-pid", Environment.ProcessId.ToString(), "status" };
            var status = await Run(config, "neutrino_client.exe", owner, token, false);
            if (!status.Contains("READY", StringComparison.Ordinal)) {
                if (!status.Contains("BUSY", StringComparison.Ordinal) && (ownedServer == null || ownedServer.HasExited)) {
                    ownedServer?.Dispose();
                    var start = CreateStart(config, "neutrino_server.exe");
                    using var current = Process.GetCurrentProcess();
                    foreach (var arg in new[] { "--owner-pid", Environment.ProcessId.ToString(), "--owner-created", current.StartTime.ToFileTimeUtc().ToString() }) {
                        start.ArgumentList.Add(arg);
                    }
                    ownedServer = Process.Start(start) ?? throw new InvalidOperationException("Could not start NEUTRINO server.");
                    ownedServer.OutputDataReceived += (_, e) => { if (e.Data != null) { Serilog.Log.Information("NEUTRINO server: {Output}", e.Data); } };
                    ownedServer.ErrorDataReceived += (_, e) => { if (e.Data != null) { Serilog.Log.Information("NEUTRINO server: {Output}", e.Data); } };
                    ownedServer.BeginOutputReadLine();
                    ownedServer.BeginErrorReadLine();
                }
                for (int i = 0; i < 240; i++) {
                    await Task.Delay(250, token);
                    status = await Run(config, "neutrino_client.exe", owner, token, false);
                    if (status.Contains("READY", StringComparison.Ordinal)) { return; }
                    if (ownedServer != null && ownedServer.HasExited) {
                        throw new InvalidOperationException($"NEUTRINO server exited with code {ownedServer.ExitCode}. Check that bin contains the engine DLLs.");
                    }
                }
                throw new TimeoutException($"NEUTRINO server was not ready after 60 seconds: {status}");
            }
        }

        static ProcessStartInfo CreateStart(NeutrinoConfig config, string executable) => new ProcessStartInfo {
            FileName = Path.Combine(config.EnginePath, "bin", executable),
            WorkingDirectory = config.EnginePath,
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
        };

        static async Task<string> Run(NeutrinoConfig config, string executable, IEnumerable<string> args,
                CancellationToken token, bool requireSuccess = true) {
            var start = CreateStart(config, executable);
            foreach (string arg in args) { start.ArgumentList.Add(arg); }
            using var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {executable}.");
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            try { await process.WaitForExitAsync(token); }
            catch (OperationCanceledException) {
                if (!process.HasExited) { process.Kill(true); }
                await process.WaitForExitAsync();
                throw;
            }
            string output = await stdout + "\n" + await stderr;
            Serilog.Log.Information("NEUTRINO {Executable}: {Output}", executable, output);
            if (requireSuccess && (process.ExitCode != 0 || output.Contains("ERROR:", StringComparison.OrdinalIgnoreCase) || output.Contains("Error:", StringComparison.OrdinalIgnoreCase) || output.Contains("[ERROR]", StringComparison.OrdinalIgnoreCase))) {
                throw new InvalidOperationException($"{executable} failed ({process.ExitCode}): {output}");
            }
            return output;
        }
    }
}

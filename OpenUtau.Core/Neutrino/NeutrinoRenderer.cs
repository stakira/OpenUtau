using System;
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
        static NeutrinoOnnxEngine? engine;
        static string? engineIdentity;
        public USingerType SingerType => USingerType.Neutrino;
        public bool SupportsPhonemeEnvelope => false;
        public override string ToString() => Renderers.NEUTRINO;
        public bool SupportsExpression(UExpressionDescriptor descriptor) => descriptor.abbr is Format.Ustx.DYN or Format.Ustx.PITD;
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
                var pitchOffsets = SamplePitchOffsets(phrase);
                byte[] pitchBytes = new byte[pitchOffsets.Length * sizeof(float)];
                Buffer.BlockCopy(pitchOffsets, 0, pitchBytes, 0, pitchBytes.Length);
                string pitchIdentity = Convert.ToHexString(SHA256.HashData(pitchBytes));
                // Include model and backend identity to invalidate incompatible cached audio.
                string modelInfo = File.ReadAllText(Path.Combine(config.ModelPath, "info.toml"));
                string modelIdentity = config.ModelPath + string.Join("|", new[] { "t.bin", "p.bin", "s.bin", "v.bin" }.Select(name => {
                    var file = new FileInfo(Path.Combine(config.ModelPath, name));
                    return $"{name}:{file.Length}:{file.LastWriteTimeUtc.Ticks}";
                }));
                string identity = $"neutrino-tau-internal-phones-v7\n{config.ModelPath}\n{modelInfo}\n{modelIdentity}\n{pitchIdentity}\n{score}";
                string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
                string prefix = Path.Combine(PathManager.Inst.CachePath, $"neutrino-{hash}");
                Directory.CreateDirectory(PathManager.Inst.CachePath);
                string wav = prefix + ".wav";
                phrase.AddCacheFile(wav);
                var elapsed = System.Diagnostics.Stopwatch.StartNew();
                Serilog.Log.Information("NEUTRINO {Singer}: {DurationMs} ms, cache={Cached}", singer.Name, Layout(phrase).estimatedLengthMs, File.Exists(wav));
                if (!File.Exists(wav)) {
                    string temporaryWav = prefix + ".tmp.wav";
                    var intermediates = new[] { temporaryWav };
                    try {
                        var phones = NeutrinoPhonemes.Build(score);
                        if (engine == null || engineIdentity != modelIdentity) {
                            engine?.Dispose();
                            engine = null;
                            progress.Complete(0, info + " — loading ONNX models");
                            engine = new NeutrinoOnnxEngine(config.ModelPath);
                            Serilog.Log.Information("NEUTRINO {Singer}: models loaded in {Seconds} s", singer.Name, elapsed.Elapsed.TotalSeconds);
                            engineIdentity = modelIdentity;
                        }
                        double durationMs = Layout(phrase).estimatedLengthMs;
                        progress.Complete(0, info + " — synthesizing");
                        float[] samples48k = await Task.Run(() => engine.Synthesize(phones, durationMs, token, pitchOffsets), token);
                        using (var writer = new WaveFileWriter(temporaryWav, WaveFormat.CreateIeeeFloatWaveFormat(48000, 1))) {
                            writer.WriteSamples(samples48k, 0, samples48k.Length);
                        }
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
                Serilog.Log.Information("NEUTRINO {Singer}: audio ready in {Seconds} s, {Samples} samples", singer.Name, elapsed.Elapsed.TotalSeconds, result.samples.Length);
                progress.Complete(phrase.phones.Length, info);
                return result;
            } finally { gate.Release(); }
        }

        internal static float[] SamplePitchOffsets(RenderPhrase phrase) {
            double startMs = phrase.notes[0].positionMs - NeutrinoScore.PaddingMs;
            int frames = Math.Max(1, (int)Math.Ceiling((phrase.notes[^1].endMs - phrase.notes[0].positionMs + 2 * NeutrinoScore.PaddingMs) / 10));
            var offsets = new float[frames];
            int noteIndex = 0;
            for (int f = 0; f < frames; f++) {
                double ms = startMs + f * 10;
                if (ms < phrase.notes[0].positionMs || ms >= phrase.notes[^1].endMs) { continue; }
                while (noteIndex < phrase.notes.Length - 1 && ms >= phrase.notes[noteIndex].endMs) { noteIndex++; }
                var note = phrase.notes[noteIndex];
                if (ms < note.positionMs || NeutrinoScore.IsRestLyric(note.lyric) || NeutrinoScore.IsBreathLyric(note.lyric)) { continue; }
                double index = (phrase.timeAxis.MsPosToTickPos(ms) - phrase.position + phrase.leading) / 5d;
                index = Math.Clamp(index, 0, phrase.pitches.Length - 1);
                int left = (int)Math.Floor(index), right = Math.Min(left + 1, phrase.pitches.Length - 1);
                double cents = phrase.pitches[left] + (phrase.pitches[right] - phrase.pitches[left]) * (index - left);
                offsets[f] = (float)(cents - note.tone * 100);
            }
            return offsets;
        }
    }
}

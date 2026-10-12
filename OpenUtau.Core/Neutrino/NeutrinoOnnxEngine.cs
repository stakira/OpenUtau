using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace OpenUtau.Core.Neutrino {
    // Direct CPU inference for the Tau 3.2.x ONNX model interface.
    // The model files remain original; no NEUTRINO process is started.
    internal sealed class NeutrinoOnnxEngine : IDisposable {
        static readonly string[] vocabulary = "a b br by ch cl d dy e f g gy h hy i j k ky m my n N ny o p pau py r ry s sh sil t ts ty u v w y z".Split(' ');
        static readonly Dictionary<string, long> phoneIds = vocabulary.Select((p, i) => (p, id: (long)i + 1)).ToDictionary(x => x.p, x => x.id);
        static readonly Regex phonePattern = new Regex(@"\^[^^]*-([^+]+)\+", RegexOptions.Compiled);
        static readonly Regex pitchPattern = new Regex(@"/E:([A-G])([#b]?)(-?\d+)\]", RegexOptions.Compiled);
        static readonly Regex numberPattern = new Regex(@"~[^~]*-(\d+)!(\d+)\[", RegexOptions.Compiled);
        readonly InferenceSession timing, pitch, spectral, vocoder;
        internal record Phone(string Symbol, long Id, float Hz, float NoteDuration, long InNote, double Start, double End);

        public NeutrinoOnnxEngine(string modelPath) {
            using var options = new SessionOptions { IntraOpNumThreads = Math.Max(1, Math.Min(4, Environment.ProcessorCount)), InterOpNumThreads = 1 };
            var sessions = new List<InferenceSession>();
            try {
                timing = Load("t.bin"); pitch = Load("p.bin"); spectral = Load("s.bin"); vocoder = Load("v.bin");
                if (!timing.InputNames.SequenceEqual(new[] { "electron", "muon", "tau", "selectron" }) ||
                    !vocoder.InputMetadata.ContainsKey("input")) { throw new InvalidDataException("Unsupported NEUTRINO ONNX interface. This backend requires Tau 3.2.x models."); }
            } catch { foreach (var session in sessions) { session.Dispose(); } throw; }
            InferenceSession Load(string name) { var session = new InferenceSession(Path.Combine(modelPath, name), options); sessions.Add(session); return session; }
        }

        internal static Phone ParseLabel(string line) {
            var fields = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length != 3) { throw new InvalidDataException("Invalid NEUTRINO full-context label."); }
            double start = long.Parse(fields[0], CultureInfo.InvariantCulture) / 10000000d;
            double end = long.Parse(fields[1], CultureInfo.InvariantCulture) / 10000000d;
            var phone = phonePattern.Match(fields[2]);
            if (!phone.Success || !phoneIds.TryGetValue(phone.Groups[1].Value, out long id)) { throw new InvalidDataException($"Unsupported NEUTRINO phoneme: {fields[2]}"); }
            string symbol = phone.Groups[1].Value;
            float hz = 0;
            var tone = pitchPattern.Match(fields[2]);
            if (tone.Success) {
                int pc = tone.Groups[1].Value[0] switch { 'C' => 0, 'D' => 2, 'E' => 4, 'F' => 5, 'G' => 7, 'A' => 9, 'B' => 11, _ => throw new InvalidDataException() };
                pc += tone.Groups[2].Value == "#" ? 1 : tone.Groups[2].Value == "b" ? -1 : 0;
                int midi = 12 * (int.Parse(tone.Groups[3].Value, CultureInfo.InvariantCulture) + 1) + pc;
                hz = (float)(440 * Math.Pow(2, (midi - 69) / 12d));
            }
            var number = numberPattern.Match(fields[2]);
            long inNote = number.Success ? long.Parse(number.Groups[1].Value, CultureInfo.InvariantCulture) - 1 : 0;
            if (inNote < 0 || inNote >= 8) { throw new InvalidDataException("Unsupported NEUTRINO phoneme position in syllable."); }
            if (symbol != "pau" && symbol != "sil" && symbol != "br" && hz <= 0) { throw new InvalidDataException("NEUTRINO label has no valid score pitch."); }
            return new Phone(symbol, id, hz, (float)(end - start), inNote, start, end);
        }

        public float[] Synthesize(string fullLabels, double durationMs, CancellationToken token)
            => Synthesize(File.ReadLines(fullLabels).Where(l => !string.IsNullOrWhiteSpace(l)).Select(ParseLabel).ToArray(), durationMs, token);

        internal static Phone CreatePhone(string symbol, int tone, long inNote, double start, double end) {
            if (!phoneIds.TryGetValue(symbol, out long id)) { throw new InvalidDataException($"Unsupported NEUTRINO phoneme: {symbol}"); }
            return new Phone(symbol, id, symbol is "pau" or "br" ? 0 : (float)(440 * Math.Pow(2, (tone - 69) / 12d)), (float)(end - start), inNote, start, end);
        }

        internal float[] Synthesize(Phone[] phones, double durationMs, CancellationToken token) {
            int totalFrames = Math.Max(1, (int)Math.Ceiling(durationMs / 10));
            float[] packed = new float[checked(totalFrames * 101)];
            for (int f = 0; f < totalFrames; f++) { for (int m = 0; m < 100; m++) { packed[f * 101 + m] = -7; } }
            int position = 0;
            while (position < phones.Length) {
                token.ThrowIfCancellationRequested();
                if (IsRest(phones[position])) { position++; continue; }
                int end = position + 1;
                while (end < phones.Length && !IsRest(phones[end])) { end++; }
                RenderGroup(phones[position..end], packed, totalFrames, token);
                position = end;
            }
            var wave = Run(vocoder, new[] { F("input", packed, 1, totalFrames, 101) }, token);
            if (wave.Length != totalFrames * 480 || wave.Any(x => !float.IsFinite(x))) { throw new InvalidDataException("NEUTRINO vocoder returned invalid audio."); }
            return wave;
        }

        static bool IsRest(Phone phone) => phone.Symbol is "pau" or "sil";

        void RenderGroup(Phone[] phones, float[] packed, int totalFrames, CancellationToken token) {
            int n = phones.Length;
            long[] ids = phones.Select(p => p.Id).ToArray(), numbers = phones.Select(p => p.InNote).ToArray();
            float[] hz = phones.Select(p => p.Hz).ToArray(), noteDurations = phones.Select(p => p.NoteDuration).ToArray();
            var shifts = Run(timing, new[] { L("electron", ids, 1, n), F("muon", hz, 1, n), F("tau", noteDurations, 1, n), L("selectron", numbers, 1, n) }, token);
            if (shifts.Length != n + 1 || shifts.Any(x => !float.IsFinite(x))) { throw new InvalidDataException("NEUTRINO timing model returned invalid boundaries."); }
            double[] boundaries = new double[n + 1];
            double end = phones[^1].End;
            for (int i = 0; i < n; i++) {
                double predicted = Math.Round((phones[i].Start + shifts[i]) * 1000) / 1000;
                double lower = i == 0 ? 0 : boundaries[i - 1] + .001;
                boundaries[i] = Math.Clamp(predicted, lower, Math.Max(lower, end - .001 * (n - i)));
            }
            boundaries[n] = Math.Max(end, boundaries[n - 1] + .001);
            int firstFrame = Math.Max(0, (int)Math.Round(boundaries[0] * 100));
            int lastFrame = Math.Min(totalFrames, (int)Math.Round(boundaries[n] * 100));
            int frames = lastFrame - firstFrame;
            if (frames <= 0) { return; }
            var durations = Enumerable.Range(0, n).Select(i => (float)(boundaries[i + 1] - boundaries[i])).ToArray();
            long[] mapping = new long[frames];
            int phoneIndex = 0;
            for (int f = 0; f < frames; f++) {
                while (phoneIndex < n - 1 && firstFrame + f >= (int)Math.Round(boundaries[phoneIndex + 1] * 100)) { phoneIndex++; }
                mapping[f] = phoneIndex + 1;
            }
            var features = new List<NamedOnnxValue> {
                L("electron", ids, 1, n), F("muon", durations, 1, n), F("tau", hz, 1, n),
                F("selectron", noteDurations, 1, n), L("smuon", numbers, 1, n), L("stau", mapping, 1, frames),
            };
            var f0 = Run(pitch, features, token);
            if (f0.Length != frames || f0.Any(x => !float.IsFinite(x) || x < 0)) { throw new InvalidDataException("NEUTRINO pitch model returned invalid F0."); }
            features.Add(F("photon", f0, 1, frames));
            var mel = Run(spectral, features, token);
            if (mel.Length != frames * 100 || mel.Any(x => !float.IsFinite(x))) { throw new InvalidDataException("NEUTRINO spectral model returned invalid mel features."); }
            CopyVocoderFeatures(packed, firstFrame, mel, f0);
        }

        // v.bin slices input[..., 0:100] as mel and input[..., -1:] as F0.
        internal static void CopyVocoderFeatures(float[] packed, int firstFrame, float[] mel, float[] f0) {
            if (mel.Length != f0.Length * 100 || firstFrame < 0 || (long)(firstFrame + f0.Length) * 101 > packed.Length) {
                throw new ArgumentException("Invalid NEUTRINO vocoder feature dimensions.");
            }
            for (int f = 0; f < f0.Length; f++) {
                Array.Copy(mel, f * 100, packed, (firstFrame + f) * 101, 100);
                packed[(firstFrame + f) * 101 + 100] = f0[f];
            }
        }

        static NamedOnnxValue F(string name, float[] values, params int[] shape) => NamedOnnxValue.CreateFromTensor(name, new DenseTensor<float>(values, shape));
        static NamedOnnxValue L(string name, long[] values, params int[] shape) => NamedOnnxValue.CreateFromTensor(name, new DenseTensor<long>(values, shape));
        static float[] Run(InferenceSession session, IEnumerable<NamedOnnxValue> inputs, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            using var options = new RunOptions();
            using var registration = token.Register(() => options.Terminate = true);
            try { using var output = session.Run(inputs.ToArray(), session.OutputNames, options); return output.First().AsTensor<float>().ToArray(); }
            catch (OnnxRuntimeException) when (token.IsCancellationRequested) { throw new OperationCanceledException(token); }
        }
        public void Dispose() { timing.Dispose(); pitch.Dispose(); spectral.Dispose(); vocoder.Dispose(); }
    }
}

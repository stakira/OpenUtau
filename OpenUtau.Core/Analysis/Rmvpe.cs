using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NWaves.Operations;
using NWaves.Signals;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Core.Analysis;

public class RmvpeResult {
    public double TimeStepSeconds { get; init; } = 0.01;
    public float[] F0 { get; init; } = Array.Empty<float>();

    const int MedianWindowRadius = 2;
    const double AdaptiveSpikeThresholdCents = 75.0;
    const double AdaptiveSpikeBlend = 0.7;
    const int MaxFilledGapSteps = 12;
    const double MaxEdgeTrimMs = 25.0;
    const double MaxEdgeTrimRatio = 0.15;

    struct PitchPoint {
        public int x;
        public int y;
    }

    struct NoteSegment {
        public double onsetMs;
        public double durationMs;
        public int midi;
        public bool rest;
    }

    static List<int> MedianFilter(IReadOnlyList<int> values) {
        var result = new List<int>(values.Count);
        for (var i = 0; i < values.Count; ++i) {
            var window = new List<int>();
            for (var j = Math.Max(0, i - MedianWindowRadius); j <= Math.Min(values.Count - 1, i + MedianWindowRadius); ++j) {
                window.Add(values[j]);
            }
            window.Sort();
            result.Add(window[window.Count / 2]);
        }
        return result;
    }

    static List<int> AdaptiveSmooth(IReadOnlyList<int> values) {
        if (values.Count <= 2) {
            return values.ToList();
        }
        var result = values.ToList();
        for (var i = 1; i < values.Count - 1; ++i) {
            var neighborAverage = (result[i - 1] + result[i + 1]) / 2.0;
            var delta = result[i] - neighborAverage;
            if (Math.Abs(delta) <= AdaptiveSpikeThresholdCents) {
                continue;
            }
            result[i] = (int)Math.Round(result[i] - delta * AdaptiveSpikeBlend);
        }
        return result;
    }

    static List<PitchPoint> FillShortGapsAndEdges(List<PitchPoint> points, int noteStartX, int noteEndX) {
        if (points.Count == 0) {
            return points;
        }
        var expanded = new List<PitchPoint>();
        expanded.Add(points[0]);
        for (var i = 1; i < points.Count; ++i) {
            var prev = expanded[^1];
            var current = points[i];
            var gapSteps = Math.Max(0, (current.x - prev.x) / UCurve.interval - 1);
            if (gapSteps > 0 && gapSteps <= MaxFilledGapSteps) {
                for (var step = 1; step <= gapSteps; ++step) {
                    var ratio = step / (double)(gapSteps + 1);
                    expanded.Add(new PitchPoint {
                        x = prev.x + step * UCurve.interval,
                        y = (int)Math.Round(prev.y + (current.y - prev.y) * ratio),
                    });
                }
            }
            expanded.Add(current);
        }
        return expanded;
    }

    static void AppendSmoothedPoints(UCurve curve, List<PitchPoint> points, int noteStartX, int noteEndX) {
        if (points.Count == 0) {
            return;
        }
        var processedPoints = FillShortGapsAndEdges(points, noteStartX, noteEndX);
        var ys = processedPoints.Select(point => point.y).ToList();
        var smoothedYs = AdaptiveSmooth(MedianFilter(ys));
        for (var i = 0; i < processedPoints.Count; ++i) {
            var point = processedPoints[i];
            if (curve.xs.Count > 0 && curve.xs[^1] == point.x) {
                curve.ys[^1] = smoothedYs[i];
            } else {
                curve.xs.Add(point.x);
                curve.ys.Add(smoothedYs[i]);
            }
        }
    }

    static List<NoteSegment> BuildSegments(UProject project, UVoicePart part) {
        return part.notes
            .OrderBy(note => note.position)
            .Select(note => {
                var onsetTick = part.position + note.position;
                var endTick = part.position + note.End;
                var onsetMs = project.timeAxis.TickPosToMsPos(onsetTick);
                var endMs = project.timeAxis.TickPosToMsPos(endTick);
                return new NoteSegment {
                    onsetMs = onsetMs,
                    durationMs = endMs - onsetMs,
                    midi = note.tone,
                    rest = false,
                };
            })
            .ToList();
    }

    public void ApplyToPart(UProject project, UVoicePart part) {
        ApplyToPart(project, part, BuildSegments(project, part));
    }

    void ApplyToPart(UProject project, UVoicePart part, IReadOnlyList<NoteSegment> notes) {
        if (F0.Length == 0 || notes.Count == 0 || !project.expressions.TryGetValue(Format.Ustx.PITD, out var descriptor)) {
            Log.Information(
                "RMVPE apply skipped. f0={F0Count} notes={NoteCount} hasPITD={HasPitd}",
                F0.Length,
                notes.Count,
                project.expressions.ContainsKey(Format.Ustx.PITD));
            return;
        }
        var curve = new UCurve(descriptor);
        var frameMs = TimeStepSeconds * 1000.0;
        var partStartMs = project.timeAxis.TickPosToMsPos(part.position);
        var pendingPoints = new List<PitchPoint>();
        var pendingNoteIndex = -1;
        int pendingNoteStartX = 0;
        int pendingNoteEndX = 0;
        int noteIndex = 0;
        for (int i = 0; i < F0.Length; ++i) {
            var hz = F0[i];
            var localTimeMs = i * frameMs;
            var absoluteTimeMs = partStartMs + localTimeMs;
            while (noteIndex + 1 < notes.Count && notes[noteIndex].onsetMs + notes[noteIndex].durationMs <= absoluteTimeMs) {
                noteIndex++;
            }
            if (noteIndex >= notes.Count) {
                break;
            }
            var note = notes[noteIndex];
            if (pendingPoints.Count > 0 && pendingNoteIndex != noteIndex) {
                AppendSmoothedPoints(curve, pendingPoints, pendingNoteStartX, pendingNoteEndX);
                pendingPoints.Clear();
                pendingNoteIndex = -1;
            }
            var isInNote = note.onsetMs <= absoluteTimeMs && absoluteTimeMs < note.onsetMs + note.durationMs;
            if (!isInNote || note.rest || hz <= 0) {
                continue;
            }
            var noteOffsetMs = absoluteTimeMs - note.onsetMs;
            var edgeTrimMs = Math.Min(MaxEdgeTrimMs, note.durationMs * MaxEdgeTrimRatio);
            if (note.durationMs > edgeTrimMs * 2 &&
                (noteOffsetMs < edgeTrimMs || note.durationMs - noteOffsetMs <= edgeTrimMs)) {
                continue;
            }
            var tick = project.timeAxis.MsPosToTickPos(absoluteTimeMs);
            var x = tick - part.position;
            var midi = 69.0 + 12.0 * Math.Log2(hz / 440.0);
            var y = (int)Math.Round(Math.Clamp((midi - note.midi) * 100.0, descriptor.min, descriptor.max));
            var snappedX = (int)Math.Round((double)x / UCurve.interval) * UCurve.interval;
            pendingNoteStartX = (int)Math.Round((double)(project.timeAxis.MsPosToTickPos(note.onsetMs) - part.position) / UCurve.interval) * UCurve.interval;
            pendingNoteEndX = (int)Math.Round((double)(project.timeAxis.MsPosToTickPos(note.onsetMs + note.durationMs) - part.position) / UCurve.interval) * UCurve.interval;
            pendingNoteIndex = noteIndex;
            if (pendingPoints.Count > 0 && pendingPoints[^1].x == snappedX) {
                pendingPoints[^1] = new PitchPoint { x = snappedX, y = y };
            } else {
                pendingPoints.Add(new PitchPoint { x = snappedX, y = y });
            }
        }
        AppendSmoothedPoints(curve, pendingPoints, pendingNoteStartX, pendingNoteEndX);
        curve.Simplify();
        if (curve.xs.Count > 0) {
            part.curves.RemoveAll(c => c.abbr == Format.Ustx.PITD);
            part.curves.Add(curve);
        }
        Log.Information("RMVPE applied pitch curve. points={PointCount}", curve.xs.Count);
    }
}

public class RmvpeTranscriber : IDisposable {
    const int SampleRate = 16000;
    const int HopLength = 160;
    const int WindowLength = 1024;
    const int MelBins = 128;
    const int FftBins = WindowLength / 2 + 1;
    const int PitchBins = 360;
    const double MelFMin = 30;
    const double MelFMax = 8000;
    const double Threshold = 0.03;
    const double CentBase = 1997.3794084376191;

    readonly InferenceSession session;
    readonly string inputName;
    readonly string outputName;
    readonly double[] hannWindow;
    readonly float[][] melBasis;
    readonly string modelPath;
    bool disposed;

    public RmvpeTranscriber() {
        modelPath = ResolveModelPath();
        if (!File.Exists(modelPath)) {
            throw new MessageCustomizableException(
                "RMVPE not found",
                "<translate:errors.failed.transcribe.rmvpe>",
                new FileNotFoundException(modelPath),
                false,
                new[] { modelPath });
        }
        Log.Information("RMVPE loading model from {ModelPath}", modelPath);
        session = Onnx.getInferenceSession(modelPath, force_cpu: true);
        inputName = session.InputNames.First();
        outputName = session.OutputNames.First();
        hannWindow = BuildHannWindow(WindowLength);
        melBasis = BuildMelBasis();
    }

    static string ResolveModelPath() {
        var candidates = new List<string> {
            Path.Combine(PathManager.Inst.DependencyPath, "rmvpe", "rmvpe.onnx"),
            Path.Combine(PathManager.Inst.DependencyPath, "RMVPE", "rmvpe.onnx"),
            Path.Combine(PathManager.Inst.DependencyPath, "DiffSinger", "rmvpe.onnx"),
        };
        var current = new DirectoryInfo(PathManager.Inst.RootPath);
        for (var i = 0; i < 6 && current != null; ++i, current = current.Parent) {
            candidates.Add(Path.Combine(current.FullName, "RMVPE", "rmvpe.onnx"));
        }
        return candidates.FirstOrDefault(File.Exists)
            ?? Path.Combine(PathManager.Inst.DependencyPath, "rmvpe", "rmvpe.onnx");
    }

    public RmvpeResult Infer(UWavePart wavePart) {
        var mono = wavePart.channels == 1
            ? wavePart.Samples
            : Enumerable.Range(0, wavePart.Samples.Length / wavePart.channels)
                .Select(i => wavePart.Samples.Skip(i * wavePart.channels).Take(wavePart.channels).Average())
                .ToArray();
        var signal = new DiscreteSignal(wavePart.sampleRate, mono);
        if (signal.SamplingRate != SampleRate) {
            var resampler = new Resampler();
            signal = resampler.Resample(signal, SampleRate);
        }
        var mel = BuildLogMel(signal.Samples);
        var originalFrames = mel.GetLength(2);
        var paddedFrames = ((originalFrames - 1) / 32 + 1) * 32;
        var input = new DenseTensor<float>(new[] { 1, MelBins, paddedFrames });
        for (int m = 0; m < MelBins; ++m) {
            for (int t = 0; t < originalFrames; ++t) {
                input[0, m, t] = mel[0, m, t];
            }
        }
        using var outputs = session.Run(new[] { NamedOnnxValue.CreateFromTensor(inputName, input) });
        var hiddenTensor = outputs.First(output => output.Name == outputName).AsTensor<float>();
        var dims = hiddenTensor.Dimensions.ToArray();
        if (dims.Length != 3 || dims[2] != PitchBins) {
            throw new InvalidDataException($"Unexpected RMVPE output shape: [{string.Join(", ", dims)}]");
        }
        var hidden = hiddenTensor.ToArray();
        var f0 = DecodeLocalAverage(hidden, dims[1]).Take(originalFrames).ToArray();
        return new RmvpeResult {
            TimeStepSeconds = 0.01,
            F0 = f0,
        };
    }

    static float[] DecodeLocalAverage(float[] hidden, int frames) {
        var result = new float[frames];
        for (int t = 0; t < frames; ++t) {
            var baseIndex = t * PitchBins;
            var bestIndex = 0;
            var bestValue = float.MinValue;
            for (int i = 0; i < PitchBins; ++i) {
                var value = hidden[baseIndex + i];
                if (value > bestValue) {
                    bestValue = value;
                    bestIndex = i;
                }
            }
            if (bestValue < Threshold) {
                result[t] = 0;
                continue;
            }
            var start = Math.Max(0, bestIndex - 4);
            var end = Math.Min(PitchBins, bestIndex + 5);
            double productSum = 0;
            double weightSum = 0;
            for (int i = start; i < end; ++i) {
                var weight = hidden[baseIndex + i];
                productSum += weight * (i * 20.0 + CentBase);
                weightSum += weight;
            }
            if (weightSum <= 0) {
                result[t] = 0;
                continue;
            }
            var cents = productSum / weightSum;
            result[t] = (float)(10.0 * Math.Pow(2.0, cents / 1200.0));
        }
        return result;
    }

    float[,,] BuildLogMel(float[] samples) {
        var padded = new float[samples.Length + WindowLength];
        Array.Copy(samples, 0, padded, WindowLength / 2, samples.Length);
        var frames = Math.Max(1, samples.Length / HopLength + 1);
        var mel = new float[1, MelBins, frames];
        var frame = new Complex[WindowLength];
        var spectrum = new double[FftBins];

        for (int t = 0; t < frames; ++t) {
            var start = t * HopLength;
            for (int i = 0; i < WindowLength; ++i) {
                var sample = padded[start + i] * hannWindow[i];
                frame[i] = new Complex(sample, 0);
            }
            Fft(frame);
            for (int i = 0; i < FftBins; ++i) {
                spectrum[i] = frame[i].Magnitude;
            }
            for (int m = 0; m < MelBins; ++m) {
                double sum = 0;
                for (int k = 0; k < FftBins; ++k) {
                    sum += melBasis[m][k] * spectrum[k];
                }
                mel[0, m, t] = (float)Math.Log(Math.Max(1e-5, sum));
            }
        }
        return mel;
    }

    static void Fft(Complex[] buffer) {
        var n = buffer.Length;
        for (int i = 1, j = 0; i < n; ++i) {
            var bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1) {
                j &= ~bit;
            }
            j |= bit;
            if (i < j) {
                (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
            }
        }
        for (int len = 2; len <= n; len <<= 1) {
            var angle = -2 * Math.PI / len;
            var wLen = new Complex(Math.Cos(angle), Math.Sin(angle));
            for (int i = 0; i < n; i += len) {
                var w = Complex.One;
                for (int j = 0; j < len / 2; ++j) {
                    var u = buffer[i + j];
                    var v = buffer[i + j + len / 2] * w;
                    buffer[i + j] = u + v;
                    buffer[i + j + len / 2] = u - v;
                    w *= wLen;
                }
            }
        }
    }

    static double[] BuildHannWindow(int size) {
        var window = new double[size];
        for (int i = 0; i < size; ++i) {
            window[i] = 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / size);
        }
        return window;
    }

    static float[][] BuildMelBasis() {
        var basis = new float[MelBins][];
        var fftFreqs = Enumerable.Range(0, FftBins)
            .Select(i => (double)i * SampleRate / WindowLength)
            .ToArray();
        var melPoints = Linspace(HzToMel(MelFMin), HzToMel(MelFMax), MelBins + 2)
            .Select(MelToHz)
            .ToArray();
        for (int m = 0; m < MelBins; ++m) {
            basis[m] = new float[FftBins];
            var lower = melPoints[m];
            var center = melPoints[m + 1];
            var upper = melPoints[m + 2];
            var enorm = 2.0 / Math.Max(1e-12, upper - lower);
            for (int k = 0; k < FftBins; ++k) {
                var freq = fftFreqs[k];
                var lowerSlope = (freq - lower) / Math.Max(1e-12, center - lower);
                var upperSlope = (upper - freq) / Math.Max(1e-12, upper - center);
                basis[m][k] = (float)(Math.Max(0, Math.Min(lowerSlope, upperSlope)) * enorm);
            }
        }
        return basis;
    }

    static double[] Linspace(double start, double end, int count) {
        if (count <= 1) {
            return new[] { start };
        }
        var result = new double[count];
        var step = (end - start) / (count - 1);
        for (int i = 0; i < count; ++i) {
            result[i] = start + step * i;
        }
        return result;
    }

    static double HzToMel(double hz) => 2595.0 * Math.Log10(1.0 + hz / 700.0);
    static double MelToHz(double mel) => 700.0 * (Math.Pow(10.0, mel / 2595.0) - 1.0);

    void Dispose(bool disposing) {
        if (!disposed) {
            if (disposing) {
                session.Dispose();
            }
            disposed = true;
        }
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

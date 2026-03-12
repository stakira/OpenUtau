using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
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
    const float Threshold = 0.03f;

    readonly InferenceSession session;
    readonly string waveformInputName;
    readonly string thresholdInputName;
    readonly string f0OutputName;
    readonly string uvOutputName;
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
        waveformInputName = ResolveWaveformInputName(session);
        thresholdInputName = ResolveThresholdInputName(session);
        f0OutputName = ResolveF0OutputName(session);
        uvOutputName = ResolveUvOutputName(session);
        Log.Information(
            "RMVPE session ready. inputWaveform={WaveformInput} inputThreshold={ThresholdInput} outputF0={F0Output} outputUv={UvOutput}",
            waveformInputName,
            thresholdInputName,
            f0OutputName,
            uvOutputName);
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
        var mono = ToMono(wavePart);
        var resampled = ResampleTo16k(mono, wavePart.sampleRate);
        var waveform = new DenseTensor<float>(new[] { 1, resampled.Length });
        for (int i = 0; i < resampled.Length; ++i) {
            waveform[0, i] = Math.Clamp(resampled[i], -1f, 1f);
        }
        var threshold = new DenseTensor<float>(new[] { Threshold }, Array.Empty<int>());
        using var outputs = session.Run(new[] {
            NamedOnnxValue.CreateFromTensor(waveformInputName, waveform),
            NamedOnnxValue.CreateFromTensor(thresholdInputName, threshold),
        });
        var f0Tensor = outputs.First(output => output.Name == f0OutputName).AsTensor<float>();
        var uvTensor = outputs.First(output => output.Name == uvOutputName).AsTensor<bool>();
        var f0 = f0Tensor.ToArray();
        var uv = uvTensor.ToArray();
        if (f0.Length != uv.Length) {
            throw new InvalidDataException($"Unexpected RMVPE output sizes: f0={f0.Length}, uv={uv.Length}");
        }
        for (int i = 0; i < f0.Length; ++i) {
            if (uv[i]) {
                f0[i] = 0f;
            }
        }
        return new RmvpeResult {
            TimeStepSeconds = (double)HopLength / SampleRate,
            F0 = f0,
        };
    }

    static string ResolveWaveformInputName(InferenceSession session) {
        return session.InputNames.FirstOrDefault(name =>
                string.Equals(name, "waveform", StringComparison.OrdinalIgnoreCase))
            ?? session.InputNames.First();
    }

    static string ResolveThresholdInputName(InferenceSession session) {
        return session.InputNames.FirstOrDefault(name =>
                string.Equals(name, "threshold", StringComparison.OrdinalIgnoreCase))
            ?? session.InputNames.ElementAtOrDefault(1)
            ?? throw new InvalidDataException("RMVPE model must expose a threshold input.");
    }

    static string ResolveF0OutputName(InferenceSession session) {
        return session.OutputNames.FirstOrDefault(name =>
                string.Equals(name, "f0", StringComparison.OrdinalIgnoreCase))
            ?? session.OutputNames.First();
    }

    static string ResolveUvOutputName(InferenceSession session) {
        return session.OutputNames.FirstOrDefault(name =>
                string.Equals(name, "uv", StringComparison.OrdinalIgnoreCase))
            ?? session.OutputNames.ElementAtOrDefault(1)
            ?? throw new InvalidDataException("RMVPE model must expose a uv output.");
    }

    static float[] ToMono(UWavePart wavePart) {
        if (wavePart.channels == 1) {
            return wavePart.Samples;
        }
        return Enumerable.Range(0, wavePart.Samples.Length / wavePart.channels)
            .Select(i => wavePart.Samples.Skip(i * wavePart.channels).Take(wavePart.channels).Average())
            .ToArray();
    }

    static float[] ResampleTo16k(float[] samples, int sourceSampleRate) {
        if (sourceSampleRate == SampleRate) {
            return samples;
        }
        var targetLength = Math.Max(1, (int)Math.Round(samples.Length * (double)SampleRate / sourceSampleRate));
        var result = new float[targetLength];
        var scale = (double)sourceSampleRate / SampleRate;
        for (int i = 0; i < targetLength; ++i) {
            var sourcePos = i * scale;
            var left = Math.Clamp((int)Math.Floor(sourcePos), 0, samples.Length - 1);
            var right = Math.Min(left + 1, samples.Length - 1);
            var frac = sourcePos - left;
            result[i] = (float)(samples[left] * (1.0 - frac) + samples[right] * frac);
        }
        return result;
    }

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

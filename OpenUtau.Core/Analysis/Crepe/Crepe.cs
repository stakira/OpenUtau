using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NWaves.Operations;
using NWaves.Signals;

namespace OpenUtau.Core.Analysis.Crepe {
    public class Crepe : IDisposable {
        const int kModelSampleRate = 16000;
        const int kFrameSize = 1024;
        const int kActivationSize = 360;

        InferenceSession session;
        double[] centsMapping;
        private bool disposedValue;

        public Crepe() {
            session = new InferenceSession(Resources.tiny);
            centsMapping = Enumerable.Range(0, kActivationSize)
                .Select(i => i * 20 + 1997.3794084376191)
                .ToArray();
        }

        public double[] ComputeF0(DiscreteSignal signal, double stepMs, double threshold = 0.21) {
            if (signal.SamplingRate != kModelSampleRate) {
                var resampler = new Resampler();
                signal = resampler.Resample(signal, kModelSampleRate);
            }
            var input = ToFrames(signal, stepMs);
            int length = input.Dimensions[0];
            var inputs = new List<NamedOnnxValue>();
            inputs.Add(NamedOnnxValue.CreateFromTensor("input", input));
            var outputs = session.Run(inputs);
            var activations = outputs.First().AsTensor<float>().ToArray();
            int[] path = new int[length];
            GetPath(activations, path);
            float[] confidences = new float[length];
            double[] cents = new double[length];
            double[] f0 = new double[length];
            for (int i = 0; i < length; ++i) {
                var frame = new ArraySegment<float>(activations, i * kActivationSize, kActivationSize);
                cents[i] = GetCents(frame, path[i]);
                confidences[i] = frame[path[i]];
                f0[i] = double.IsNormal(cents[i])
                    && double.IsNormal(confidences[i])
                    && confidences[i] > threshold
                    ? 10f * Math.Pow(2.0, cents[i] / 1200.0) : 0;
            }
            return f0;
        }

        Tensor<float> ToFrames(DiscreteSignal signal, double stepMs) {
            float[] paddedSamples = new float[signal.Length + kFrameSize];
            Array.Copy(signal.Samples, 0, paddedSamples, kFrameSize / 2, signal.Length);
            int hopSize = (int)(kModelSampleRate * stepMs / 1000);
            int length = signal.Length / hopSize;
            float[] frames = new float[length * kFrameSize];
            for (int i = 0; i < length; ++i) {
                Array.Copy(paddedSamples, i * hopSize,
                    frames, i * kFrameSize, kFrameSize);
                NormalizeFrame(new ArraySegment<float>(
                    frames, i * kFrameSize, kFrameSize));
            }
            return frames.ToTensor().Reshape(new int[] { length, kFrameSize });
        }

        void GetPath(float[] activations, int[] path) {
            float[] prob = new float[kActivationSize];
            float[] nextProb = new float[kActivationSize];
            for (int i = 0; i < kActivationSize; ++i) {
                prob[i] = (float)Math.Log(1.0 / kActivationSize);
            }
            float[,] transitions = new float[kActivationSize, kActivationSize];
            int dist = 12;
            for (int i = 0; i < kActivationSize; ++i) {
                int low = Math.Max(0, i - dist);
                int high = Math.Min(kActivationSize, i + dist);
                float sum = 0;
                for (int j = low; j < high; ++j) {
                    transitions[i, j] = dist - Math.Abs(i - j);
                    sum += transitions[i, j];
                }
                for (int j = low; j < high; ++j) {
                    transitions[i, j] = (float)Math.Log(transitions[i, j] / sum);
                }
            }
            for (int i = 0; i < path.Length; ++i) {
                var activ = new ArraySegment<float>(activations, i * kActivationSize, kActivationSize);
                Array.Clear(nextProb, 0, nextProb.Length);
                for (int j = 0; j < kActivationSize; ++j) {
                    int low = Math.Max(0, j - dist);
                    int high = Math.Min(kActivationSize, j + dist);
                    float maxP = float.MinValue;
                    for (int k = low; k < high; ++k) {
                        float p = (float)(prob[k] + transitions[j, k] + Math.Log(activ[k]));
                        if (p > maxP) {
                            maxP = p;
                        }
                    }
                    nextProb[j] = maxP;
                }
                path[i] = ArgMax(nextProb);
            }
        }

        double GetCents(ArraySegment<float> activations, int index) {
            int start = Math.Max(0, index - 4);
            int end = Math.Min(activations.Count, index + 5);
            double weightedSum = 0;
            double weightSum = 0;
            for (int i = start; i < end; ++i) {
                weightedSum += activations[i] * centsMapping[i];
                weightSum += activations[i];
            }
            return weightedSum / weightSum;
        }

        static int ArgMax(Span<float> values) {
            int index = -1;
            float value = float.MinValue;
            for (int i = 0; i < values.Length; ++i) {
                if (value < values[i]) {
                    index = i;
                    value = values[i];
                }
            }
            return index;
        }

        void NormalizeFrame(ArraySegment<float> data) {
            double avg = data.Average();
            double std = Math.Sqrt(data.Average(d => Math.Pow(d - avg, 2)));
            for (int i = 0; i < data.Count; ++i) {
                data[i] = (float)((data[i] - avg) / std);
            }
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    session.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

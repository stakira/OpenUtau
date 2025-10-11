using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;

namespace OpenUtau.Core.Render {
    public static class Renderers {
        public const string CLASSIC = "CLASSIC";
        public const string WORLDLINER = "WORLDLINE-R";
        public const string ENUNU = "ENUNU";
        public const string VOGEN = "VOGEN";
        public const string DIFFSINGER = "DIFFSINGER";
        public const string VOICEVOX = "VOICEVOX";

        static readonly string[] classicRenderers = new[] { WORLDLINER, CLASSIC };
        static readonly string[] enunuRenderers = new[] { ENUNU };
        static readonly string[] vogenRenderers = new[] { VOGEN };
        static readonly string[] diffSingerRenderers = new[] { DIFFSINGER };
        static readonly string[] voicevoxRenderers = new[] { VOICEVOX };
        static readonly string[] noRenderers = new string[0];

        public static string[] GetSupportedRenderers(USingerType singerType) {
            switch (singerType) {
                case USingerType.Classic:
                    return classicRenderers;
                case USingerType.Enunu:
                    return enunuRenderers;
                case USingerType.Vogen:
                    return vogenRenderers;
                case USingerType.DiffSinger:
                    return diffSingerRenderers;
                case USingerType.Voicevox:
                    return voicevoxRenderers;
                default:
                    return noRenderers;
            }
        }

        public static List<string> getRendererOptions() {
            return new List<string> {
                "WORLDLINE-R",
                "Classic"
            };
        }

        public static string GetDefaultRenderer(USingerType singerType) {
            if (Preferences.Default.DefaultRenderer == "Classic" && singerType == USingerType.Classic) {
                return CLASSIC;
            } else {
                return GetSupportedRenderers(singerType)[0];
            }
        }

        public static IRenderer CreateRenderer(string renderer) {
            if (renderer == CLASSIC) {
                return new ClassicRenderer();
            } else if (renderer?.StartsWith(WORLDLINER.Substring(0, 9)) ?? false) {
                return new WorldlineRenderer();
            } else if (renderer == ENUNU) {
                return new Enunu.EnunuRenderer();
            } else if (renderer == VOGEN) {
                return new Vogen.VogenRenderer();
            } else if (renderer == DIFFSINGER) {
                return new DiffSinger.DiffSingerRenderer();
            } else if (renderer == VOICEVOX) {
                return new Voicevox.VoicevoxRenderer();
            }
            return null;
        }

        readonly static ConcurrentDictionary<string, object> cacheLockMap
            = new ConcurrentDictionary<string, object>();

        public static object GetCacheLock(string key) {
            return cacheLockMap.GetOrAdd(key, _ => new object());
        }

        private const int EDGE_SAMPLES_COUNT = 100;
        private const double EXPONENTIAL_CURVE_FACTOR = -5.0;
        private const int SAMPLE_RATE = 44100;

        public static void ApplyPostProcessing(RenderResult result) {
            if (result.samples == null || result.samples.Length == 0) {
                return;
            }

            // Remove DC offset first to eliminate root cause of clicks
            if (Preferences.Default.RemoveDCOffset) {
                RemoveDCOffset(result.samples);
            }

            // Apply fade to prevent noise at phrase boundaries
            if (Preferences.Default.ApplyPhraseFade) {
                ApplyFades(result.samples, Preferences.Default.PhraseFadeMs);
            }
        }

        private static void RemoveDCOffset(float[] samples) {
            if (samples == null || samples.Length == 0) return;

            // Use first and last N samples to estimate DC offset at boundaries
            int edgeSamples = Math.Min(EDGE_SAMPLES_COUNT, samples.Length / 4);

            // Calculate mean of edge samples (first + last)
            double edgeSum = 0.0;
            int edgeCount = 0;

            // First edge
            for (int i = 0; i < edgeSamples && i < samples.Length; i++) {
                edgeSum += samples[i];
                edgeCount++;
            }

            // Last edge (avoid double-counting if arrays overlap)
            int startOfLastEdge = Math.Max(edgeSamples, samples.Length - edgeSamples);
            for (int i = startOfLastEdge; i < samples.Length; i++) {
                edgeSum += samples[i];
                edgeCount++;
            }

            float dcOffset = (float)(edgeSum / edgeCount);

            // Remove DC offset
            for (int i = 0; i < samples.Length; i++) {
                samples[i] -= dcOffset;
            }
        }

        private static void ApplyFades(float[] samples, double fadeMs) {
            // Validate input
            if (fadeMs < 0) return;
            if (fadeMs > 50) fadeMs = 50; // Clamp to max allowed value

            int fadeSamples = (int)(SAMPLE_RATE * fadeMs / 1000.0);
            fadeSamples = Math.Min(fadeSamples, samples.Length / 2);

            if (fadeSamples <= 0) return;

            string curve = Preferences.Default.PhraseFadeCurve;

            // Apply fade-in
            for (int i = 0; i < fadeSamples; i++) {
                double fadeRatio = (double)i / fadeSamples;
                double fadeGain = GetFadeGain(fadeRatio, curve);
                samples[i] *= (float)fadeGain;
            }

            // Apply fade-out
            for (int i = 0; i < fadeSamples; i++) {
                int sampleIndex = samples.Length - 1 - i;
                double fadeRatio = (double)i / fadeSamples;
                double fadeGain = GetFadeGain(fadeRatio, curve);
                samples[sampleIndex] *= (float)fadeGain;
            }
        }

        private static double GetFadeGain(double ratio, string curve) {
            if (string.Equals(curve, "linear", StringComparison.OrdinalIgnoreCase)) {
                return ratio;
            } else if (string.Equals(curve, "exponential", StringComparison.OrdinalIgnoreCase)) {
                // Exponential curve: starts slow, accelerates
                // Normalize to ensure it reaches exactly 1.0 at ratio=1.0
                return (1.0 - Math.Exp(EXPONENTIAL_CURVE_FACTOR * ratio)) / (1.0 - Math.Exp(EXPONENTIAL_CURVE_FACTOR));
            } else if (string.Equals(curve, "sine", StringComparison.OrdinalIgnoreCase)) {
                // Quarter sine wave
                return Math.Sin(ratio * Math.PI / 2.0);
            } else if (string.Equals(curve, "equal-power", StringComparison.OrdinalIgnoreCase)) {
                // Squared sine for equal-power crossfade
                double sineValue = Math.Sin(ratio * Math.PI / 2.0);
                return sineValue * sineValue;
            } else {
                // Raised cosine (Hann window)
                return 0.5 * (1.0 - Math.Cos(Math.PI * ratio));
            }
        }

        public static void ApplyDynamics(RenderPhrase phrase, RenderResult result) {
            const int interval = 5;
            if (phrase.dynamics == null) {
                return;
            }
            int startTick = phrase.position - phrase.leading;
            double startMs = result.positionMs - result.leadingMs;
            int startSample = 0;
            for (int i = 0; i < phrase.dynamics.Length; ++i) {
                int endTick = startTick + interval;
                double endMs = phrase.timeAxis.TickPosToMsPos(endTick);
                int endSample = Math.Min((int)((endMs - startMs) / 1000 * 44100), result.samples.Length);
                float a = phrase.dynamics[i];
                float b = (i + 1) == phrase.dynamics.Length ? phrase.dynamics[i] : phrase.dynamics[i + 1];
                for (int j = startSample; j < endSample; ++j) {
                    result.samples[j] *= a + (b - a) * (j - startSample) / (endSample - startSample);
                }
                startTick = endTick;
                startSample = endSample;
            }
        }

        public static IReadOnlyList<IResampler> GetSupportedResamplers(IWavtool? wavtool) {
            if (wavtool is SharpWavtool) {
                return ToolsManager.Inst.Resamplers;
            } else {
                return ToolsManager.Inst.Resamplers
                    .Where(r => !(r is WorldlineResampler))
                    .ToArray();
            }
        }

        public static IReadOnlyList<IWavtool> GetSupportedWavtools(IResampler? resampler) {
            return ToolsManager.Inst.Wavtools;
        }
    }
}

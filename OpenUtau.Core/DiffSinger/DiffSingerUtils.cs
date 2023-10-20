using System;
using OpenUtau.Core.Render;

namespace OpenUtau.Core.DiffSinger {
    public static class DiffSingerUtils {
        public const string VELC = "velc";
        public const string ENE = "ene";
        public const string PEXP = "pexp";
        public const string VoiceColorHeader = "cl";
        public const float headMs = 100;
        public const float tailMs = 100;

        public static double[] SampleCurve(RenderPhrase phrase, float[] curve, double defaultValue, double frameMs, int length, int headFrames, int tailFrames, Func<double, double> convert) {
            const int interval = 5;
            var result = new double[length];
            if (curve == null) {
                Array.Fill(result, defaultValue);
                return result;
            }

            for (int i = 0; i < length - headFrames - tailFrames; i++) {
                double posMs = phrase.positionMs - phrase.leadingMs + i * frameMs;
                int ticks = phrase.timeAxis.MsPosToTickPos(posMs) - (phrase.position - phrase.leading);
                int index = Math.Max(0, (int)((double)ticks / interval));
                if (index < curve.Length) {
                    result[i + headFrames] = convert(curve[index]);
                }
            }
            //Fill head and tail
            Array.Fill(result, convert(curve[0]), 0, headFrames);
            Array.Fill(result, convert(curve[^1]), length - tailFrames, tailFrames);
            return result;
        }
    }
}

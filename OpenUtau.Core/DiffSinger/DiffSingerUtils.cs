using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.ML.OnnxRuntime.Tensors;
using Newtonsoft.Json;
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


        //MusicMath.Linear, but float numbers are used instead of double
        public static float LinearF(float x0, float x1, float y0, float y1, float x) {
            const float ep = 0.001f;
            if(x1 - x0 < ep){
                return y1;
            }
            return y0 + (y1 - y0) * (x - x0) / (x1 - x0);
        }

        /// <summary>
        /// Resample a curve to a new length.
        /// Used when the hopsize of the variance model is different from the hopsize of the acoustic model.
        /// </summary>
        /// <param name="curve">The curve to resample.</param>
        /// <param name="length">The new length of the curve.</param>
        public static float[] ResampleCurve(float[] curve, int length) {
            if (curve == null || curve.Length == 0) {
                return null;
            }
            if(length == curve.Length){
                return curve;
            }
            if(length == 1){
                return new float[]{curve[0]};
            }
            float[] result = new float[length];
            for (int i = 0; i < length; i++) {
                var x = (float)i / (length - 1) * (curve.Length - 1);
                int x0 = (int)x;
                int x1 = Math.Min(x0 + 1, curve.Length - 1);
                float y0 = curve[x0];
                float y1 = curve[x1];
                result[i] = LinearF(x0, x1, y0, y1, x);
            }
            return result;
        }

        /// <summary>
        /// Validate the shape of a tensor.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tensor">Tensor to be validated</param>
        /// <param name="expectedShape">Expected shape of the tensor, -1 means the length of the axis is dynamic</param>
        /// <returns></returns>
        public static bool ValidateShape<T>(Tensor<T> tensor, int[] expectedShape){
            var shape = tensor.Dimensions;
            if(shape.Length != expectedShape.Length){
                return false;
            }
            for (int i = 0; i < shape.Length; i++) {
                if(shape[i] != expectedShape[i] && expectedShape[i] != -1){
                    return false;
                }
            }
            return true;
        }

        public static string ShapeString<T>(Tensor<T> tensor){
            var shape = tensor.Dimensions;
            return "(" + string.Join(", ", shape.ToArray()) + ")";
        }

        public static Dictionary<string, int> LoadPhonemes(string filePath){
            switch(Path.GetExtension(filePath).ToLower()){
                case ".json":
                    return LoadPhonemesFromJson(filePath);
                default:
                    return LoadPhonemesFromTxt(filePath);
            }
        }

        static Dictionary<string, int> LoadPhonemesFromJson(string filePath){
            var json = File.ReadAllText(filePath, Encoding.UTF8);
            return JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
        }

        static Dictionary<string, int> LoadPhonemesFromTxt(string filePath){
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            var result = new Dictionary<string, int>();
            for (int i = 0; i < lines.Length; i++) {
                result[lines[i]] = i;
            }
            return result;
        }

        public static Dictionary<string, int> LoadLanguageIds(string filePath){
            var json = File.ReadAllText(filePath, Encoding.UTF8);
            return JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
        }

        public static string PhonemeLanguage(string phoneme){
            if(phoneme.Contains("/")){
                return phoneme.Split("/")[0];
            }
            return "";
        }
    }
}

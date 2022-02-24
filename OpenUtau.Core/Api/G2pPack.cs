using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SharpCompress.Archives;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace OpenUtau.Api {
    public abstract class G2pPack : IG2p {
        protected Dictionary<string, int> GraphemeIndexes { get; set; }
        protected string[] Phonemes { get; set; }
        protected IG2p Dict { get; set; }
        protected InferenceSession Session { get; set; }
        protected Dictionary<string, string[]> PredCache { get; set; }

        public static string ExtractText(byte[] data, string key) {
            using (var stream = new MemoryStream(data)) {
                using var archive = ArchiveFactory.Open(stream);
                foreach (var entry in archive.Entries) {
                    if (entry.Key == key) {
                        using var entryStream = entry.OpenEntryStream();
                        using var reader = new StreamReader(entryStream, Encoding.UTF8);
                        return reader.ReadToEnd();
                    }
                }
            }
            return null;
        }

        public static byte[] ExtractBinary(byte[] data, string key) {
            using (var stream = new MemoryStream(data)) {
                using var archive = ArchiveFactory.Open(stream);
                foreach (var entry in archive.Entries) {
                    if (entry.Key == key) {
                        using var entryStream = entry.OpenEntryStream();
                        using var memStream = new MemoryStream();
                        entryStream.CopyTo(memStream);
                        return memStream.ToArray();
                    }
                }
            }
            return null;
        }

        protected (IG2p, InferenceSession) LoadPack(byte[] data) {
            string dictTxt = ExtractText(data, "dict.txt");
            string phonesTxt = ExtractText(data, "phones.txt");
            byte[] g2pData = ExtractBinary(data, "g2p.onnx");
            var builder = G2pDictionary.NewBuilder();
            phonesTxt.Split('\n')
                .Select(line => line.Trim().ToLowerInvariant())
                .Select(line => line.Split())
                .Where(parts => parts.Length == 2)
                .ToList()
                .ForEach(parts => builder.AddSymbol(parts[0], parts[1]));
            dictTxt.Split('\n')
                .Where(line => !line.StartsWith(";;;"))
                .Select(line => line.Trim().ToLowerInvariant())
                .Select(line => line.Split(new string[] { "  " }, StringSplitOptions.None))
                .Where(parts => parts.Length == 2)
                .ToList()
                .ForEach(parts => builder.AddEntry(parts[0], parts[1].Split().Select(symbol => RemoveTailDigits(symbol))));
            var dict = builder.Build();
            var session = new InferenceSession(g2pData);
            return (dict, session);
        }

        protected string RemoveTailDigits(string s) {
            while (char.IsDigit(s.Last())) {
                s = s.Substring(0, s.Length - 1);
            }
            return s;
        }

        public bool IsValidSymbol(string symbol) {
            return Dict.IsValidSymbol(symbol);
        }

        public bool IsVowel(string symbol) {
            return Dict.IsVowel(symbol);
        }

        public string[] Query(string grapheme) {
            var phonemes = Dict.Query(grapheme);
            if (phonemes == null && !PredCache.TryGetValue(grapheme, out phonemes)) {
                phonemes = Predict(grapheme);
                PredCache.Add(grapheme, phonemes);
            }
            return phonemes;
        }

        public string[] UnpackHint(string hint, char separator = ' ') {
            return Dict.UnpackHint(hint, separator);
        }

        protected string[] Predict(string grapheme) {
            Tensor<int> src = EncodeWord(grapheme);
            if (src.Length == 0) {
                return new string[0];
            }
            Tensor<int> tgt = new int[,] { { 2 } }.ToTensor();
            Tensor<int> t = new DenseTensor<int>(1);
            var srcLength = src.Dimensions[1];
            var inputs = new List<NamedOnnxValue>();
            while (t[0] < srcLength && tgt.Length < 48) {
                inputs.Clear();
                inputs.Add(NamedOnnxValue.CreateFromTensor("src", src));
                inputs.Add(NamedOnnxValue.CreateFromTensor("tgt", tgt));
                inputs.Add(NamedOnnxValue.CreateFromTensor("t", t));
                var outputs = Session.Run(inputs);
                var pred = outputs.First().AsTensor<int>()[0];
                if (pred != 2) {
                    var newTgt = new DenseTensor<int>(new int[] { 1, tgt.Dimensions[1] + 1 });
                    for (int i = 0; i < tgt.Dimensions[1]; ++i) {
                        newTgt[0, i] = tgt[0, i];
                    }
                    newTgt[0, tgt.Dimensions[1]] = pred;
                    tgt = newTgt;
                } else {
                    t[0] += 1;
                }
                outputs.Dispose();
            }
            var phonemes = DecodePhonemes(tgt.Skip(1).ToArray());
            return phonemes;
        }

        protected Tensor<int> EncodeWord(string grapheme) {
            var encoded = new List<int>();
            foreach (char c in grapheme.ToLowerInvariant()) {
                if (GraphemeIndexes.TryGetValue(c.ToString(), out var index)) {
                    encoded.Add(index);
                }
            }
            var tensor = new DenseTensor<int>(new int[] { 1, encoded.Count });
            for (int i = 0; i < encoded.Count; ++i) {
                tensor[0, i] = encoded[i];
            }
            return tensor;
        }

        protected string[] DecodePhonemes(int[] indexes) {
            return indexes.Select(idx => Phonemes[idx]).ToArray();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using OpenUtau.Api;

namespace OpenUtau.Core.G2p {
    public class DutchTompoesG2p : G2pPack {
        private static readonly string[] graphemes = new string[] {
            "", "", "", "", "'", "-", "A", "B", "C", "D", "E", "F",
            "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R",
            "S", "T", "U", "V", "W", "X", "Y", "Z", "a", "b", "c", "d",
            "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p",
            "q", "r", "s", "t", "u", "v", "w", "x", "y", "z",
        };

        private static readonly string[] phonemes = new string[] {
            "", "", "", "", "a", "aa", "aai", "au", "b", "d", "e", "ee",
            "eeu", "ei", "er", "eu", "ex", "f", "g", "h", "i", "ie", "ieu", "j",
            "k", "l", "m", "n", "ng", "o", "oe", "oei", "oi", "oo", "p", "r",
            "s", "sj", "t", "tj", "u", "ui", "uu", "uw", "v", "w", "z", "q",
            "vf", "cl",
        };

        private static object lockObj = new object();
        private static Dictionary<string, int> graphemeIndexes;
        private static IG2p dict;
        private static InferenceSession session;
        private static Dictionary<string, string[]> predCache = new Dictionary<string, string[]>();

        public DutchTompoesG2p() {
            lock (lockObj) {
                if (graphemeIndexes == null) {
                    graphemeIndexes = graphemes
                        .Skip(4)
                        .Select((g, i) => Tuple.Create(g, i))
                        .ToDictionary(t => t.Item1, t => t.Item2 + 4);
                    var tuple = LoadPack(Data.Resources.g2p_nl_tompoes);
                    dict = tuple.Item1;
                    session = tuple.Item2;
                }
            }
            GraphemeIndexes = graphemeIndexes;
            Phonemes = phonemes;
            Dict = dict;
            Session = session;
            PredCache = predCache;
        }
    }
}

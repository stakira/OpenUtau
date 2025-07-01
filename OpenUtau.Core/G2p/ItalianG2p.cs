using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using OpenUtau.Api;

namespace OpenUtau.Core.G2p {
    public class ItalianG2p : G2pPack {
        private static readonly string[] graphemes = new string[] {
            "", "", "", "", "\'", "a", "b", "c", "d", "e",
            "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p",
            "q", "r", "s", "t", "u", "v", "w", "x", "y", "z",
            "à", "é", "è", "È", "í", "ì", "ò", "ú", "ù",
        };

        private static readonly string[] phonemes = new string[] {
            "", "", "", "", "a", "a1", "b", "d", "dz", "dZZ", "e", "e1", "EE", "f",
            "g", "i", "i1", "JJ", "k", "l", "LL", "m", "n", "nf", "ng", "o", "o1",
            "OO", "p", "r", "rr", "s", "SS", "t", "ts", "tSS", "u", "u1", "v", "w",
            "y", "z",
        };

        private static object lockObj = new object();
        private static Dictionary<string, int> graphemeIndexes;
        private static IG2p dict;
        private static InferenceSession session;
        private static Dictionary<string, string[]> predCache = new Dictionary<string, string[]>();

        public ItalianG2p() {
            lock (lockObj) {
                if (graphemeIndexes == null) {
                    graphemeIndexes = graphemes
                        .Skip(4)
                        .Select((g, i) => Tuple.Create(g, i))
                        .ToDictionary(t => t.Item1, t => t.Item2 + 4);
                    var tuple = LoadPack(Data.Resources.g2p_it);
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

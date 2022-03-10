using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using OpenUtau.Api;

namespace OpenUtau.Plugin.Builtin {
    public class InterFRG2p : G2pPack {
        private static readonly string[] graphemes = new string[] {
            "", "", "", "", "-", "a", "b", "c", "d", "e", "f", "g", "h",
            "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t",
            "u", "v", "w", "x", "y", "z", "à", "á", "â", "ã", "ç",
            "è", "é", "ê", "í", "î", "ó", "ô", "õ", "ú", "û", "ü",
            "1", "2", "3", "4", "5", "6", "7", "8", "9", "0",
        };

        private static readonly string[] phonemes = new string[] {
            "", "", "", " ", "ii", "ei", "ai", "aa", "oo", "au",
            "ou", "uu", "ee", "oe", "in", "an", "on", "bb", "ch",
            "dd", "ff", "gg", "jj", "kk", "ll", "mm", "nn", "pp",
            "rr", "ss", "tt", "uy", "vv", "ww", "yy", "zz",
        };

        private static object lockObj = new object();
        private static Dictionary<string, int> graphemeIndexes;
        private static IG2p dict;
        private static InferenceSession session;
        private static Dictionary<string, string[]> predCache = new Dictionary<string, string[]>();

        public InterFRG2p() {
            lock (lockObj) {
                if (graphemeIndexes == null) {
                    graphemeIndexes = graphemes
                    .Skip(4)
                    .Select((g, i) => Tuple.Create(g, i))
                    .ToDictionary(t => t.Item1, t => t.Item2 + 4);
                    var tuple = LoadPack(Data.Resources.g2p_frint);
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

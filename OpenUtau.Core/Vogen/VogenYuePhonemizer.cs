using Microsoft.ML.OnnxRuntime;
using OpenUtau.Api;
using System;

namespace OpenUtau.Core.Vogen {
    [Phonemizer("Vogen Chinese Yue Phonemizer", "VOGEN ZH-YUE")]
    public class VogenYuePhonemizer : VogenBasePhonemizer {
        private static TrieNode trie;
        private static InferenceSession g2p;
        private static InferenceSession prosody;

        public VogenYuePhonemizer() {
            trie ??= TrieNode.LoadDictionary(
                Data.VogenRes.yue
                    .Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None));
            g2p ??= new InferenceSession(Data.VogenRes.g2p_yue);
            G2p = g2p;
            prosody ??= new InferenceSession(Data.VogenRes.po_yue);
            Prosody = prosody;
        }

        protected override string LangPrefix => "yue:";

        protected override string Romanize(string lyric) {
            var romanized = trie.Query(lyric);
            if (romanized != null && romanized.Length > 0) {
                return romanized[0];
            }
            return string.Empty;
        }
    }
}

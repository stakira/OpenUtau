using System.Collections.Generic;
using System.Linq;

namespace OpenUtau.Api {
    public class G2pRemapper : IG2p {
        private IG2p mapped;
        private Dictionary<string, bool> phonemeSymbols; // (phoneme, isVowel)
        private HashSet<string> glideSymbols;
        private Dictionary<string, string> replacements;

        public G2pRemapper(IG2p mapped,
            Dictionary<string, bool> phonemeSymbols,
            Dictionary<string, string> replacements,
            HashSet<string> glideSymbols = null) {
            this.mapped = mapped;
            this.phonemeSymbols = phonemeSymbols;
            this.replacements = replacements;
            this.glideSymbols = glideSymbols ?? new HashSet<string>();
        }

        public bool IsValidSymbol(string symbol) {
            return phonemeSymbols.ContainsKey(symbol);
        }

        public bool IsVowel(string symbol) {
            return phonemeSymbols.TryGetValue(symbol, out var isVowel) && isVowel;
        }

        public bool IsGlide(string symbol) {
            return glideSymbols.Contains(symbol);
        }

        public string[] Query(string grapheme) {
            var phonemes = mapped.Query(grapheme);
            if (phonemes == null) {
                return null;
            }
            phonemes = phonemes.Clone() as string[];
            for (int i = 0; i < phonemes.Length; ++i) {
                if (replacements.TryGetValue(phonemes[i], out var replacement)) {
                    phonemes[i] = replacement;
                }
            }
            return phonemes;
        }

        public string[] UnpackHint(string hint, char separator = ' ') {
            return hint.Split(separator)
                .Where(s => phonemeSymbols.ContainsKey(s))
                .ToArray();
        }
    }
}

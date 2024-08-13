namespace OpenUtau.Api {
    public interface IG2p {
        bool IsValidSymbol(string symbol);
        bool IsVowel(string symbol);

        /// <summary>
        /// Returns true if the symbol is a semivowel or liquid phoneme, like y, w, l, r in English.
        /// </summary>
        bool IsGlide(string symbol);

        /// <summary>
        /// Produces a list of phonemes from grapheme.
        /// </summary>
        string[] Query(string grapheme);

        /// <summary>
        /// Produces a list of phonemes from hint, removing invalid symbols.
        /// </summary>
        string[] UnpackHint(string hint, char separator = ' ');
    }
}

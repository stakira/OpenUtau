namespace OpenUtau.Api {
    /// <summary>
    /// Data class used to deserialize yaml dictionary.
    /// </summary>
    public class G2pDictionaryData {
        public struct SymbolData {
            public string symbol;
            public string type;
        }

        public struct Entry {
            public string grapheme;
            public string[] phonemes;
        }

        public SymbolData[] symbols;
        public Entry[] entries;
    }
}

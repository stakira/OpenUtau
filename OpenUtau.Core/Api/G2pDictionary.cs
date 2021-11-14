using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenUtau.Api {
    public class G2pDictionary : IG2p {
        /// <summary>
        /// Dictionaries are stored as a trie for compact footprint and quick access.
        /// See https://en.wikipedia.org/wiki/Trie.
        /// </summary>
        class TrieNode {
            public Dictionary<char, TrieNode> children = new Dictionary<char, TrieNode>();
            public string[] symbols;
        }

        static object locker = new object();
        static Dictionary<string, G2pDictionary> shared = new Dictionary<string, G2pDictionary>();

        public static G2pDictionary GetShared(string key) {
            lock (locker) {
                if (shared.TryGetValue(key, out var dict)) {
                    return dict;
                }
                if (key == "cmudict") {
                    var builder = NewBuilder();
                    Core.Api.Resources.cmudict_0_7b_phones.Split('\n')
                        .Select(line => line.Trim().ToLowerInvariant())
                        .Select(line => line.Split())
                        .Where(parts => parts.Length == 2)
                        .ToList()
                        .ForEach(parts => builder.AddSymbol(parts[0], parts[1]));
                    Core.Api.Resources.cmudict_0_7b.Split('\n').AsParallel().ForAll(line => {
                        if (line.StartsWith(";;;")) {
                            return;
                        }
                        line = line.Trim().ToLowerInvariant();
                        var parts = line.Split(new string[] { "  " }, StringSplitOptions.None);
                        if (parts.Length != 2) {
                            return;
                        }
                        var values = parts[1].Split().Select(symbol => RemoveTailDigits(symbol));
                        lock (builder) {
                            builder.AddEntry(parts[0], values);
                        }
                    });
                    dict = builder.Build();
                    lock (locker) {
                        shared[key] = dict;
                    }
                    return dict;
                }
                return null;
            }
        }

        public static void PutShared(string key, G2pDictionary dict) {
            lock (locker) {
                shared[key] = dict;
            }
        }

        static string RemoveTailDigits(string s) {
            while (char.IsDigit(s.Last())) {
                s = s.Substring(0, s.Length - 1);
            }
            return s;
        }

        TrieNode root;
        Dictionary<string, bool> phonemeSymbols; // (phoneme, isVowel)

        G2pDictionary(TrieNode root, Dictionary<string, bool> phonemeSymbols) {
            this.root = root;
            this.phonemeSymbols = phonemeSymbols;
        }

        public bool IsValidSymbol(string symbol) {
            return phonemeSymbols.ContainsKey(symbol);
        }

        public bool IsVowel(string symbol) {
            return phonemeSymbols.TryGetValue(symbol, out var isVowel) && isVowel;
        }

        public string[] Query(string grapheme) {
            return QueryTrie(root, grapheme, 0);
        }

        public string[] UnpackHint(string hint, char separator = ' ') {
            return hint.Split(separator)
                .Where(s => phonemeSymbols.ContainsKey(s))
                .ToArray();
        }

        string[] QueryTrie(TrieNode node, string word, int index) {
            if (index == word.Length) {
                return node.symbols;
            }
            if (node.children.TryGetValue(word[index], out var child)) {
                return QueryTrie(child, word, index + 1);
            }
            return null;
        }

        public class Builder {
            TrieNode root;
            Dictionary<string, bool> phonemeSymbols; // (phoneme, isVowel)

            internal Builder() {
                root = new TrieNode();
                phonemeSymbols = new Dictionary<string, bool>();
            }

            /// <summary>
            /// Add valid symbols of dictionary.
            /// </summary>
            public Builder AddSymbol(string symbol, string type) {
                phonemeSymbols[symbol] = type == "vowel";
                return this;
            }
            public Builder AddSymbol(string symbol, bool isVowel) {
                phonemeSymbols[symbol] = isVowel;
                return this;
            }

            /// <summary>
            /// Must finish adding symbols before adding entries, otherwise symbols get ignored.
            /// </summary>
            public Builder AddEntry(string grapheme, IEnumerable<string> symbols) {
                BuildTrie(root, grapheme, 0, symbols);
                return this;
            }

            void BuildTrie(TrieNode node, string grapheme, int index, IEnumerable<string> symbols) {
                if (index == grapheme.Length) {
                    node.symbols = symbols
                        .Where(symbol => phonemeSymbols.ContainsKey(symbol))
                        .ToArray();
                    return;
                }
                if (!node.children.TryGetValue(grapheme[index], out var child)) {
                    child = new TrieNode();
                    node.children[grapheme[index]] = child;
                }
                BuildTrie(child, grapheme, index + 1, symbols);
            }

            public Builder Load(string input) {
                var data = Core.Yaml.DefaultDeserializer.Deserialize<G2pDictionaryData>(input);
                if (data.symbols != null) {
                    foreach (var symbolData in data.symbols) {
                        AddSymbol(symbolData.symbol, symbolData.type);
                    }
                }
                if (data.entries != null) {
                    foreach (var entry in data.entries) {
                        AddEntry(entry.grapheme, entry.phonemes);
                    }
                }
                return this;
            }

            public Builder Load(TextReader textReader) {
                var data = Core.Yaml.DefaultDeserializer.Deserialize<G2pDictionaryData>(textReader);
                foreach (var symbolData in data.symbols) {
                    AddSymbol(symbolData.symbol, symbolData.type);
                }
                foreach (var entry in data.entries) {
                    AddEntry(entry.grapheme, entry.phonemes);
                }
                return this;
            }

            public G2pDictionary Build() {
                return new G2pDictionary(root, phonemeSymbols);
            }
        }

        public static Builder NewBuilder() {
            return new Builder();
        }
    }
}

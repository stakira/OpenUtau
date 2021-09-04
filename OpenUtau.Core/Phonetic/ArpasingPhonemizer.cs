using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core {
    public class ArpasingPhonemizer : Phonemizer {
        enum PhoneType { vowel, stop, affricate, fricative, aspirate, liquid, nasal, semivowel }
        class TrieNode {
            public Dictionary<char, TrieNode> children = new Dictionary<char, TrieNode>();
            public string[] symbols;
        }

        static Dictionary<string, PhoneType> phones;
        static TrieNode root;

        static ArpasingPhonemizer() {
            root = new TrieNode();
            phones = Properties.Resources.cmudict_0_7b_phones.Split('\n')
                .Select(line => line.ToLowerInvariant())
                .Select(line => line.Split())
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => (PhoneType)Enum.Parse(typeof(PhoneType), parts[1]));
            Properties.Resources.cmudict_0_7b.Split('\n')
               .Where(line => !line.StartsWith(";;;"))
                .Select(line => line.ToLowerInvariant())
               .Select(line => line.Split(new string[] { "  " }, StringSplitOptions.None))
               .Where(parts => parts.Length == 2)
               .ToList()
               .ForEach(parts => BuildTrie(root, parts[0], 0, parts[1]));
        }

        static void BuildTrie(TrieNode node, string word, int index, string symbols) {
            if (index == word.Length) {
                node.symbols = symbols.Split()
                    .Select(symbol => RemoveTailDigits(symbol))
                    .ToArray();
                return;
            }
            if (!node.children.TryGetValue(word[index], out var child)) {
                child = new TrieNode();
                node.children[word[index]] = child;
            }
            BuildTrie(child, word, index + 1, symbols);
        }

        static string[] QueryTrie(TrieNode node, string word, int index) {
            if (index == word.Length) {
                return node.symbols;
            }
            if (node.children.TryGetValue(word[index], out var child)) {
                return QueryTrie(child, word, index + 1);
            }
            return null;
        }

        private USinger singer;

        public override string Name => "Arpasing Phonemizer";
        public override string Tag => "EN ARPA";
        public override void SetSinger(USinger singer) => this.singer = singer;

        public override Phoneme[] Process(Note[] notes, Note? prev, Note? next) {
            var note = notes[0];
            var prevSymbols = prev == null ? null : QueryTrie(root, prev?.lyric, 0);
            var symbols = QueryTrie(root, note.lyric, 0);
            if (symbols == null || symbols.Length == 0) {
                if (note.lyric == "-" && prevSymbols != null) {
                    return new Phoneme[] {
                        new Phoneme() {
                            phoneme = $"{prevSymbols.Last()} -",
                            duration = note.duration,
                        }
                    };
                }
                return new Phoneme[] {
                    new Phoneme() {
                        phoneme = note.lyric,
                        duration = note.duration,
                    }
                };
            }
            var result = new Phoneme[symbols.Length];
            string prevSymbol = prevSymbols == null ? "-" : prevSymbols.Last();
            string phoneme = TryMapPhoneme($"{prevSymbol} {symbols[0]}", note.tone, singer);
            if (!singer.TryGetOto(phoneme, note.tone, out var _)) {
                phoneme = TryMapPhoneme($"- {symbols[0]}", note.tone, singer); // Fallback to not use vc
            }
            result[0] = new Phoneme {
                phoneme = phoneme,
            };
            for (int i = 1; i < symbols.Length; i++) {
                phoneme = TryMapPhoneme($"{symbols[i - 1]} {symbols[i]}", note.tone, singer);
                if (!singer.TryGetOto(phoneme, note.tone, out var _)) {
                    phoneme = TryMapPhoneme($"- {symbols[i]}", note.tone, singer); // Fallback to not use vc
                }
                result[i] = new Phoneme {
                    phoneme = phoneme,
                };
            }

            // Distrubute duration
            int consonants = 0;
            int vowels = 0;
            int duration = notes.Sum(n => n.duration);
            for (int i = 0; i < symbols.Length; i++) {
                if (phones[symbols[i]] == PhoneType.vowel || phones[symbols[i]] == PhoneType.semivowel) {
                    vowels++;
                } else {
                    consonants++;
                }
            }
            int consonantDuration = consonants > 0 ? Math.Min(60, duration / 2 / consonants) : 0;
            int vowelDuration = vowels > 0 ? (duration - consonantDuration * consonants) / vowels : 0;
            for (int i = 0; i < symbols.Length; i++) {
                if (phones[symbols[i]] == PhoneType.vowel || phones[symbols[i]] == PhoneType.semivowel) {
                    result[i].duration = vowelDuration;
                } else {
                    result[i].duration = consonantDuration;
                }
            }
            return result;
        }

        static string RemoveTailDigits(string s) {
            while (char.IsDigit(s.Last())) {
                s = s.Substring(0, s.Length - 1);
            }
            return s;
        }
    }
}

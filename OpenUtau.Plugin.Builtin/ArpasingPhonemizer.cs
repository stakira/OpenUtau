using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    /// <summary>
    /// The English Arpasing Phonemizer.
    /// <para>
    /// Arpasing is a system that uses CMUdict as dictionary to convert English words to phoneme symbols.
    /// See http://www.speech.cs.cmu.edu/cgi-bin/cmudict and https://arpasing.neocities.org/en/faq.html.
    /// </para>
    /// </summary>
    [Phonemizer("English Arpasing Phonemizer", "EN ARPA")]
    public class ArpasingPhonemizer : Phonemizer {
        enum PhoneType { vowel, stop, affricate, fricative, aspirate, liquid, nasal, semivowel }
        /// <summary>
        /// The CMUdict is stored as a trie for compact footprint and quick access.
        /// See https://en.wikipedia.org/wiki/Trie.
        /// </summary>
        class TrieNode {
            public Dictionary<char, TrieNode> children = new Dictionary<char, TrieNode>();
            public string[] symbols;
        }

        static readonly object initLock = new object();
        static Dictionary<string, PhoneType> phones;
        static Dictionary<string, string[]> vowelFallback;
        /// <summary>
        /// The root node of the CMUdict trie.
        /// </summary>
        static TrieNode root;

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

        /// <summary>
        /// Produce a symbol list from hints or lyric.
        /// </summary>
        /// <param name="note"></param>
        /// <returns></returns>
        static string[] GetSymbols(Note note) {
            if (string.IsNullOrEmpty(note.phoneticHint)) {
                // User has not provided hint, query CMUdict.
                return QueryTrie(root, note.lyric, 0);
            }
            // Split space-separated symbols into an array.
            return note.phoneticHint.Split()
                .Where(s => phones.ContainsKey(s)) // skip the invalid symbols.
                .ToArray();
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

        static string RemoveTailDigits(string s) {
            while (char.IsDigit(s.Last())) {
                s = s.Substring(0, s.Length - 1);
            }
            return s;
        }

        private USinger singer;
        private readonly List<Tuple<int, int>> alignments = new List<Tuple<int, int>>();

        /// <summary>
        /// This property will later be exposed in UI for user adjustment.
        /// </summary>
        public int ConsonantLength { get; set; } = 60;

        public ArpasingPhonemizer() {
            Initialize();
        }

        /// <summary>
        /// Initializes the CMUdict.
        /// </summary>
        private static void Initialize() {
            Task.Run(() => {
                try {
                    lock (initLock) {
                        if (ArpasingPhonemizer.root != null) {
                            // If already initialized, skip it.
                            return;
                        }
                        var root = new TrieNode();
                        phones = Resources.cmudict_0_7b_phones.Split('\n')
                            .Select(line => line.Trim().ToLowerInvariant())
                            .Select(line => line.Split())
                            .Where(parts => parts.Length == 2)
                            .ToDictionary(parts => parts[0], parts => (PhoneType)Enum.Parse(typeof(PhoneType), parts[1]));
                        // Arpasing voicebanks are often incomplete. A fallback table is used to slightly improve the situation.
                        vowelFallback = "aa=ah,ae;ae=ah,aa;ah=aa,ae;ao=ow;ow=ao;eh=ae;ih=iy;iy=ih;uh=uw;uw=uh;aw=ao".Split(';')
                            .Select(entry => entry.Split('='))
                            .ToDictionary(parts => parts[0], parts => parts[1].Split(','));
                        Resources.cmudict_0_7b.Split('\n')
                           .Where(line => !line.StartsWith(";;;"))
                            .Select(line => line.Trim().ToLowerInvariant())
                           .Select(line => line.Split(new string[] { "  " }, StringSplitOptions.None))
                           .Where(parts => parts.Length == 2)
                           .ToList()
                           .ForEach(parts => BuildTrie(root, parts[0], 0, parts[1]));
                        ArpasingPhonemizer.root = root;
                    }
                } catch (Exception e) {
                    Log.Error(e, "Failed to initialize.");
                }
            });
        }

        // Simply stores the singer in a field.
        public override void SetSinger(USinger singer) => this.singer = singer;

        public override Phoneme[] Process(Note[] notes, Note? prevNeighbour, Note? nextNeighbour) {
            lock (initLock) {
                if (root == null) {
                    return new Phoneme[0];
                }
            }
            var note = notes[0];
            // Get the symbols of previous note.
            var prevSymbols = prevNeighbour == null ? null : GetSymbols(prevNeighbour.Value);
            // Get the symbols of current note.
            var symbols = GetSymbols(note);
            if (symbols == null || symbols.Length == 0) {
                // No symbol is found for current note.
                if (note.lyric == "-" && prevSymbols != null) {
                    // The user is using a tail "-" note to produce a "<something> -" sound.
                    return new Phoneme[] {
                        new Phoneme() {
                            phoneme = $"{prevSymbols.Last()} -",
                        }
                    };
                }
                // Otherwise assumes the user put in a 
                return new Phoneme[] {
                    new Phoneme() {
                        phoneme = note.lyric,
                    }
                };
            }
            // Find phone types of symbols.
            var phoneTypes = symbols.Select(s => phones[s]).ToArray();
            // Arpasing aligns the first vowel at 0 and shifts leading consonants to negative positions,
            // so we need to find the first vowel.
            int firstVowel = Array.IndexOf(phoneTypes, PhoneType.vowel);
            var phonemes = new Phoneme[symbols.Length];
            // Creates the first diphone using info of the previous note.
            string prevSymbol = prevSymbols == null ? "-" : prevSymbols.Last();
            string phoneme = $"{prevSymbol} {symbols[0]}";
            if (!singer.TryGetMappedOto(phoneme, note.tone, out var _)) {
                // Arpasing voicebanks are often incomplete. If the voicebank doesn't have this diphone, fallback to use a more likely to exist one.
                phoneme = $"- {symbols[0]}";
            }
            phonemes[0] = new Phoneme {
                phoneme = phoneme,
            };
            // The 2nd+ diphones.
            for (int i = 1; i < symbols.Length; i++) {
                // The logic is very similar to creating the first diphone.
                phonemes[i] = new Phoneme {
                    phoneme = GetPhonemeOrFallback(symbols[i - 1], symbols[i], note.tone),
                };
            }

            // Alignments
            // Alignment is where a user use "...n" (n is a number) to align n-th phoneme with an extender note.
            // We build the aligment points first, these are the phonemes must be aligned to a certain position,
            // phonemes that are not aligment points are distributed in-between.
            alignments.Clear();
            if (firstVowel > 0) {
                // If there are leading consonants, add the first vowel as an align point.
                alignments.Add(Tuple.Create(firstVowel, 0));
            } else {
                firstVowel = 0;
            }
            int position = 0;
            for (int i = 0; i < notes.Length; ++i) {
                string alignmentHint = notes[i].lyric;
                if (alignmentHint.StartsWith("...")) {
                    alignmentHint = alignmentHint.Substring(3);
                } else {
                    position += notes[i].duration;
                    continue;
                }
                // Parse the number n in "...n".
                if (int.TryParse(alignmentHint, out int index)) {
                    index--; // Convert from 1-based index to 0-based index.
                    if (index > 0 && (alignments.Count == 0 || alignments.Last().Item1 < index) && index < phonemes.Length) {
                        // Adds a alignment point.
                        // Some details in the if condition:
                        // 1. The first phoneme cannot be user-aligned.
                        // 2. The index must be incrementing, otherwise ignored.
                        // 3. The index must be within range.
                        alignments.Add(Tuple.Create(index, position));
                    }
                }
                position += notes[i].duration;
            }
            alignments.Add(Tuple.Create(phonemes.Length, position));

            int startIndex = 0;
            int startTick = -ConsonantLength * firstVowel;
            foreach (var alignment in alignments) {
                // Distributes phonemes between two aligment points.
                DistributeDuration(phoneTypes, phonemes, startIndex, alignment.Item1, startTick, alignment.Item2);
                startIndex = alignment.Item1;
                startTick = alignment.Item2;
            }
            alignments.Clear();

            MapPhonemes(notes, phonemes, singer);
            return phonemes;
        }

        string GetPhonemeOrFallback(string prevSymbol, string symbol, int tone) {
            string phoneme = $"{prevSymbol} {symbol}";
            if (singer.TryGetMappedOto(phoneme, tone, out var _)) {
                return phoneme;
            }
            if (vowelFallback.TryGetValue(symbol, out string[] fallbacks)) {
                foreach (var fallback in fallbacks) {
                    phoneme = $"{prevSymbol} {fallback}";
                    if (singer.TryGetMappedOto(phoneme, tone, out var _)) {
                        return phoneme;
                    }
                }
            }
            return $"- {symbol}";
        }

        void DistributeDuration(PhoneType[] phoneTypes, Phoneme[] phonemes, int startIndex, int endIndex, int startTick, int endTick) {
            // First count number of vowels and consonants.
            int consonants = 0;
            int vowels = 0;
            int duration = endTick - startTick;
            for (int i = startIndex; i < endIndex; i++) {
                if (phoneTypes[i] == PhoneType.vowel) {
                    vowels++;
                } else {
                    consonants++;
                }
            }
            // If vowels exist, consonants are given fixed length, but combined no more than half duration.
            // However, if no vowel exists, consonants are evenly distributed within the total duration.
            int consonantDuration = vowels > 0
                ? (consonants > 0 ? Math.Min(ConsonantLength, duration / 2 / consonants) : 0)
                : duration / consonants;
            // Vowels are evenly distributed within (total duration - total consonant duration).
            int vowelDuration = vowels > 0 ? (duration - consonantDuration * consonants) / vowels : 0;
            int position = startTick;
            // Compute positions using previously computed durations.
            for (int i = startIndex; i < endIndex; i++) {
                if (phoneTypes[i] == PhoneType.vowel) {
                    phonemes[i].position = position;
                    position += vowelDuration;
                } else {
                    phonemes[i].position = position;
                    position += consonantDuration;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private Dictionary<string, string[]> vowelFallback;
        private USinger singer;
        private IG2p cmudict;
        private IG2p pluginDict;
        private IG2p singerDict;
        private IG2p mergedG2p;

        private readonly List<Tuple<int, int>> alignments = new List<Tuple<int, int>>();

        /// <summary>
        /// This property will later be exposed in UI for user adjustment.
        /// </summary>
        public int ConsonantLength { get; set; } = 60;

        public ArpasingPhonemizer() {
            try {
                Initialize();
            } catch (Exception e) {
                Log.Error(e, "Failed to initialize.");
            }
        }

        /// <summary>
        /// Initializes the CMUdict.
        /// </summary>
        private void Initialize() {
            // Load cmudict.
            cmudict = G2pDictionary.GetShared("cmudict");
            // Load g2p plugin dictionary.
            string filepath = Path.Combine(PluginDir, "arpasing.yaml");
            try {
                CreateDefaultPluginDict(filepath);
                if (File.Exists(filepath)) {
                    pluginDict = G2pDictionary.NewBuilder().Load(File.ReadAllText(filepath)).Build();
                }
            } catch (Exception e) {
                Log.Error(e, $"Failed to load {filepath}");
            }
            // Load g2p singer dictionary.
            LoadSingerDict();
            mergedG2p = new G2pFallbacks(new IG2p[] { pluginDict, singerDict, cmudict }.OfType<IG2p>().ToArray());
            // Arpasing voicebanks are often incomplete. A fallback table is used to slightly improve the situation.
            vowelFallback = "aa=ah,ae;ae=ah,aa;ah=aa,ae;ao=ow;ow=ao;eh=ae;ih=iy;iy=ih;uh=uw;uw=uh;aw=ao".Split(';')
                .Select(entry => entry.Split('='))
                .ToDictionary(parts => parts[0], parts => parts[1].Split(','));
        }

        private void CreateDefaultPluginDict(string filepath) {
            if (File.Exists(filepath)) {
                return;
            }
            File.WriteAllBytes(filepath, Core.Api.Resources.arpasing_template);
        }

        private void LoadSingerDict() {
            if (singer != null && singer.Loaded) {
                string file = Path.Combine(singer.Location, "arpasing.yaml");
                if (File.Exists(file)) {
                    try {
                        singerDict = G2pDictionary.NewBuilder().Load(File.ReadAllText(file)).Build();
                    } catch (Exception e) {
                        Log.Error(e, $"Failed to load {file}");
                    }
                }
            }
        }

        // Simply stores the singer in a field.
        public override void SetSinger(USinger singer) {
            this.singer = singer;
            LoadSingerDict();
            mergedG2p = new G2pFallbacks(new IG2p[] { pluginDict, singerDict, cmudict }.OfType<IG2p>().ToArray());
        }

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour) {
            var note = notes[0];
            // Get the symbols of previous note.
            var prevSymbols = prevNeighbour == null ? null : GetSymbols(prevNeighbour.Value);
            // Get the symbols of current note.
            var symbols = GetSymbols(note);
            if (symbols == null || symbols.Length == 0) {
                // No symbol is found for current note.
                if (note.lyric == "-" && prevSymbols != null) {
                    // The user is using a tail "-" note to produce a "<something> -" sound.
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme() {
                                phoneme = $"{prevSymbols.Last()} -",
                            }
                        },
                    };
                }
                // Otherwise assumes the user put in an alias.
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme() {
                            phoneme = note.lyric,
                        }
                    },
                };
            }
            // Find phone types of symbols.
            var isVowel = symbols.Select(s => mergedG2p.IsVowel(s)).ToArray();
            // Arpasing aligns the first vowel at 0 and shifts leading consonants to negative positions,
            // so we need to find the first vowel.
            int firstVowel = Array.IndexOf(isVowel, true);
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
                DistributeDuration(isVowel, phonemes, startIndex, alignment.Item1, startTick, alignment.Item2);
                startIndex = alignment.Item1;
                startTick = alignment.Item2;
            }
            alignments.Clear();

            MapPhonemes(notes, phonemes, singer);
            return new Result {
                phonemes = phonemes,
            };
        }

        string[] GetSymbols(Note note) {
            if (string.IsNullOrEmpty(note.phoneticHint)) {
                // User has not provided hint, query CMUdict.
                return mergedG2p.Query(note.lyric.ToLowerInvariant());
            }
            // Split space-separated symbols into an array.
            return note.phoneticHint.Split()
                .Where(s => mergedG2p.IsValidSymbol(s)) // skip the invalid symbols.
                .ToArray();
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

        void DistributeDuration(bool[] isVowel, Phoneme[] phonemes, int startIndex, int endIndex, int startTick, int endTick) {
            // First count number of vowels and consonants.
            int consonants = 0;
            int vowels = 0;
            int duration = endTick - startTick;
            for (int i = startIndex; i < endIndex; i++) {
                if (isVowel[i]) {
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
                if (isVowel[i]) {
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

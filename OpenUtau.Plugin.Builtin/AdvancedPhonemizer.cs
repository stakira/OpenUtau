using System;
using System.Collections.Generic;
using System.Text;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using System.Linq;
using System.IO;

namespace OpenUtau.Plugin.Builtin {
    /// <summary>
    /// Use this class as a base for easier phonemizer configuration
    /// Works for vb styles like VCV, VCCV, CVC etc, supports dictionary. 
    /// </summary>
    public abstract class AdvancedPhonemizer : Phonemizer {

        /// <summary>
        /// result of dictionary parsing. Phonemes from previous word are included to the first syllable
        /// </summary>
        protected struct Word {
            public Syllable[] syllables;
            public Ending ending;
        }

        /// <summary>
        /// Syllable is [V] [C..] [V]
        /// </summary>
        protected struct Syllable {
            /// <summary>
            /// vowel from previous syllable for VC
            /// </summary>
            public string prevV;
            /// <summary>
            /// CCs, may be empty
            /// </summary>
            public string[] cc;
            /// <summary>
            /// "base" note. May not actually be vowel, if only consonants way provided
            /// </summary>
            public string v;
            /// <summary>
            /// Start position for vowel. All VC CC goes before this position
            /// </summary>
            public int position;
            /// <summary>
            /// previous note duration, i.e. this is container for VC and CC notes
            /// </summary>
            public int duration;
            /// <summary>
            /// Tone for VC and CC
            /// </summary>
            public int tone;
            /// <summary>
            /// tone for base "vowel" phoneme
            /// </summary>
            public int vowelTone;
        }

        protected struct Ending {
            /// <summary>
            /// vowel from the last syllable to make VC
            /// </summary>
            public string prevV;
            /// <summary>
            ///  actuall CC at the ending
            /// </summary>
            public string[] cc;
            /// <summary>
            /// last note position + duration, all phonemes must be less than this
            /// </summary>
            public int position;
            /// <summary>
            /// last syllable length, max container for all VC CC C-
            /// </summary>
            public int duration;
            /// <summary>
            /// the tone from last syllable, for all ending phonemes
            /// </summary>
            public int tone;
        }

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour) {
            var mainNote = notes[0];
            if (mainNote.lyric.StartsWith(FORCED_ALIAS_SYMBOL)) {
                return MakeForcedAliasResult(mainNote);
            }

            Word? prevWord = null;
            if (prevNeighbour.HasValue && !prevNeighbour.Value.lyric.StartsWith(FORCED_ALIAS_SYMBOL)) {
                prevWord = MakeWord(new Note[] { prevNeighbour.Value });
            }
            var word = MakeWord(notes, prevWord);

            var phonemes = new List<Phoneme>();
            foreach (var syllable in word.syllables) {
                phonemes.AddRange(MakePhonemes(TrySyllable(syllable), syllable.duration, syllable.position,
                    syllable.tone, syllable.vowelTone, false));
            }
            if (!nextNeighbour.HasValue) {
                phonemes.AddRange(MakePhonemes(TryEnding(word.ending), word.ending.duration, word.ending.position, 
                    word.ending.tone, word.ending.tone, true));
            }

            return new Result() {
                phonemes = phonemes.ToArray()
            };
        }

        public override void SetSinger(USinger singer) {
            this.singer = singer;
            if (!hasDictionary) {
                ReadDictionary();
            }
            Init();
        }

        protected USinger singer;
        /// <summary>
        /// may be used to apply shorter aliases
        /// </summary>
        protected double shortNoteThreshold = 120;
        protected bool hasDictionary => dictionaries.ContainsKey(GetType());
        protected G2pDictionary dictionary => dictionaries[GetType()];

        private static Dictionary<Type, G2pDictionary> dictionaries = new Dictionary<Type, G2pDictionary>();
        private const string FORCED_ALIAS_SYMBOL = "/";

        /// <summary>
        /// Returns list of vowels
        /// </summary>
        /// <returns></returns>
        protected abstract string[] GetVowels();

        /// <summary>
        /// returns phoneme symbols, like, VCV, or VC + CV, or -CV, etc
        /// </summary>
        /// <returns>List of phonemes</returns>
        protected abstract List<string> TrySyllable(Syllable syllable);

        /// <summary>
        /// phoneme symbols for ending, like, V-, or VC-, or VC+C
        /// </summary>
        protected abstract List<string> TryEnding(Ending ending);

        /// <summary>
        /// simple alias to alias fallback
        /// </summary>
        /// <returns></returns>
        protected virtual Dictionary<string, string> GetAliasesFallback() { return null; }

        /// <summary>
        /// Use to some custom init, if needed
        /// </summary>
        protected virtual void Init() { }

        /// <summary>
        /// Override and provide the dictionary path, if available
        /// A file in CMU Dict format. Save it in Core/Api/Resources
        /// </summary>
        /// <returns></returns>
        protected virtual string GetDictionaryPath() { return null; }

        /// <summary>
        /// extracts array of phoneme symbols from note. Override for procedural dictionary or something
        /// reads from dictionary if provided
        /// </summary>
        /// <param name="note"></param>
        /// <returns></returns>
        protected virtual string[] GetSymbols(Note note) {
            string[] getSymbolsRaw(string lyrics) {
                if (lyrics == null) {
                    return new string[0];
                } else return lyrics.Split(" ");
            }

            if (hasDictionary) {
                if (!string.IsNullOrEmpty(note.phoneticHint)) {
                    return getSymbolsRaw(note.phoneticHint);
                }
                try {
                    var result = dictionary.Query(note.lyric.Trim().ToLowerInvariant());
                    return result != null ? result : new string[] { note.lyric };
                } catch {
                    return new string[] { note.lyric };
                }
            }
            else {
                return getSymbolsRaw(note.lyric);
            }
        }

        /// <summary>
        /// Checks if mapped and validated alias exists in oto
        /// </summary>
        /// <param name="alias"></param>
        /// <param name="note"></param>
        /// <returns></returns>
        protected bool HasOto(string alias, int tone) {
            return singer.TryGetMappedOto(ValidateAlias(alias), tone, out _);
        }

        /// <summary>
        /// Instead of changing symbols in cmudict itself for each reclist, 
        /// you may leave it be and provide symbol replacements with this method.
        /// </summary>
        /// <returns></returns>
        protected virtual Dictionary<string, string> GetDictionaryPhonemesReplacement() { return null; }

        /// <summary>
        /// separates symbols to syllables and ending.
        /// Not sure you would like to override it, but, anyway.
        /// </summary>
        /// <param name="notes"></param>
        /// <param name="prevWord"></param>
        /// <returns></returns>
        protected virtual Word MakeWord(Note[] notes, Word? prevWord = null) {
            var mainNote = notes[0];
            var symbols = GetSymbols(mainNote);
            if (symbols.Length == 0) {
                symbols = new string[] { "" };
            }
            List<int> vowelIds = ExtractVowels(symbols);
            if (vowelIds.Count == 0) {
                // no syllables or all consonants, the last phoneme will be interpreted as vowel
                vowelIds.Add(symbols.Length - 1);
            }
            var firstVowelId = vowelIds[0];
            Word word = new Word() {
                syllables = new Syllable[Math.Min(notes.Length, vowelIds.Count)]
            };

            // Making the first syllable
            if (prevWord.HasValue) {
                var prevEnding = prevWord.Value.ending;
                var beginningCc = prevEnding.cc.ToList();
                beginningCc.AddRange(symbols.Take(firstVowelId));

                // If we had a prev neighbour, let's take info from it
                word.syllables[0] = new Syllable() {
                    prevV = prevEnding.prevV,
                    cc = beginningCc.ToArray(),
                    v = symbols[firstVowelId],
                    tone = prevEnding.tone,
                    duration = prevEnding.duration,
                    position = 0,
                    vowelTone = mainNote.tone
                };
            } else {
                // there is only empty space before us
                word.syllables[0] = new Syllable() {
                    prevV = "",
                    cc = symbols.Take(firstVowelId).ToArray(),
                    v = symbols[firstVowelId],
                    tone = mainNote.tone,
                    duration = -1,
                    position = 0,
                    vowelTone = mainNote.tone
                };
            }

            // normal syllables after the first one
            var syllableI = 1;
            var ccs = new List<string>();
            var position = 0;
            for (var i = firstVowelId + 1; i < symbols.Length & syllableI < notes.Length; i++) {
                if (!vowelIds.Contains(i)) {
                    ccs.Add(symbols[i]);
                } else {
                    position += notes[syllableI - 1].duration;
                    word.syllables[syllableI] = new Syllable() {
                        prevV = word.syllables[syllableI - 1].v,
                        cc = ccs.ToArray(),
                        v = symbols[i],
                        tone = word.syllables[syllableI - 1].vowelTone,
                        duration = notes[syllableI - 1].duration,
                        position = position,
                        vowelTone = notes[syllableI].tone
                    };
                    ccs = new List<string>();
                    syllableI++;
                }
            }

            // making the ending
            var lastVowelI = vowelIds.Last();
            var lastNote = notes.Last();
            word.ending = new Ending() {
                prevV = word.syllables[syllableI - 1].v,
                cc = symbols.Skip(lastVowelI + 1).ToArray(),
                tone = lastNote.tone,
                duration = lastNote.duration,
                position = position + lastNote.duration
            };

            return word;
        }

        #region private

        private Result MakeForcedAliasResult(Note note) {
            return new Result {
                phonemes = new Phoneme[] {
                    new Phoneme {
                        phoneme = note.lyric.Substring(1)
                    }
                }
            };
        }

        private void ReadDictionary() {
            var dictPath = GetDictionaryPath();
            if (dictPath == null || !File.Exists(dictPath))
                return;
            try {
                var builder = G2pDictionary.NewBuilder();
                var vowels = GetVowels();
                foreach (var vowel in vowels) {
                    builder.AddSymbol(vowel, true);
                }
                var replacements = GetDictionaryPhonemesReplacement();

                File.ReadAllText(dictPath).Split('\n')
                        .Where(line => !line.StartsWith(";;;"))
                        .Select(line => line.Trim())
                        .Select(line => line.Split(new string[] { "  " }, StringSplitOptions.None))
                        .Where(parts => parts.Length == 2)
                        .ToList()
                        .ForEach(parts => builder.AddEntry(parts[0].ToLowerInvariant(), parts[1].Split(" ").Select(
                            n => {
                                var result = replacements != null && replacements.ContainsKey(n) ? replacements[n] : n;
                                if (!vowels.Contains(result))
                                    builder.AddSymbol(result, false);
                                return result;
                                }
                            )));
                var dict = builder.Build();
                dictionaries[GetType()] = dict;
            }
            catch (Exception ex) { }
        }

        private List<int> ExtractVowels(string[] symbols) {
            var vowelIds = new List<int>();
            var vowels = GetVowels();
            for (var i = 0; i < symbols.Length; i++) {
                if (vowels.Contains(symbols[i])) {
                    vowelIds.Add(i);
                }
            }
            return vowelIds;
        }

        private string ValidateAlias(string alias) {
            var aliasesFallback = GetAliasesFallback();
            return aliasesFallback == null ? alias : aliasesFallback.ContainsKey(alias) ? aliasesFallback[alias] : alias;
        }

        private int GetNoteLength(int phonemesCount, int containerLength = -1) {
            var noteLength = 120.0;
            if (containerLength == -1) {
                return MsToTick(noteLength) / 15 * 15;
            }

            var fullLength = noteLength * 1.5 + noteLength * phonemesCount;
            if (fullLength <= containerLength) {
                return MsToTick(noteLength) / 15 * 15;
            }
            return MsToTick(containerLength / fullLength * noteLength) / 15 * 15;
        }

        private Phoneme[] MakePhonemes(List<string> phonemeSymbols, int containerLength, int position, int tone, int lastTone, bool isEnding) {
            var phonemes = new Phoneme[phonemeSymbols.Count];
            var noteLengthTick = GetNoteLength(phonemeSymbols.Count - 1, containerLength);
            for (var i = 0; i < phonemeSymbols.Count; i++) {
                var offset = isEnding ? i + 1 : phonemeSymbols.Count - i - 1;
                var phonemeI = isEnding ? phonemeSymbols.Count - i - 1 : i;

                var currentTone = phonemeI == phonemeSymbols.Count - 1 ? lastTone : tone;
                phonemes[phonemeI].phoneme = MapPhoneme(ValidateAlias(phonemeSymbols[phonemeI]), currentTone, singer);
                phonemes[phonemeI].position = position - noteLengthTick * offset;
            }
            return phonemes;
        }

        #endregion
    }
}

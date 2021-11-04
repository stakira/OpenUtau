using System;
using System.Collections.Generic;
using System.Text;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using System.Linq;

namespace OpenUtau.Plugin.Builtin {
    public abstract class AdvancedPhonemizer : Phonemizer {

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour) {
            var note = notes[0];

            var symbols = GetSymbols(note);
            var prevNoteSymbols = prevNeighbour.HasValue ? GetSymbols(prevNeighbour.Value) : new string[0];
            var cv = GetCV(symbols);
            var prevLastConsonants = GetRightConsonants(prevNoteSymbols, true);
            (var prevV, var cc, var v) = Normalize(cv, prevLastConsonants);
            var phonemeSymbols = TrySyllable(note, prevV, cc, v);
            var basePhonemeI = phonemeSymbols.Count - 1;

            if (!nextNeighbour.HasValue) {
                var lastConsonants = GetRightConsonants(symbols, false);
                TryEnding(phonemeSymbols, note, v, lastConsonants);
            }
            var phonemes = MakePhonemes(phonemeSymbols, note, prevNeighbour, basePhonemeI);
            return new Result() {
                phonemes = phonemes
            };
        }

        public override void SetSinger(USinger singer) {
            this.singer = singer;
            Init();
        }

        protected USinger singer;
        protected double shortNoteThreshold = 120;

        /// <summary>
        /// Returns list of vowels
        /// </summary>
        /// <returns></returns>
        protected abstract string[] GetVowels();

        /// <summary>
        /// returns phoneme symbols, like, VCV, or VC + CV, or -CV, etc
        /// </summary>
        /// <param name="note">current note</param>
        /// <param name="prevV">the vowel of the previous note neighbour, may be empty string</param>
        /// <param name="cc">array of consonants, may be empty</param>
        /// <param name="v">base vowel</param>
        /// <returns></returns>
        protected abstract List<string> TrySyllable(Note note, string prevV, string[] cc, string v);

        /// <summary>
        /// phoneme symbols for ending, like, V-, or VC-, or VC+C
        /// </summary>
        /// <param name="phonemeSymbols">add new phonemes to this array</param>
        /// <param name="note">current note</param>
        /// <param name="v">base vowel</param>
        /// <param name="cc">consonants after the base vowel, may be emtpy</param>
        protected abstract void TryEnding(List<string> phonemeSymbols, Note note, string v, string[] cc);

        protected virtual Dictionary<string, string> GetAliasesFallback() { return null; }
        protected virtual void Init() { }

        /// <summary>
        /// extracts array of phoneme symbols from note. Override for procedural dictionary
        /// </summary>
        /// <param name="note"></param>
        /// <returns></returns>
        protected virtual string[] GetSymbols(Note note) {
            // dictionary is not yet supported so just read all lyrics as phonetic input
            if (note.lyric == null) {
                return new string[0];
            } else return note.lyric.Split(" ");
        }

        protected bool HasOto(string alias, Note note) {
            return singer.TryGetMappedOto(ValidateAlias(alias), note.tone, out _);
        }

        private string[] GetRightConsonants(string[] symbols, bool withV) {
            var vowels = GetVowels();
            if (symbols.Length == 0)
                return symbols;
            var vowelIdx = -1;
            for (var i = symbols.Length - 1; i >= 0; i--) {
                var symbol = (string)symbols[i];
                if (vowels.Contains(symbol)) {
                    vowelIdx = i;
                    break;
                }
            }
            if (vowelIdx == -1) {
                return new string[0];
            }
            if (withV) {
                return symbols.TakeLast(symbols.Length - vowelIdx).ToArray();
            } else if (vowelIdx == 0 && symbols.Length == 1) {
                return new string[0];
            } else {
                return symbols.TakeLast(symbols.Length - vowelIdx - 1).ToArray();
            }
        }

        private string[] GetCV(string[] symbols) {
            var vowels = GetVowels();
            var vowelIdx = -1;
            for (var i = symbols.Length - 1; i >= 0; i--) {
                var symbol = (string)symbols[i];
                if (vowels.Contains(symbol)) {
                    vowelIdx = i;
                    break;
                }
            }
            if (vowelIdx == -1) {
                return symbols;
            }
            return symbols.Take(vowelIdx + 1).ToArray();
        }

        private (string, string[], string) Normalize(string[] cv, string[] tail) {
            var vowels = GetVowels();
            var prevV = "";
            var v = "";
            var cc = new List<string>();
            if (tail.Length > 0) {
                var start = 0;
                if (vowels.Contains(tail[0])) {
                    prevV = tail[0];
                    start = 1;
                }
                for (var i = start; i < tail.Length; i++) {
                    cc.Add(tail[i]);
                }
            }
            if (cv.Length > 0) {
                v = cv.Last();
            }
            for (var i = 0; i < cv.Length - 1; i++) {
                cc.Add(cv[i]);
            }

            return (prevV, cc.ToArray(), v);
        }

        protected string ValidateAlias(string alias) {
            var aliasesFallback = GetAliasesFallback();
            return aliasesFallback == null ? alias : aliasesFallback.ContainsKey(alias) ? aliasesFallback[alias] : alias;
        }

        protected int GetNoteLength(int phonemesCount, int containerLength = -1) {
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

        private Phoneme[] MakePhonemes(List<string> phonemeSymbols, Note note, Note? prevNeighbour, int basePhonemeI) {
            var phonemes = new Phoneme[phonemeSymbols.Count];
            var noteLengthTick = GetNoteLength(phonemeSymbols.Count - 1, prevNeighbour.HasValue ? prevNeighbour.Value.duration : -1);
            var prevTone = prevNeighbour.HasValue ? prevNeighbour.Value.tone : note.tone;
            for (var i = 0; i < basePhonemeI; i++) {
                var offset = basePhonemeI - i;
                phonemes[i].phoneme = MapPhoneme(ValidateAlias(phonemeSymbols[i]), prevTone, singer);
                phonemes[i].position = -noteLengthTick * offset;
            }
            {
                phonemes[basePhonemeI].phoneme = MapPhoneme(ValidateAlias(phonemeSymbols[basePhonemeI]), note.tone, singer);
                phonemes[basePhonemeI].position = 0;
            }

            noteLengthTick = GetNoteLength(phonemeSymbols.Count - basePhonemeI - 1, note.duration);
            for (var i = basePhonemeI + 1; i < phonemeSymbols.Count; i++) {
                var offset = phonemeSymbols.Count - basePhonemeI - 1;
                phonemes[i].phoneme = MapPhoneme(ValidateAlias(phonemeSymbols[i]), note.tone, singer);
                phonemes[i].position = note.duration - noteLengthTick * offset;
            }
            return phonemes;
        }

    }
}

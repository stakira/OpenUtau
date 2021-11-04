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
            var phonemes = TrySyllable(note, prevNeighbour, prevV, cc, v);
            ValidatePositions(phonemes, note, prevNeighbour);

            if (!nextNeighbour.HasValue) {
                var lastConsonants = GetRightConsonants(symbols, false);
                TryEnding(phonemes, note, v, lastConsonants);
            }
            return new Result() {
                phonemes = phonemes.ToArray()
            };
        }

        public override void SetSinger(USinger singer) {
            this.singer = singer;
            Init();
        }

        protected USinger singer;
        protected double shortNoteThreshold = 120;

        protected abstract string[] GetVowels();
        protected virtual Dictionary<string, string> GetAliasesFallback() { return null; }
        protected virtual void Init() { }
        protected abstract List<Phoneme> TrySyllable(Note note, Note? prevNote, string prevV, string[] cc, string v);
        protected abstract void TryEnding(List<Phoneme> phonemes, Note note, string v, string[] cc);

        protected virtual string[] GetSymbols(Note note) {
            // dictionary is not yet supported so just read all lyrics as phonetic input
            if (note.lyric == null) {
                return new string[0];
            } else return note.lyric.Split(" ");
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

        private void ValidatePositions(List<Phoneme> phonemes, Note note, Note? prevNeighbour) {
            if (phonemes.Count <= 1) {
                return;
            }
            var noteLengthTick = GetNoteLength(phonemes.Count - 1, prevNeighbour.HasValue ? prevNeighbour.Value.duration : -1);
            for (var i = 0; i < phonemes.Count; i++) {
                var phonemeI = phonemes.Count - 1 - i;
                var phoneme = phonemes[phonemeI];
                phonemes[phonemeI] = new Phoneme() {
                    phoneme = phoneme.phoneme,
                    position = -noteLengthTick * i
                };
            }
        }

        protected int GetNoteLength(int phonemesCount, int containerLength = -1) {
            var noteLength = 120.0;
            if (containerLength != -1) {
                var maxVCLength = containerLength - 30;
                if (maxVCLength < noteLength * phonemesCount) {
                    noteLength = maxVCLength / phonemesCount;
                }
            }
            return MsToTick(noteLength) / 15 * 15;
        }

    }
}

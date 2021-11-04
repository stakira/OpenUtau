using System;
using System.Collections.Generic;
using System.Text;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using System.Linq;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Russian CVC Phonemizer", "RU CVC", "Heiden.BZR")]
    public class RussianCVCPhonemizer : Phonemizer {

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
        }

        private USinger singer;
        private readonly string[] vowels = "a,e,o,u,y,i,M,N".Split(",");

        private Dictionary<string, string> aliasesFallback = "ic=yc;y4'=y4;ij=yj".Split(';')
                .Select(entry => entry.Split('='))
                .ToDictionary(parts => parts[0], parts => parts[1]);

        private string[] burstConsonants = "t,t',k,k',p,p',4',c,b,b',g,g',d,d'".Split(",");

        private string[] GetSymbols(Note note) {
            // dictionary is not yet supported so just read all lyrics as phonetic input
            if (note.lyric == null) {
                return new string[0];
            } else return note.lyric.Split(" ");
        }

        private double shortNoteThreshold = 120;

        private string[] GetRightConsonants(string[] symbols, bool withV) {
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
            }
            else if (vowelIdx == 0 && symbols.Length == 1) {
                return new string[0];
            }
            else {
                return symbols.TakeLast(symbols.Length - vowelIdx - 1).ToArray();
            }
        }

        private string[] GetCV(string[] symbols) {
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

        private List<Phoneme> TrySyllable(Note note, Note? prevNote, string prevV, string[] cc, string v) {
            var basePhoneme = new Phoneme();
            var phonemes = new List<Phoneme>();
            var prevTone = prevNote.HasValue ? prevNote.Value.tone : note.tone;
            if (prevV == "") {
                if (cc.Length == 0) {
                    basePhoneme.phoneme = $"-{v}";
                }
                else {
                    basePhoneme.phoneme = $"-{cc.Last()}{v}";
                    for (var i = 0; i < cc.Length - 1; i++) {
                        phonemes.Add(new Phoneme() {
                            phoneme = MapPhoneme($"-{cc[i]}", prevTone, singer)
                        });
                    }
                }
                for (var i = 0; i < cc.Length - 2; i++) {
                    phonemes.Add(new Phoneme() {
                        phoneme = $"-{cc[i]}"
                    });
                }
            }
            else if (cc.Length == 0) {
                basePhoneme.phoneme = v;
            }
            else {
                if (cc.Length == 1 || TickToMs(note.duration) < shortNoteThreshold || cc.Last() == "`") {
                    basePhoneme.phoneme = $"{cc.Last()}{v}";
                }
                else {
                    basePhoneme.phoneme = $"-{cc.Last()}{v}";
                }
                phonemes.Add(new Phoneme() {
                    phoneme = MapPhoneme(ValidateAlias($"{prevV}{cc[0]}"), prevTone, singer)
                });
                var offset = burstConsonants.Contains(cc[0]) ? 0 : 1;
                for (var i = offset; i < cc.Length - 1; i++) {
                    phonemes.Add(new Phoneme() {
                        phoneme = MapPhoneme(cc[i], prevTone, singer)
                    });
                }
            }
            basePhoneme.phoneme = MapPhoneme(basePhoneme.phoneme, note.tone, singer);
            phonemes.Add(basePhoneme);
            return phonemes;
        }

        private void TryEnding(List<Phoneme> phonemes, Note note, string v, string[] cc) {
            var basePhonemeI = phonemes.Count - 1;
            if (cc.Length == 0) {
                phonemes.Add(new Phoneme() {
                    phoneme = MapPhoneme($"{v}-", note.tone, singer)
                });
            }
            else {
                if (cc[0] == "`") {
                    phonemes.Add(new Phoneme() {
                        phoneme = MapPhoneme(v + cc[0], note.tone, singer)
                    });
                }
                else {
                    phonemes.Add(new Phoneme() {
                        phoneme = MapPhoneme($"{ValidateAlias(v + cc[0])}-", note.tone, singer)
                    });
                }
                for (var i = 1; i < cc.Length; i++) {
                    phonemes.Add(new Phoneme() {
                        phoneme = MapPhoneme(cc[i], note.tone, singer)
                    });
                }
            }
            var noteLengthTick = GetNoteLength(phonemes.Count - basePhonemeI - 1, note.duration);
            var y = 0;
            for (var i = phonemes.Count - 1; i > basePhonemeI; i--) {
                y++;
                phonemes[i] = new Phoneme() {
                    phoneme = phonemes[i].phoneme,
                    position = note.duration - noteLengthTick * y
                };
            }
        }

        private string ValidateAlias(string alias) {
            return aliasesFallback.ContainsKey(alias) ? aliasesFallback[alias] : alias;
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

        private int GetNoteLength(int phonemesCount, int containerLength = -1) {
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

using System;
using System.Collections.Generic;
using System.Text;
using OpenUtau.Api;
using System.Linq;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Russian CVC Phonemizer", "RU CVC", "Heiden.BZR")]
    public class RussianCVCPhonemizer : AdvancedPhonemizer {

        private readonly string[] vowels = "a,e,o,u,y,i,M,N".Split(",");
        private Dictionary<string, string> aliasesFallback = "ic=yc;y4'=y4;ij=yj".Split(';')
                .Select(entry => entry.Split('='))
                .ToDictionary(parts => parts[0], parts => parts[1]);
        private string[] burstConsonants = "t,t',k,k',p,p',4',c,b,b',g,g',d,d'".Split(",");

        protected override string[] GetVowels() {
            return vowels;
        }

        protected override Dictionary<string, string> GetAliasesFallback() {
            return aliasesFallback;
        }

        protected override List<Phoneme> TrySyllable(Note note, Note? prevNote, string prevV, string[] cc, string v) {
            var basePhoneme = new Phoneme();
            var phonemes = new List<Phoneme>();
            var prevTone = prevNote.HasValue ? prevNote.Value.tone : note.tone;
            if (prevV == "") {
                if (cc.Length == 0) {
                    // -V
                    basePhoneme.phoneme = $"-{v}";
                }
                else {
                    // -CV
                    basePhoneme.phoneme = $"-{cc.Last()}{v}";
                    for (var i = 0; i < cc.Length - 1; i++) {
                        phonemes.Add(new Phoneme() {
                            // -C
                            phoneme = MapPhoneme($"-{cc[i]}", prevTone, singer)
                        });
                    }
                }
                for (var i = 0; i < cc.Length - 2; i++) {
                    phonemes.Add(new Phoneme() {
                        // -C
                        phoneme = $"-{cc[i]}"
                    });
                }
            }
            else if (cc.Length == 0) {
                // VV
                basePhoneme.phoneme = v;
            }
            else {
                if (cc.Length == 1 || TickToMs(note.duration) < shortNoteThreshold || cc.Last() == "`") {
                    // CV or `V
                    basePhoneme.phoneme = $"{cc.Last()}{v}";
                }
                else {
                    // -CV
                    basePhoneme.phoneme = $"-{cc.Last()}{v}";
                }
                phonemes.Add(new Phoneme() {
                    // VC
                    phoneme = MapPhoneme(ValidateAlias($"{prevV}{cc[0]}"), prevTone, singer)
                });
                var offset = burstConsonants.Contains(cc[0]) ? 0 : 1;
                for (var i = offset; i < cc.Length - 1; i++) {
                    phonemes.Add(new Phoneme() {
                        // C
                        phoneme = MapPhoneme(cc[i], prevTone, singer)
                    });
                }
            }
            basePhoneme.phoneme = MapPhoneme(basePhoneme.phoneme, note.tone, singer);
            phonemes.Add(basePhoneme);
            return phonemes;
        }

        protected override void TryEnding(List<Phoneme> phonemes, Note note, string v, string[] cc) {
            var basePhonemeI = phonemes.Count - 1;
            if (cc.Length == 0) {
                phonemes.Add(new Phoneme() {
                    // V-
                    phoneme = MapPhoneme($"{v}-", note.tone, singer)
                });
            }
            else {
                if (cc[0] == "`") {
                    phonemes.Add(new Phoneme() {
                        // V`
                        phoneme = MapPhoneme(v + cc[0], note.tone, singer)
                    });
                }
                else {
                    phonemes.Add(new Phoneme() {
                        // VC-
                        phoneme = MapPhoneme($"{ValidateAlias(v + cc[0])}-", note.tone, singer)
                    });
                }
                for (var i = 1; i < cc.Length; i++) {
                    phonemes.Add(new Phoneme() {
                        // C
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

    }
}

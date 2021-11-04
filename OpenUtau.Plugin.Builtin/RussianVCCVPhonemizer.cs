using System;
using System.Collections.Generic;
using System.Text;
using OpenUtau.Api;
using System.Linq;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Russian VCCV Phonemizer", "RU VCCV", "Heiden.BZR")]
    public class RussianVCCVPhonemizer : AdvancedPhonemizer {

        private readonly string[] vowels = "a,e,o,u,y,i,M,N,ex,ax,x".Split(",");
        private string[] burstConsonants = "t,t',k,k',p,p',ch,ts,b,b',g,g',d,d'".Split(",");

        protected override string[] GetVowels() {
            return vowels;
        }

        protected override List<Phoneme> TrySyllable(Note note, Note? prevNote, string prevV, string[] cc, string v) {
            var basePhoneme = new Phoneme();
            var phonemes = new List<Phoneme>();
            var prevTone = prevNote.HasValue ? prevNote.Value.tone : note.tone;
            if (prevV == "") {
                if (cc.Length == 0) {
                    // -V
                    basePhoneme.phoneme = $"- {v}";
                } else if (cc.Length == 1) {
                    // -CV or -C CV
                    var rcv = $"- {cc.Last()}{v}";
                    if (singer.TryGetMappedOto(rcv, note.tone, out _)) {
                        basePhoneme.phoneme = rcv;
                    }
                    else {
                        basePhoneme.phoneme = $"{cc.Last()}{v}";
                        phonemes.Add(new Phoneme() {
                            phoneme = $"- {cc.Last()}"
                        });
                    }
                } else {
                    // -C
                    basePhoneme.phoneme = $"{cc.Last()}{v}";
                    phonemes.Add(new Phoneme() {
                        phoneme = $"- {cc[0]}"
                    });
                }
            } else if (cc.Length == 0) {
                // VV
                basePhoneme.phoneme = $"{prevV} {v}";
            } else {
                // CV
                basePhoneme.phoneme = $"{cc.Last()}{v}";
                phonemes.Add(new Phoneme() {
                    // VC
                    phoneme = MapPhoneme(ValidateAlias($"{prevV} {cc[0]}"), prevTone, singer)
                });
            }
            for (var i = 0; i < cc.Length - 1; i++) {
                // CC
                phonemes.Add(new Phoneme() {
                    phoneme = $"{cc[i]} {cc[i + 1]}"
                });
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
                    phoneme = MapPhoneme($"{v} -", note.tone, singer)
                });
            } else if (cc.Length == 1) {
                if (cc[0] == "`") {
                    // V`
                    phonemes.Add(new Phoneme() {
                        phoneme = MapPhoneme(v + cc[0], note.tone, singer)
                    });
                } else {
                    // VC- or VC C-
                    var vcr = $"{ValidateAlias(v + cc[0])} -";
                    if (singer.TryGetMappedOto(vcr, note.tone, out _)) {
                        phonemes.Add(new Phoneme() {
                            phoneme = MapPhoneme($"{ValidateAlias(v + cc[0])} -", note.tone, singer)
                        });
                    }
                    else {
                        phonemes.Add(new Phoneme() {
                            phoneme = MapPhoneme($"{ValidateAlias(v + cc[0])}", note.tone, singer)
                        });
                        phonemes.Add(new Phoneme() {
                            phoneme = MapPhoneme($"{cc[0]} -", note.tone, singer)
                        });
                    }
                }
            } else {
                phonemes.Add(new Phoneme() {
                    // VC
                    phoneme = MapPhoneme($"{ValidateAlias(v + cc[0])}", note.tone, singer)
                });
                if (burstConsonants.Contains(cc.Last())) {
                    // C-
                    phonemes.Add(new Phoneme() {
                        // VC
                        phoneme = MapPhoneme($"{cc.Last()} -", note.tone, singer)
                    });
                }
                for (var i = 0; i < cc.Length - 1; i++) {
                    // CC
                    phonemes.Add(new Phoneme() {
                        phoneme = MapPhoneme($"{cc[i]} {cc[i + 1]}", note.tone, singer)
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

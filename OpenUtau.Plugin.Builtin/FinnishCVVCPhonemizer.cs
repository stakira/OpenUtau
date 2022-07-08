using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenUtau.Api;

namespace OpenUtau.Plugin.Builtin {
    /// <summary>
    /// Original phonemizer by Heiden.BZR
    /// Reclist by 
    /// </summary>
    [Phonemizer("Finnish CVVC Phonemizer", "FI CVVC", "nago")]
    public class FinnishCVVCPhonemizer : SyllableBasedPhonemizer {

        private readonly string[] vowels = "a e i o u y A O".Split(" ");
        protected override string[] GetVowels() => vowels;

        protected override List<string> ProcessSyllable(Syllable syllable) {
            string prevV = syllable.prevV;
            string[] cc = syllable.cc;
            string v = syllable.v;

            string basePhoneme;
            var phonemes = new List<string>();
            if (syllable.IsStartingV) {
                basePhoneme = $"- {v}";
            }
            else if (syllable.IsStartingCV) {
                basePhoneme = $"{cc.Last()}{v}";
                phonemes.Add($"- {cc[0]}");
                for (var i = 0; i < cc.Length - 1; i++) {
                    phonemes.Add($"{cc[i]}");
                }
            }
            else if (syllable.IsVV) {
                if (!CanMakeAliasExtension(syllable)) {
                    basePhoneme = $"{prevV} {v}";
                } else {
                    // the previous alias will be extended
                    basePhoneme = null;
                }
            } else { // VCV
                basePhoneme = $"{cc.Last()}{v}";
                phonemes.Add($"{prevV} {cc[0]}"); ;
                for (var i = 0; i < cc.Length - 1; i++) {
                    phonemes.Add($"{cc[0]} {cc[1]}");

                }
            }
            phonemes.Add(basePhoneme);
            return phonemes;
        }

        protected override List<string> ProcessEnding(Ending ending) {
            string[] cc = ending.cc;
            string v = ending.prevV;

            var phonemes = new List<string>();
            if (ending.IsEndingV) {
                phonemes.Add($"{v} -");
            } else {
                phonemes.Add($"{v} {cc[0]}");
                phonemes.Add($"{cc[0]} -");
                for (var i = 1; i < cc.Length; i++) {
                    var cr = $"{cc[i]} -";
                    phonemes.Add(HasOto(cr, ending.tone) ? cr : cc[i]);
                }
            }

            return phonemes;
        }
    }
} 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenUtau.Api;

namespace OpenUtau.Plugin.Builtin {
    /// <summary>
    /// And example of realization of AdvancedPhonemizer without a dictionary
    /// Reclist by haku
    /// </summary>
    [Phonemizer("Polish CVC Phonemizer", "PL CVC", "Heiden.BZR")]
    public class PolishCVCPhonemizer : AdvancedPhonemizer {

        private readonly string[] vowels = "a A e E i o u y".Split(" ");
        protected override string[] GetVowels() => vowels;

        protected override List<string> TrySyllable(Syllable syllable) {
            string prevV = syllable.prevV;
            string[] cc = syllable.cc;
            string v = syllable.v;

            string basePhoneme;
            var phonemes = new List<string>();
            if (prevV == "") {
                if (cc.Length == 0) {
                    basePhoneme = $"- {v}";
                } else {
                    basePhoneme = $"- {cc.Last()}{v}";
                }
                for (var i = 0; i < cc.Length - 1; i++) {
                    phonemes.Add($"- {cc[i]}");
                }
            } else if (cc.Length == 0) {
                basePhoneme = v;
            } else {
                basePhoneme = $"{cc.Last()}{v}";
                phonemes.Add($"{prevV} {cc[0]}"); ;
                for (var i = 0; i < cc.Length - 1; i++) {
                    phonemes.Add(cc[i]);
                }
            }
            phonemes.Add(basePhoneme);
            return phonemes;
        }

        protected override List<string> TryEnding(Ending ending) {
            string[] cc = ending.cc;
            string v = ending.prevV;

            var phonemes = new List<string>();
            if (cc.Length == 0) {
                phonemes.Add($"{v} -");
            } else {
                phonemes.Add($"{v} {cc[0]}-");
                for (var i = 1; i < cc.Length; i++) {
                    var cr = $"{cc[i]} -";
                    phonemes.Add(HasOto(cr, ending.tone) ? cr : cc[i]);
                }
            }

            return phonemes;
        }
    }
}

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

        protected override List<string> TrySyllable(Note note, string prevV, string[] cc, string v) {
            string basePhoneme;
            var phonemes = new List<string>();
            if (prevV == "") {
                if (cc.Length == 0) {
                    basePhoneme = $"- {v}";
                } else if (cc.Length == 1) {
                    // -CV or -C CV
                    var rcv = $"- {cc[0]}{v}";
                    if (HasOto(rcv, note)) {
                        basePhoneme = rcv;
                    }
                    else {
                        basePhoneme = $"{cc[0]}{v}";
                        phonemes.Add($"- {cc[0]}");
                    }
                } else {
                    basePhoneme = $"{cc.Last()}{v}";
                    phonemes.Add($"- {cc[0]}");
                }
            } else if (cc.Length == 0) {
                basePhoneme = $"{prevV} {v}";
            } else {
                basePhoneme = $"{cc.Last()}{v}";
                phonemes.Add($"{prevV} {cc[0]}");
            }
            for (var i = 0; i < cc.Length - 1; i++) {
                phonemes.Add($"{cc[i]} {cc[i + 1]}");
            }
            phonemes.Add(basePhoneme);
            return phonemes;
        }

        protected override void TryEnding(List<string> phonemes, Note note, string v, string[] cc) {
            if (cc.Length == 0) {
                phonemes.Add($"{v} -");
            } else if (cc.Length == 1) {
                // VC- or VC C-
                var vcr = $"{ValidateAlias(v + cc[0])} -";
                if (HasOto(vcr, note)) {
                    phonemes.Add(vcr);
                } else {
                    phonemes.Add(v + cc[0]);
                    phonemes.Add($"{cc[0]} -");
                }
            } else {
                phonemes.Add(v + cc[0]);
                if (burstConsonants.Contains(cc.Last())) {
                    phonemes.Add($"{cc.Last()} -");
                }
                for (var i = 0; i < cc.Length - 1; i++) {
                    phonemes.Add($"{cc[i]} {cc[i + 1]}");
                }
            }
        }
    }
}

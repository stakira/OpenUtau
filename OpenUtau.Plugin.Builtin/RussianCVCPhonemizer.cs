using System;
using System.Collections.Generic;
using System.Text;
using OpenUtau.Api;
using System.Linq;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Russian CVC Phonemizer", "RU CVC", "Heiden.BZR")]
    public class RussianCVCPhonemizer : AdvancedPhonemizer {

        private readonly string[] vowels = "a,e,o,u,y,i,M,N".Split(",");
        private Dictionary<string, string> aliasesFallback = "ic=yc;y4'=y4;ij=yj;ic-=yc-;y4'-=y4-;ij-=yj-".Split(';')
                .Select(entry => entry.Split('='))
                .ToDictionary(parts => parts[0], parts => parts[1]);
        private string[] burstConsonants = "t,t',k,k',p,p',4',c,b,b',g,g',d,d'".Split(",");

        protected override string[] GetVowels() {
            return vowels;
        }

        protected override Dictionary<string, string> GetAliasesFallback() {
            return aliasesFallback;
        }

        protected override List<string> TrySyllable(Note note, string prevV, string[] cc, string v) {
            string basePhoneme;
            var phonemes = new List<string>();
            if (prevV == "") {
                if (cc.Length == 0) {
                    basePhoneme = $"-{v}";
                }
                else {
                    basePhoneme = $"-{cc.Last()}{v}";
                }
                for (var i = 0; i < cc.Length - 2; i++) {
                    phonemes.Add($"-{cc[i]}");
                }
            }
            else if (cc.Length == 0) {
                basePhoneme = v;
            }
            else {
                if (cc.Length == 1 || TickToMs(note.duration) < shortNoteThreshold || cc.Last() == "`") {
                    basePhoneme = $"{cc.Last()}{v}";
                }
                else {
                    basePhoneme = $"-{cc.Last()}{v}";
                }
                phonemes.Add($"{prevV}{cc[0]}"); ;
                var offset = burstConsonants.Contains(cc[0]) ? 0 : 1;
                for (var i = offset; i < cc.Length - 1; i++) {
                    phonemes.Add(cc[i]);
                }
            }
            phonemes.Add(basePhoneme);
            return phonemes;
        }

        protected override void TryEnding(List<string> phonemes, Note note, string v, string[] cc) {
            if (cc.Length == 0) {
                phonemes.Add($"{v}-");
            }
            else {
                phonemes.Add($"{v}{cc[0]}-");
                for (var i = 1; i < cc.Length; i++) {
                    phonemes.Add(cc[i]);
                }
            }
        }

    }
}

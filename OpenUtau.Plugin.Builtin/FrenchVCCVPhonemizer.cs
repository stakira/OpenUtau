using System;
using System.Collections.Generic;
using System.Text;
using OpenUtau.Api;
using System.Linq;


namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("French VCCV m2RUg Phonemizer", "FR VCCV", "Mim")]
    // This is Phonemizer 

    public class FrenchVCCVPhonemizer : SyllableBasedPhonemizer {

        private readonly string[] vowels = "A,E,e,2,9,i,o,O,u,y,a,U,0".Split(",");
        private readonly string[] consonants = "b,d,f,g,Z,k,l,m,n,p,R,s,S,t,v,w,j,z,J,H".Split(",");
        private readonly Dictionary<string, string> dictionaryReplacements = (
            "aa=A;ai=E;ei=e;eu=2;ee=2;oe=9;ii=i;au=o;oo=O;ou=u;uu=y;an=a;in=U;un=U;on=0;uy=H;" +
            "bb=b;dd=d;ff=f;gg=g;jj=Z;kk=k;ll=l;mm=m;nn=n;pp=p;rr=R;ss=s;ch=S;tt=t;vv=v;ww=w;yy=j;zz=z;gn=J;").Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);


        //private string[] shortConsonants = "R".Split(",");
        //private string[] longConsonants = "t,k,g,p,s,S,Z".Split(",");

        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => consonants;
        protected override string GetDictionaryName() => "cmudict_fr.txt";
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => dictionaryReplacements;

        protected override List<string> ProcessSyllable(Syllable syllable) {
            string prevV = syllable.prevV;
            string[] cc = syllable.cc;
            string v = syllable.v;
            var lastC = cc.Length - 1;
            var firstC = 0;

            string basePhoneme;
            var phonemes = new List<string>();


            // --------------------------- STARTING V ------------------------------- //
            if (syllable.IsStartingV) {
                basePhoneme = $"- {v}";

            } else if (syllable.IsVV) {  // if VV
                if (!CanMakeAliasExtension(syllable)) {
                    basePhoneme = $"{prevV} {v}";
                } else {
                    // the previous alias will be extended
                    basePhoneme = null;
                }
                // --------------------------- STARTING CV ------------------------------- //
            } else if (syllable.IsStartingCVWithOneConsonant) {

                basePhoneme = $"- {cc[0]}{v}";
                if(!HasOto(basePhoneme,syllable.tone)) {
                    TryAddPhoneme(phonemes, syllable.tone, $"- {cc[0]}");
                    basePhoneme = $"{cc[0]}{v}";
                }

                // --------------------------- STARTING CCV ------------------------------- //
            } else if (syllable.IsStartingCVWithMoreThanOneConsonant) {
                phonemes.Add($"- {cc[0]}");

                for (int i = 0; i < cc.Length - 1; i++) {
                    var cci = $"{cc[i]} {cc[i + 1]}";
                    
                    if (!HasOto(cci, syllable.tone)) {
                        cci = $"{cc[i]}{cc[i + 1]}_";
                    }

                    TryAddPhoneme(phonemes, syllable.tone, cci);
                }

                basePhoneme = $"{cc.Last()}{v}";
            }
                // --------------------------- IS VCV ------------------------------- //
                else if (syllable.IsVCVWithOneConsonant) {

                // try VCV
                var vc = $"{prevV} {cc[0]}";
                phonemes.Add(vc);

                basePhoneme = $"{cc[0]}{v}";
            } else {
                // ------------- IS VCV WITH MORE THAN ONE CONSONANT --------------- //
                var vc = $"{prevV} {cc[0]}";
                phonemes.Add(vc);

                for (int i = 0; i < cc.Length - 1; i++) {
                    var cci = $"{cc[i]}{cc[i + 1]}_"; 
                    if (!HasOto(cci, syllable.tone)) {
                        cci = $"{cc[i]} {cc[i + 1]}";
                    }

                    TryAddPhoneme(phonemes, syllable.tone, cci);
                }

                basePhoneme = $"{cc.Last()}{v}";
            }

            phonemes.Add(basePhoneme);
            return phonemes;
        }
        protected override List<string> ProcessEnding(Ending ending) {
            string[] cc = ending.cc;
            string v = ending.prevV;

            var phonemes = new List<string>();

            // --------------------------- ENDING V ------------------------------- //
            if (ending.IsEndingV) {
                var vE = $"{v} -";
                phonemes.Add(vE);

            } else {
                // --------------------------- ENDING VC ------------------------------- //
                if (ending.IsEndingVCWithOneConsonant) {

                    var vc = $"{v} {cc[0]}";
                    var cE = $"{cc[0]} -";
                    phonemes.Add(vc);
                    phonemes.Add(cE);

                } else {

                    // --------------------------- ENDING VCC ------------------------------- //
                    var vc = $"{v} {cc[0]}";
                    phonemes.Add(vc);

                    for (int i = 0; i < cc.Length - 1; i++) {
                        var cci = $"{cc[i]} {cc[i + 1]}";
                        if (!HasOto(cci,ending.tone)) {
                            cci = $"{cc[i]}{cc[i + 1]}_";
                        }

                        TryAddPhoneme(phonemes, ending.tone, cci);
                    }

                    var cE = $"{cc.Last()} -";
                    TryAddPhoneme(phonemes, ending.tone, cE);
                }


            }

            // ---------------------------------------------------------------------------------- //

            return phonemes;
        }


        //protected override double GetTransitionBasicLengthMs(string alias = "") {
        //    foreach (var c in shortConsonants) {
        //        if (alias.EndsWith(c)) {
        //            return base.GetTransitionBasicLengthMs() * 0.75;
        //        }
        //    }
        //    foreach (var c in longConsonants) {
        //        if (alias.EndsWith(c)) {
        //            return base.GetTransitionBasicLengthMs() * 1.5;
        //        }
        //    }
        //    return base.GetTransitionBasicLengthMs() * 1.25;
        //}


    }
}

using System;
using System.Collections.Generic;
using System.Text;
using OpenUtau.Api;
using System.Linq;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("French CVVC Phonemizer", "FR CVVC", "Heiden.BZR, nago & Mim")]
    public class FrenchCVVCPhonemizer : SyllableBasedPhonemizer {

        private readonly string[] vowels = "ah,ae,eh,ee,oe,ih,oh,oo,ou,uh,en,in,on,oi,ui".Split(",");
        private readonly string[] consonants = "b,d,f,g,j,k,l,m,n,p,r,s,sh,t,v,w,y,z".Split(",");
        private readonly Dictionary<string, string> dictionaryReplacements = (
            "aa=ah;ai=ae;ei=eh;eu=ee;ee=ee;oe=oe;ii=ih;au=oh;oo=oo;ou=ou;uu=uh;an=en;in=in;un=in;on=on;uy=ui;" +
            "bb=b;dd=d;ff=f;gg=g;jj=j;kk=k;ll=l;mm=m;nn=n;pp=p;rr=r;ss=s;ch=sh;tt=t;vv=v;ww=w;yy=y;zz=z").Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);


        private string[] shortConsonants = "r".Split(",");
        private string[] longConsonants = "t,k,g,p,s,sh,j".Split(",");
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
                // if starting V -> -V
                basePhoneme = $"-{v}";

                //if no -V -> V
                if (!HasOto(basePhoneme, syllable.vowelTone)) {
                    basePhoneme = v;
                }

            // --------------------------- STARTING VV ------------------------------- //
            } else if (syllable.IsVV) {  // if VV
                if (!CanMakeAliasExtension(syllable)) {
                    //try V V
                    basePhoneme = $"{prevV} {v}";

                    //if no V V -> _V
                    if (!HasOto(basePhoneme, syllable.vowelTone)) {
                        basePhoneme = $"_{v}";

                        //if no _V -> V
                        if (!HasOto(basePhoneme, syllable.vowelTone)) {
                            basePhoneme = v;
                        }
                    }
                } else {
                    // the previous alias will be extended
                    basePhoneme = null;
                }
                // --------------------------- STARTING CV ------------------------------- //
            } else if (syllable.IsStartingCVWithOneConsonant) {
                //if starting CV -> -CV
                basePhoneme = $"-{cc[0]}{v}";

                if (!HasOto(basePhoneme, syllable.vowelTone)) {

                    //else -CV -> CV
                    basePhoneme = $"{cc[0]}{v}";

                    //try -C + CV
                    var sc = $"-{cc[0]}";
                    if (HasOto(sc, syllable.vowelTone)) {
                        if (consonants.Contains(cc[0])) {
                            phonemes.Add(sc);
                        }
                    }
                }
            // --------------------------- STARTING CCV ------------------------------- //
            } else if (syllable.IsStartingCVWithMoreThanOneConsonant) {
                // try -CCV
                var rccv = $"-{string.Join("", cc)}{v}";
                if (HasOto(rccv, syllable.vowelTone)) {
                    basePhoneme = rccv;
                } else {
                    //try _CV else add CV
                    if (HasOto($"_{cc.Last()}{v}", syllable.vowelTone) && cc.Length > 1) {
                        basePhoneme = $"_{cc.Last()}{v}";
                    } 
                    else { basePhoneme = $"{cc.Last()}{v}"; }
                    // try -CC of all lengths
                    for (var i = cc.Length; i > 1; i--) {
                        if (TryAddPhoneme(phonemes, syllable.tone, $"-{string.Join("", cc.Take(i))}")) {
                            firstC = i;
                            break;
                        }
                    }
                    // then try CC of all lengths
                    if (phonemes.Count == 0) {
                        for (var i = cc.Length; i > 1; i--) {
                            if (TryAddPhoneme(phonemes, syllable.tone, $"{string.Join("", cc.Take(i))}")) {
                                firstC = i;
                                break;
                            }
                        }
                    }

                    if (phonemes.Count == 0) {
                        // add -C
                        if (!TryAddPhoneme(phonemes, syllable.tone, $"-{cc[0]}")) {
                            // add -Coe
                            if (!TryAddPhoneme(phonemes, syllable.tone, $"-{cc[0]}oe")) {
                                //add Coe
                                phonemes.Add($"{cc[0]}oe");
                            }
                        }

                    }


                    // try CCV - needs better implementation
                    //string ccv = $"{cc[0]}{cc[1]}{v}";
                    //if (cc.Length == 2 && HasOto(ccv, syllable.vowelTone)) {
                    //    basePhoneme = ccv;
                    //}

                }
            }
                // --------------------------- IS VCV ------------------------------- //
                else {
                //try _CV else add CV
                if (HasOto($"_{cc.Last()}{v}", syllable.vowelTone) && cc.Length > 1) {
                    basePhoneme = $"_{cc.Last()}{v}";
                } else { basePhoneme = $"{cc.Last()}{v}"; }
                // try VCC else VC
                for (var i = lastC + 1; i >= 0; i--) {
                    //if (i == 0) {
                    //    phonemes.Add($"{prevV}-");
                    //    break;
                    //}
                    var vcc = $"{prevV}{string.Join("", cc.Take(i))}";
                    if (HasOto(vcc, syllable.tone)) {
                        phonemes.Add(vcc);
                        firstC = i - 1;
                        break;
                    }
                }
                if (phonemes.Count==0) { 
                phonemes.Add($"{prevV}{cc[0]}");
                }


                bool usesCC = false;
                // NEEDS BETTER IMPLEMENTATION this is a first loop that checks if it uses CC 
                for (var i = 0; i < cc.Length - 1; i++) {
                    var currentCc = $"{cc[i]}{cc[i + 1]}";

                    if (!HasOto(currentCc, syllable.tone) && cc[i + 1] != cc.Last()) {
                        // french exclusion of "w" consonant
                        if ($"{cc[i + 1]}" == "w") {
                            continue;
                        }
                        continue;
                    }
                    if (!HasOto(currentCc, syllable.tone)) {
                        continue;
                    }

                    usesCC = true;
                }

                for (var i = 0; i < cc.Length - 1; i++) {
                    var currentCc = $"{cc[i]}{cc[i + 1]}";

                    if (!HasOto(currentCc, syllable.tone) && cc[i + 1] != cc.Last() && !usesCC) {
                        // french exclusion of "w" consonant
                        if ($"{cc[i + 1]}" == "w") {
                            continue;
                        }
                        phonemes.Add($"{cc[i + 1]}oe");
                        continue;
                    }
                    if (!HasOto(currentCc, syllable.tone)) {
                        continue;
                    }

                    usesCC = true;
                    phonemes.Add(currentCc);
                }

                //try CCV
                if (!usesCC) {
                    for (var i = firstC; i < cc.Length - 1; i++) {
                        var ccv = string.Join("", cc.Skip(i)) + v;
                        if (HasOto(ccv, syllable.tone)) {
                            basePhoneme = ccv;
                            break;
                        }
                    }
                }
                
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
                // try V- else no ending
                TryAddPhoneme(phonemes, ending.tone, $"{v}-");

            } else {
            // --------------------------- ENDING VC ------------------------------- //
                if (ending.IsEndingVCWithOneConsonant) {
                    if (!TryAddPhoneme(phonemes, ending.tone, $"{v}{cc[0]}-")) {
                        // add V C
                        phonemes.Add($"{v}{cc[0]}");
                    }
                } else {

             // --------------------------- ENDING VCC ------------------------------- //
                    if (!TryAddPhoneme(phonemes, ending.tone, $"{v}{cc[0]}{cc[1]}-")) {
                        if (!TryAddPhoneme(phonemes, ending.tone, $"{v}{cc[0]}{cc[1]}")) {
                            phonemes.Add($"{v}{cc[0]}");
                        }
                    }

                    // add C1C2 or C2oe
                    for (var i = 0; i < cc.Length - 1; i++) {
                        var currentCc = $"{cc[i]}{cc[i + 1]}";
                        if (!HasOto(currentCc, ending.tone)) {
                            // french exclusion of "w" consonant
                            if ($"{cc[i + 1]}" == "w") {
                                continue;
                            }
                            phonemes.Add($"{cc[i + 1]}oe");
                            continue;
                        }
                        phonemes.Add(currentCc);
                    }

                }

                // add Last C
                if (cc.Length > 1) {
                    if (!HasOto($"{v}{cc[0]}{cc[1]}-", ending.tone)) {
                        TryAddPhoneme(phonemes, ending.tone, $"{cc.Last()}-");
                    }
                }
                if (!HasOto($"{v}{cc[0]}-", ending.tone)) {
                    TryAddPhoneme(phonemes, ending.tone, $"{cc.Last()}-");
                }
            }

            // ---------------------------------------------------------------------------------- //

            return phonemes;
        }

        protected override double GetTransitionBasicLengthMs(string alias = "") {
            foreach (var c in shortConsonants) {
                if (alias.EndsWith(c)) {
                    return base.GetTransitionBasicLengthMs() * 0.75;
                }
            }
            foreach (var c in longConsonants) {
                if (alias.EndsWith(c)) {
                    return base.GetTransitionBasicLengthMs() * 1.5;
                }
            }
            return base.GetTransitionBasicLengthMs() * 1.25;
        }

    }
}

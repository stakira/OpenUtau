using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenUtau.Api;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("English Delta Phonemizer", "EN DELTA", "nago & Heiden.BZR")]
    public class EnglishDeltaPhonemizer : SyllableBasedPhonemizer {

        private readonly string[] vowels = "a,A,@,{,V,O,aU,aI,E,3,eI,I,i,oU,OI,U,u".Split(",");
        private readonly string[] consonants = "b,tS,d,D,f,g,h,dZ,k,l,m,n,N,p,r,s,S,t,T,v,w,j,z,Z".Split(",");
        private readonly Dictionary<string, string> dictionaryReplacements = ("AA=A;AA0=A;AA1=A;AA2=A;AE={;AE0={;AE1={;AE2={;AH=V;AH0=V;AH1=V;" +
        "AH2=V;AO=O;AO0=O;AO1=O;AO2=O;AW=aU;AW0=aU;AW1=aU;AW2=aU;AY=aI;AY0=aI;AY1=aI;AY2=aI;B=b;CH=tS;D=d;DH=D;EH=E;EH0=E;EH1=E;EH2=E;ER=3;" +
        "ER0=3;ER1=3;ER2=3;EY=eI;EY0=eI;EY1=eI;EY2=eI;F=f;G=g;HH=h;IH=I;IH0=I;IH1=I;IH2=I;IY=i;IY0=i;IY1=i;IY2=i;JH=dZ;K=k;L=l;M=m;N=n;NG=N;" +
        "OW=oU;OW0=oU;OW1=oU;OW2=oU;OY=OI;OY0=OI;OY1=OI;OY2=OI;P=p;R=r;S=s;SH=S;T=t;TH=T;UH=U;UH0=U;UH1=U;UH2=U;UW=u;UW0=u;UW1=u;UW2=u;V=v;" +
        "W=w;Y=j;Z=z;ZH=Z").Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);

        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => consonants;
        protected override string GetDictionaryName() => "cmudict-0_7b.txt";
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => dictionaryReplacements;

        protected override string ReadDictionary(string filename) {
            return Core.Api.Resources.cmudict_0_7b;
        }

        protected override List<string> ProcessSyllable(Syllable syllable) {
            string prevV = syllable.prevV;
            string[] cc = syllable.cc;
            string v = syllable.v;

            string basePhoneme;
            var phonemes = new List<string>();
            var lastC = cc.Length - 1;
            var firstC = 0;
            if (syllable.IsStartingV) {
                basePhoneme = $"- {v}";
            } else if (syllable.IsVV) {
                basePhoneme = $"{prevV} {v}";
                if (!HasOto(basePhoneme, syllable.vowelTone)) {
                    basePhoneme = $"- {v}";
                    phonemes.Add($"{prevV} -");
                }
            } else if (syllable.IsStartingCVWithOneConsonant) {
                // TODO: move to config -CV or -C CV
                var rc = $"- {cc[0]}{v}";
                if (HasOto(rc, syllable.vowelTone)) {
                    basePhoneme = rc;
                } else {
                    basePhoneme = $"{cc[0]}{v}";
                    if (consonants.Contains(cc[0])) {
                        phonemes.Add($"- {cc[0]}");
                    }
                }
            } else if (syllable.IsStartingCVWithMoreThanOneConsonant) {
                // try RCCV
                var rvvc = $"- {string.Join("", cc)}{v}";
                if (HasOto(rvvc, syllable.vowelTone)) {
                    basePhoneme = rvvc;
                } else {
                    basePhoneme = $"{cc.Last()}{v}";
                    // try RCC
                    for (var i = cc.Length; i > 1; i--) {
                        if (TryAddPhoneme(phonemes, syllable.tone, $"- {string.Join("", cc.Take(i))}")) {
                            firstC = i;
                            break;
                        }
                    }
                    if (phonemes.Count == 0) {
                        TryAddPhoneme(phonemes, syllable.tone, $"- {cc[0]}");
                    }
                    // try CCV
                    for (var i = firstC; i < cc.Length - 1; i++) {
                        var ccv = string.Join("", cc.Skip(i)) + v;
                        if (HasOto(ccv, syllable.tone)) {
                            basePhoneme = ccv;
                            lastC = i;
                            break;
                        }
                    }
                }
            } else { // VCV
                var vcv = $"{prevV} h{v}";
                if (cc[0] == "h" && HasOto(vcv, syllable.vowelTone)) {
                    basePhoneme = vcv;
                } else {
                    // try vcc
                    for (var i = lastC + 1; i >= 0; i--) {
                        if (i == 0) {
                            phonemes.Add($"{prevV} -");
                            break;
                        }
                        var vcc = $"{prevV} {string.Join("", cc.Take(i))}";
                        if (HasOto(vcc, syllable.tone)) {
                            phonemes.Add(vcc);
                            firstC = i - 1;
                            break;
                        }
                    }
                    basePhoneme = cc.Last() + v;
                    // try CCV
                    if (cc.Length - firstC > 1) {
                        for (var i = firstC; i < cc.Length; i++) {
                            var ccv = $"{string.Join("", cc.Skip(i))}{v}";
                            if (HasOto(ccv, syllable.vowelTone)) {
                                lastC = i;
                                basePhoneme = ccv;
                                break;
                            }
                        }
                    }
                }
            }
            for (var i = firstC; i < lastC; i++) {
                // we could use some CCV, so lastC is used
                // we could use -CC so firstC is used
                var cc1 = $"{cc[i]} {cc[i + 1]}";
                if (!HasOto(cc1, syllable.tone)) {
                    cc1 = $"{cc[i]}{cc[i + 1]}";
                }
                if (i + 1 < lastC) {
                    var cc2 = $"{cc[i + 1]} {cc[i + 2]}";
                    if (!HasOto(cc2, syllable.tone)) {
                        cc2 = $"{cc[i + 1]}{cc[i + 2]}";
                    }
                    if (HasOto(cc1, syllable.tone) && HasOto(cc2, syllable.tone)) {
                        // like [V C1] [C1 C2] [C2 C3] [C3 ..]
                        phonemes.Add(cc1);
                    } else if (TryAddPhoneme(phonemes, syllable.tone, $"{cc[i]} {cc[i + 1]}-")) {
                        // like [V C1] [C1 C2-] [C3 ..]
                        i++;
                    } else if (TryAddPhoneme(phonemes, syllable.tone, cc1)) {
                        // like [V C1] [C1 C2] [C2 ..]
                    } else {
                        // like [V C1] [C1] [C2 ..]
                        TryAddPhoneme(phonemes, syllable.tone, cc[i], $"{cc[i]} -");
                    }
                } else {
                    // like [V C1] [C1 C2]  [C2 ..] or like [V C1] [C1 -] [C3 ..]
                    TryAddPhoneme(phonemes, syllable.tone, cc1, cc[i], $"{cc[i]} -");
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
            } else if (ending.IsEndingVCWithOneConsonant) {
                var vcr = $"{v} {cc[0]}-";
                if (HasOto(vcr, ending.tone)) {
                    phonemes.Add(vcr);
                } else {
                    phonemes.Add($"{v} {cc[0]}");
                    TryAddPhoneme(phonemes, ending.tone, $"{cc[0]} -", cc[0]);
                }
            } else {
                phonemes.Add($"{v} {cc[0]}");
                // all CCs except the first one are /C1C2/, the last one is /C1 C2-/
                // but if there is no /C1C2/, we try /C1 C2-/, vise versa for the last one
                for (var i = 0; i < cc.Length - 1; i++) {
                    var cc1 = $"{cc[i]} {cc[i + 1]}";
                    if (i < cc.Length - 2) {
                        var cc2 = $"{cc[i + 1]} {cc[i + 2]}";
                        if (HasOto(cc1, ending.tone) && HasOto(cc2, ending.tone)) {
                            // like [C1 C2][C2 ...]
                            phonemes.Add(cc1);
                        } else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]}{cc[i + 2]}")) {
                            // like [C1C2][C2 ...]
                        } else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} {cc[i + 2]}-")) {
                            // like [C1 C2-][C3 ...]
                            i++;
                        } else {
                            // like [C1][C2 ...]
                            TryAddPhoneme(phonemes, ending.tone, cc[i], $"{cc[i]} -");
                        }
                    } else {
                        if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i]} {cc[i + 1]}-")) {
                            // like [C1 C2-]
                            i++;
                        } else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i]}{cc[i + 1]}")) {
                            // like [C1C2][C2 -]
                            TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} -", cc[i + 1]);
                            i++;
                        } else if (TryAddPhoneme(phonemes, ending.tone, cc1)) {
                            // like [C1 C2][C2 -]
                            TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} -", cc[i + 1]);
                            i++;
                        } else {
                            // like [C1][C2 -]
                            TryAddPhoneme(phonemes, ending.tone, cc[i], $"{cc[i]} -");
                            TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} -", cc[i + 1]);
                            i++;
                        }
                    }
                }
            }
            return phonemes;
        }
    }
}

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
        private readonly Dictionary<string, string> dictionaryReplacements = ("aa=A;ae={;ah=V;ao=O;aw=aU;ay=aI;" +
            "b=b;ch=tS;d=d;dh=D;eh=E;er=3;ey=eI;f=f;g=g;hh=h;ih=I;iy=i;jh=dZ;k=k;l=l;m=m;n=n;ng=N;ow=oU;oy=OI;" +
            "p=p;r=r;s=s;sh=S;t=t;th=T;uh=U;uw=u;v=v;w=w;y=j;z=z;zh=Z").Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);

        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => consonants;
        protected override string GetDictionaryName() => "cmudict-0_7b.txt";
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => dictionaryReplacements;

        protected override IG2p LoadBaseDictionary() => new ArpabetG2p();

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
                if (cc[0] == "h" && HasOto(vcv, syllable.vowelTone) && (syllable.IsVCVWithOneConsonant)) {
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

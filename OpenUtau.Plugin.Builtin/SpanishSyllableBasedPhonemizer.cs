using System;
using System.Collections.Generic;
using System.Text;
using OpenUtau.Api;
using System.Linq;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Spanish Syllable-Based Phonemizer", "ES SYL", "Lotte V", language: "ES")]
    public class SpanishSyllableBasedPhonemizer : SyllableBasedPhonemizer {

        /// <summary>
        /// Spanish syllable-based phonemizer.
        /// I tried to make this phonemizer as compatible with many different methods as possible.
        /// Supports both CVVC and VCV if the voicebank has it.
        /// Supports seseo ("s" instead of "z" if the voicebank doesn't have the latter).
        /// It also substitutes "nh" for "ny" if the voicebank doesn't have the first.
        /// Ít now also uses "i" instead of "y" and "u" instead of "w" depending on what the voicebank supports.
        /// Now with full VCV support, including "consonant VCV" if the voicebank has either of them (ex. "l ba", "n da" but also "m bra" etc.).
        ///</summary>

        private readonly string[] vowels = "a,e,i,o,u".Split(',');
        private readonly string[] consonants = "b,ch,d,dz,f,g,h,hh,j,k,l,ll,m,n,nh,p,r,rr,s,sh,t,ts,w,y,z,zz,zh".Split(',');
        private readonly Dictionary<string, string> dictionaryReplacements = ("a=a;e=e;i=i;o=o;u=u;" +
                "b=b;ch=ch;d=d;dz=dz;f=f;g=g;gn=nh;k=k;l=l;ll=j;m=m;n=n;p=p;r=r;rr=rr;s=s;sh=sh;t=t;ts=ts;w=w;x=h;y=y;z=z;zz=zz;zh=zh;I=i;U=u;B=b;D=d;G=g;Y=y").Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);

        private readonly string[] longConsonants = "ch,dz,h,s,sh,k,p,rr,t,ts,z".Split(',');
        private readonly string[] notClusters = "dz,hh,ll,nh,sh,zz,zh".Split(',');
        private readonly string[] specialClusters = "by,dy,fy,gy,hy,jy,ky,ly,my,py,ry,rry,sy,ty,vy,zy,bw,chw,dw,fw,gw,hw,jw,kw,lw,llw,mw,nw,pw,rw,rrw,sw,tw,vw,zw,bl,fl,gl,kl,pl,br,dr,fr,gr,kr,pr,tr".Split(',');

        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => consonants;
        protected override string GetDictionaryName() => "cmudict_es.txt";
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => dictionaryReplacements;

        protected override List<string> ProcessSyllable(Syllable syllable) {
            string prevV = syllable.prevV;
            string[] cc = syllable.cc;
            string v = syllable.v;

            string basePhoneme;
            var phonemes = new List<string>();
            var lastC = cc.Length - 1;
            var firstC = 0;
            var rcv = $"- {v}";
            var vv1 = $"{prevV} {v}";
            var vv2 = $"{prevV}{v}";
            if (syllable.IsStartingV) {
                basePhoneme = rcv;
                if (!HasOto(rcv, syllable.vowelTone)) {
                    basePhoneme = $"{v}";
                }
            } else if (syllable.IsVV) {
                basePhoneme = vv1;
                if (!HasOto(vv1, syllable.vowelTone)) {
                    basePhoneme = vv2;
                    if (!HasOto(vv2, syllable.vowelTone)) {
                        basePhoneme = $"{v}";
                    }
                }
            } else if (syllable.IsStartingCVWithOneConsonant) {
                // TODO: move to config -CV or -C CV
                var rc = $"- {cc[0]}{v}";
                var src = $"- s{v}";
                if (HasOto(rc, syllable.vowelTone)) {
                    basePhoneme = rc;
                } else if (cc[0] == "z"
                    && !HasOto(cc[0], syllable.vowelTone)
                    && HasOto(src, syllable.vowelTone)) {
                    basePhoneme = src;
                } else {
                    basePhoneme = $"{cc[0]}{v}";
                }
            } else if (syllable.IsStartingCVWithMoreThanOneConsonant) {
                // try RCCV
                var rvvc = $"- {string.Join("", cc)}{v}";
                if (HasOto(rvvc, syllable.vowelTone)) {
                    basePhoneme = rvvc;
                    if (!HasOto(rvvc, syllable.vowelTone)) {
                        rvvc = ValidateAlias(rvvc);
                        basePhoneme = rvvc;
                    }
                } else {
                    basePhoneme = $"{cc.Last()}{v}";
                    // try RCC
                    for (var i = cc.Length; i > 1; i--) {
                        if (TryAddPhoneme(phonemes, syllable.tone, $"- {string.Join("", cc.Take(i))}", ValidateAlias($"- {string.Join("", cc.Take(i))}"), string.Join("", cc.Take(i)), ValidateAlias(string.Join("", cc.Take(i))))) {
                            firstC = i;
                            break;
                        }
                    }
                    if (phonemes.Count == 0) {
                        TryAddPhoneme(phonemes, syllable.tone, $"- {cc[0]}", ValidateAlias($"- {cc[0]}"));
                    }
                    // try CCV
                    for (var i = firstC; i < cc.Length - 1; i++) {
                        var ccv = string.Join("", cc.Skip(i)) + v;
                        if (HasOto(ccv, syllable.tone)) {
                            basePhoneme = ccv;
                            lastC = i;
                            if (!HasOto(ccv, syllable.tone)) {
                                ccv = ValidateAlias(ccv);
                                basePhoneme = ccv;
                                lastC = i;
                                break;
                            }
                            break;
                        }
                    }
                }
            } else { // VCV
                var vcv = $"{prevV} {cc[0]}{v}";
                var vccv = $"{prevV} {string.Join("", cc)}{v}";
                var nyvcv = $"{prevV} ny{v}";
                var svcv = $"{prevV} s{v}";
                var syvccv = $"{prevV} sy{v}";
                var swvccv = $"{prevV} sw{v}";
                if (HasOto(vcv, syllable.vowelTone)
                    && (syllable.IsVCVWithOneConsonant)) {
                    basePhoneme = vcv;
                } else if (cc[0] == "nh"
                    && !HasOto(cc[0], syllable.vowelTone)
                    && HasOto(nyvcv, syllable.vowelTone)
                    && syllable.IsVCVWithOneConsonant) {
                    basePhoneme = nyvcv;
                } else if (cc[0] == "z"
                    && !HasOto(cc[0], syllable.vowelTone)
                    && HasOto(svcv, syllable.vowelTone)
                    && syllable.IsVCVWithOneConsonant) {
                    basePhoneme = svcv;
                } else if (string.Join("", cc) == "zy"
                    && !HasOto(string.Join("", cc), syllable.vowelTone)
                    && HasOto(syvccv, syllable.vowelTone)
                    && syllable.IsVCVWithMoreThanOneConsonant) {
                    basePhoneme = syvccv;
                } else if (string.Join("", cc) == "zw"
                    && !HasOto(string.Join("", cc), syllable.vowelTone)
                    && HasOto(swvccv, syllable.vowelTone)
                    && syllable.IsVCVWithMoreThanOneConsonant) {
                    basePhoneme = swvccv;
                } else if (HasOto(vccv, syllable.vowelTone)
                    && syllable.IsVCVWithMoreThanOneConsonant
                    && !notClusters.Contains(string.Join("", cc))) {
                    basePhoneme = vccv;
                } else { 
                    basePhoneme = cc.Last() + v;
                    // try CCV
                    if (cc.Length - firstC > 1) {
                        for (var i = firstC; i < cc.Length; i++) {
                            var ccv = $"{string.Join("", cc.Skip(i))}{v}";
                            var ccv2 = $"{string.Join(" ", cc.Skip(i))}{v}";
                            var ccv3 = $"{cc[0]} {string.Join("", cc.Skip(i))}{v}";
                            if (HasOto(ccv, syllable.vowelTone) && !notClusters.Contains(string.Join("", cc.Skip(i)))) {
                                lastC = i;
                                basePhoneme = ccv;
                                if (!HasOto(ccv, syllable.vowelTone)) {
                                    ccv = ValidateAlias(ccv);
                                    lastC = i;
                                    basePhoneme = ccv;
                                    break;
                                }
                                break;
                            } else if (HasOto(ccv2, syllable.vowelTone)) {
                                lastC = i;
                                basePhoneme = ccv2;
                                if (!HasOto(ccv2, syllable.vowelTone)) {
                                    ccv2 = ValidateAlias(ccv);
                                    lastC = i;
                                    basePhoneme = ccv2;
                                    break;
                                }
                                break;
                            } else if (HasOto(ccv3, syllable.vowelTone) && !notClusters.Contains(string.Join("", cc.Skip(i)))) {
                                lastC = i;
                                basePhoneme = ccv3;
                                if (!HasOto(ccv3, syllable.vowelTone)) {
                                    ccv3 = ValidateAlias(ccv3);
                                    lastC = i;
                                    basePhoneme = ccv3;
                                    break;
                                }
                                break;
                            }
                        }
                    }
                    // try vc
                    for (var i = lastC + 1; i >= 0; i--) {
                        var vc1 = $"{prevV} {cc[0]}";
                        var vc2 = $"{prevV}{cc[0]}";
                        // try vcc
                        var vcc = $"{prevV} {string.Join("", cc.Take(i))}";
                        var vcc2 = $"{prevV}{string.Join("", cc.Take(i))}";
                        if (i == 0 && HasOto(vc1, syllable.tone)) {
                            phonemes.Add(vc1);
                            if (!HasOto(vc1, syllable.tone)) {
                                vc1 = ValidateAlias(vc1);
                                phonemes.Add(vc1);
                                break;
                            }
                            break;
                        } else if (HasOto(vcc, syllable.tone) && !notClusters.Contains(string.Join("", cc.Take(i)))) {
                            phonemes.Add(vcc);
                            firstC = i - 1;
                            if (!HasOto(vcc, syllable.tone)) {
                                vcc = ValidateAlias(vcc);
                                phonemes.Add(vcc);
                                firstC = i - 1;
                                break;
                            }
                            break;
                        } else if (HasOto(vcc2, syllable.tone) && !notClusters.Contains(string.Join("", cc.Take(i)))) {
                            phonemes.Add(vcc2);
                            firstC = i - 1;
                            if (!HasOto(vcc2, syllable.tone)) {
                                vcc2 = ValidateAlias(vcc2);
                                phonemes.Add(vcc2);
                                firstC = i - 1;
                                break;
                            }
                            break;
                        } else if (!HasOto(vc1, syllable.tone) && HasOto(vc2, syllable.tone)) {
                            phonemes.Add(vc2);
                            if (!HasOto(vc2, syllable.tone)) {
                                vc2 = ValidateAlias(vc1);
                                phonemes.Add(vc2);
                                break;
                            }
                            break;
                        } else if (!HasOto(vc1, syllable.tone) && !HasOto(vc2, syllable.tone)) {
                            continue;
                        }
                    }
                    if (cc.Length - firstC > 1) {
                        for (var i = firstC; i < cc.Length; i++) {
                            var spccv = $"{string.Join("", cc.Skip(i))}{v}";
                            var ccv3 = $"{cc[0]} {string.Join("", cc.Skip(i))}{v}";
                            var syccv = $"{cc[0]} sy{v}";
                            var swccv = $"{cc[0]} sw{v}";
                            if (specialClusters.Contains(string.Join("", cc.Skip(i)))) {
                                lastC = i;
                                basePhoneme = spccv;
                                if (!HasOto(spccv, syllable.vowelTone)) {
                                    spccv = ValidateAlias(spccv);
                                    lastC = i;
                                    basePhoneme = spccv;
                                }
                            }
                            if (specialClusters.Contains(string.Join("", cc.Skip(i))) && HasOto(ccv3, syllable.vowelTone)) {
                                lastC = i;
                                basePhoneme = ccv3;
                                if (!HasOto(ccv3, syllable.vowelTone)) {
                                    ccv3 = ValidateAlias(ccv3);
                                    lastC = i;
                                    basePhoneme = ccv3;
                                }
                            }
                            if (string.Join("", cc) == "zy"
                                && !HasOto(string.Join("", cc), syllable.vowelTone)
                                && HasOto(syccv, syllable.vowelTone)) {
                                lastC = i;
                                basePhoneme = syccv;
                            }
                            if (string.Join("", cc) == "zw"
                                && !HasOto(string.Join("", cc), syllable.vowelTone)
                                && HasOto(swccv, syllable.vowelTone)) {
                                lastC = i;
                                basePhoneme = swccv;
                            }
                        }
                    }

                }
            }
            for (var i = firstC; i < lastC; i++) {
                // we could use some CCV, so lastC is used
                // we could use -CC so firstC is used
                var cc1 = $"{cc[i]} {cc[i + 1]}";
                var ncc1 = $"{cc[i]} n";
                if (!HasOto(cc1, syllable.tone)) {
                    cc1 = ValidateAlias(cc1);
                }
                if (!HasOto(cc1, syllable.tone)) {
                    cc1 = $"{cc[i]}{cc[i + 1]}";
                }
                if (!HasOto(cc1, syllable.tone)) {
                    cc1 = ValidateAlias(cc1);
                }
                if (i + 1 < lastC) {
                    var cc2 = $"{cc[i + 1]} {cc[i + 2]}";
                    var ncc2 = $"{cc[i + 1]} n";
                    if (!HasOto(cc2, syllable.tone)) {
                        cc2 = ValidateAlias(cc2);
                    }
                    if (!HasOto(cc2, syllable.tone)) {
                        cc2 = $"{cc[i + 1]}{cc[i + 2]}";
                    }
                    if (!HasOto(cc2, syllable.tone)) {
                        cc2 = ValidateAlias(cc2);
                    }
                    if (HasOto(cc1, syllable.tone) && HasOto(cc2, syllable.tone)) {
                        // like [V C1] [C1 C2] [C2 C3] [C3 ..]
                        phonemes.Add(cc1);
                    } else if (TryAddPhoneme(phonemes, syllable.tone, $"{cc[i]} {cc[i + 1]}-", ValidateAlias($"{cc[i]} {cc[i + 1]}-"))) {
                        // like [V C1] [C1 C2-] [C3 ..]
                        i++;
                    } else if (TryAddPhoneme(phonemes, syllable.tone, cc1, ValidateAlias(cc1))) {
                        if (cc[i + 2] == "nh" && !HasOto(cc[i + 2], syllable.tone)) {
                            TryAddPhoneme(phonemes, syllable.tone, ncc2, ValidateAlias(ncc2)); ;
                        }
                        // like [V C1] [C1 C2] [C2 ..]
                    } else if (!HasOto(cc1, syllable.tone) && firstC == i - 1 && cc[i] != cc[0]) {
                        TryAddPhoneme(phonemes, syllable.tone, cc[i], ValidateAlias(cc[i]));
                    }
                    if (!HasOto(cc2, syllable.tone) && firstC == i - 1 && cc[i + 1] != cc[1]) {
                        TryAddPhoneme(phonemes, syllable.tone, cc[i + 1], ValidateAlias(cc[i + 1]));
                    }
                } else {
                    // like [V C1] [C1 C2]  [C2 ..] or like [V C1] [C1 -] [C3 ..]
                    TryAddPhoneme(phonemes, syllable.tone, cc1);
                    if (cc[i + 1] == "nh" && !HasOto(cc[i + 1], syllable.tone)) {
                        TryAddPhoneme(phonemes, syllable.tone, ncc1, ValidateAlias(ncc1));
                    }
                    if (!HasOto(cc1, syllable.tone) && firstC == i - 1 && cc[i] != cc[0]) {
                        TryAddPhoneme(phonemes, syllable.tone, cc[i], ValidateAlias(cc[i]));
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
            if (ending.IsEndingV) {   // ending V
                TryAddPhoneme(phonemes, ending.tone, $"{v} R", $"{v} -", $"{v}-");
            } else if (ending.IsEndingVCWithOneConsonant) {   // ending VC
                TryAddPhoneme(phonemes, ending.tone, $"{v} {cc[0]}-", ValidateAlias($"{v} {cc[0]}-"), $"{v}{cc[0]}-", ValidateAlias($"{v}{cc[0]}-"), $"{v} {cc[0]}", ValidateAlias($"{v} {cc[0]}"), $"{v}{cc[0]}", ValidateAlias($"{v}{cc[0]}"), cc[0], ValidateAlias(cc[0]));
            } else if (ending.IsEndingVCWithMoreThanOneConsonant) {   // ending VCC (very rare, usually only occurs in words ending with "x")
                var vcc = $"{v} {string.Join("", cc)}";
                var vcc2 = $"{v}{string.Join("", cc)}";
                var vc = $"{v} {cc[0]}";
                var vc2 = $"{v}{cc[0]}";
                var cc1 = $"{cc[0]} {cc[1]}";
                var cc2 = $"{cc[0]}{cc[1]}";
                TryAddPhoneme(phonemes, ending.tone, vcc, vcc2);
                if (!HasOto(vcc, ending.tone) && !HasOto(vcc2, ending.tone) && HasOto(vc, ending.tone)) {
                    phonemes.Add(vc);
                    if (!HasOto(vc, ending.tone)) {
                        vc = ValidateAlias(vc);
                        phonemes.Add(vc);
                    }
                    TryAddPhoneme(phonemes, ending.tone, cc1, ValidateAlias(cc1));
                    if (!HasOto(cc1, ending.tone)) {
                        TryAddPhoneme(phonemes, ending.tone, cc2, ValidateAlias(cc2));
                    }
                    if (!HasOto(cc1, ending.tone) && !HasOto(cc2, ending.tone)) {
                        TryAddPhoneme(phonemes, ending.tone, cc[0] + cc[1], ValidateAlias(cc[0] + cc[1]));
                    }
                }
                if (!HasOto(vcc, ending.tone) && !HasOto(vcc2, ending.tone) && !HasOto(vc, ending.tone) && HasOto(vc2, ending.tone)) {
                    phonemes.Add(vc2);
                    if (!HasOto(vc2, ending.tone)) {
                        vc2 = ValidateAlias(vc2);
                        phonemes.Add(vc2);
                    }
                    TryAddPhoneme(phonemes, ending.tone, cc1, ValidateAlias(cc1));
                    if (!HasOto(cc1, ending.tone)) {
                        TryAddPhoneme(phonemes, ending.tone, cc2, ValidateAlias(cc2));
                    } else if (!HasOto(cc1, ending.tone) && !HasOto(cc2, ending.tone)) {
                        TryAddPhoneme(phonemes, ending.tone, cc[0] + cc[1], ValidateAlias(cc[0] + cc[1]));
                    }
                }
                if (!HasOto(vcc, ending.tone) && !HasOto(vcc2, ending.tone) && !HasOto(vc, ending.tone) && !HasOto(vc2, ending.tone)) {
                    phonemes.Add(cc[0]);
                    if (!HasOto(cc[0], ending.tone)) {
                        cc[0] = ValidateAlias(cc[0]);
                        phonemes.Add(cc[0]);
                    }
                    TryAddPhoneme(phonemes, ending.tone, cc1, ValidateAlias(cc1));
                    if (!HasOto(cc1, ending.tone)) {
                        TryAddPhoneme(phonemes, ending.tone, cc2, ValidateAlias(cc2));
                    } else if (!HasOto(cc1, ending.tone) && !HasOto(cc2, ending.tone)) {
                        TryAddPhoneme(phonemes, ending.tone, cc[1], ValidateAlias(cc[1]));
                    }
                }
            }
            return phonemes;
        }

        protected override string ValidateAlias(string alias) {
            foreach (var consonant in new[] { "j" }) {
                alias = alias.Replace(consonant, "ll");
            }
            foreach (var consonant in specialClusters) {
                alias = alias.Replace("w", "u");
            }
            foreach (var consonant in new[] { "z" }) {
                alias = alias.Replace(consonant, "s");
            }
            foreach (var consonant in specialClusters) {
                alias = alias.Replace("y", "i");
            }
            foreach (var consonant in new[] { "nh" }) {
                alias = alias.Replace(consonant, "ny");
            }
            foreach (var consonant in new[] { " nh" }) {
                alias = alias.Replace(consonant, " n");
            }
            foreach (var vowel in vowels) {
                foreach (var consonant in new[] { " ny" }) {
                    return alias.Replace(vowel + consonant, vowel + " n");
                }
            }
            foreach (var consonant1 in consonants) {
                foreach (var consonant2 in new[] { " ny" }) {
                    return alias.Replace(consonant1 + consonant2, consonant1 + " n");
                }
            }
            foreach (var vowel in vowels) {
                foreach (var consonant in new[] { " nh" }) {
                    return alias.Replace(vowel + consonant, vowel + " n");
                }
            }
            foreach (var consonant1 in consonants) {
                foreach (var consonant2 in new[] { " nh" }) {
                    return alias.Replace(consonant1 + consonant2, consonant1 + " n");
                }
            }
            return alias;
        }

        protected override double GetTransitionBasicLengthMs(string alias = "") {
            foreach (var c in longConsonants) {
                if (alias.EndsWith(c)) {
                    return base.GetTransitionBasicLengthMs() * 2.0;
                }
            }
            foreach (var c in new[] { "r" }) {
                if (alias.EndsWith(c)) {
                    return base.GetTransitionBasicLengthMs() * 0.75;
                }
            }
            return base.GetTransitionBasicLengthMs();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using OpenUtau.Api;
using System.Linq;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Spanish Syllable-Based Phonemizer", "ES SYL", "Lotte V")]
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
                "b=b;ch=ch;d=d;dz=dz;f=f;g=g;gn=nh;h=h;k=k;l=l;ll=j;m=m;n=n;p=p;r=r;rr=rr;s=s;sh=sh;t=t;ts=ts;w=w;y=y;z=z;zz=zz;zh=zh;I=i;U=u").Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);

        private readonly string[] longConsonants = "ch,dz,s,sh,k,p,t,ts,z,l,m,n".Split(',');
        private readonly string[] burstConsonants = "ch,dz,j,k,p,r,t,ts".Split(',');
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
                    && HasOto(src, syllable.vowelTone)
                    && syllable.IsStartingCVWithOneConsonant) {
                    basePhoneme = src;
                } else {
                    basePhoneme = $"{cc[0]}{v}";
                }
            } else if (syllable.IsStartingCVWithMoreThanOneConsonant) {
                // try RCCV
                var rvvc = $"- {string.Join("", cc)}{v}";
                var syrvvc = $"- sy{v}";
                var swrvvc = $"- sw{v}";
                if (HasOto(rvvc, syllable.vowelTone)) {
                    basePhoneme = rvvc;
                } else if (string.Join("", cc) == "zy"
                    && !HasOto(string.Join("", cc), syllable.vowelTone)
                    && HasOto(syrvvc, syllable.vowelTone)
                    && syllable.IsStartingCVWithMoreThanOneConsonant) {
                    basePhoneme = syrvvc;
                } else if (string.Join("", cc) == "zw"
                    && !HasOto(string.Join("", cc), syllable.vowelTone)
                    && HasOto(syrvvc, syllable.vowelTone)
                    && syllable.IsStartingCVWithMoreThanOneConsonant) {
                    basePhoneme = swrvvc;
                } else if (specialClusters.Contains(string.Join("", cc))) {
                    basePhoneme = $"{string.Join("", cc)}{v}";
                } else {
                    basePhoneme = $"{cc.Last()}{v}";
                    // try RCC
                    // for (var i = cc.Length; i > 1; i--) {
                    // if (TryAddPhoneme(phonemes, syllable.tone, $"- {string.Join("", cc.Take(i))}")) {
                    //    firstC = i;
                    //    break;
                    //    }
                    //}
                    // if (phonemes.Count == 0) {
                    //    TryAddPhoneme(phonemes, syllable.tone, $"- {cc[0]}");
                    //}
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
                    // try vc
                    for (var i = lastC + 1; i >= 0; i--) {
                        var vc1 = $"{prevV} {cc[0]}";
                        var vc2 = $"{prevV}{cc[0]}";
                        if (i == 0 && HasOto(vc1, syllable.tone)) {
                            phonemes.Add(vc1);
                            break;
                        } else if (!HasOto(vc1, syllable.tone) && HasOto(vc2, syllable.tone)) {
                            phonemes.Add(vc2);
                            break;
                        }
                        // try vcc
                        var vcc = $"{prevV} {string.Join("", cc.Take(i))}";
                        if (HasOto(vcc, syllable.tone) && !(notClusters.Contains(string.Join("", cc.Take(i))))) {
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
                            var ccv2 = $"{string.Join(" ", cc.Skip(i))}{v}";
                            var ccv3 = $"{cc[0]} {string.Join("", cc.Skip(i))}{v}";
                            if (HasOto(ccv, syllable.vowelTone) && !(notClusters.Contains(string.Join("", cc.Skip(i))))) {
                                lastC = i;
                                basePhoneme = ccv;
                                break;
                            } else if (HasOto(ccv2, syllable.vowelTone)) {
                                lastC = i;
                                basePhoneme = ccv2;
                                break;
                            } else if (HasOto(ccv3, syllable.vowelTone) && !(notClusters.Contains(string.Join("", cc.Skip(i))))) {
                                lastC = i;
                                basePhoneme = ccv3;
                                break;
                            }
                        }
                    }
                    for (var i = firstC; i < cc.Length; i++) {
                        var spccv = $"{string.Join("", cc.Skip(i))}{v}";
                        var ccv3 = $"{cc[0]} {string.Join("", cc.Skip(i))}{v}";
                        var syccv = $"{cc[0]} sy{v}";
                        var swccv = $"{cc[0]} sw{v}";
                        if (specialClusters.Contains(string.Join("", cc.Skip(i)))) {
                            basePhoneme = spccv;
                        }
                        if (specialClusters.Contains(string.Join("", cc.Skip(i))) && HasOto(ccv3, syllable.vowelTone)) {
                            basePhoneme = ccv3;
                        }
                        if (string.Join("", cc) == "zy"
                            && !HasOto(string.Join("", cc), syllable.vowelTone)
                            && HasOto(syccv, syllable.vowelTone)) {
                            basePhoneme = syccv;
                        }
                        if (string.Join("", cc) == "zw"
                            && !HasOto(string.Join("", cc), syllable.vowelTone)
                            && HasOto(swccv, syllable.vowelTone)) {
                            basePhoneme = swccv;
                        }
                    }
                }
            }
            for (var i = firstC; i < lastC; i++) {
                // we could use some CCV, so lastC is used
                // we could use -CC so firstC is used
                var cc1 = $"{cc[i]} {cc[i + 1]}";
                var ncc1 = $"{cc[i]} n";
                if (!HasOto(cc1, syllable.tone) && !specialClusters.Contains(string.Join("", cc.Skip(i)))) {
                    cc1 = $"{cc[i]}{cc[i + 1]}";
                }
                if (i + 1 < lastC) {
                    var cc2 = $"{cc[i + 1]} {cc[i + 2]}";
                    var ncc2 = $"{cc[i + 1]} n";
                    if (!HasOto(cc2, syllable.tone) && !specialClusters.Contains(string.Join("", cc.Skip(i)))) {
                        cc2 = $"{cc[i + 1]}{cc[i + 2]}";
                    }
                    if (HasOto(cc1, syllable.tone) && HasOto(cc2, syllable.tone)) {
                        // like [V C1] [C1 C2] [C2 C3] [C3 ..]
                        phonemes.Add(cc1);
                    } else if (TryAddPhoneme(phonemes, syllable.tone, $"{cc[i]} {cc[i + 1]}-")) {
                        // like [V C1] [C1 C2-] [C3 ..]
                        i++;
                    } else if (TryAddPhoneme(phonemes, syllable.tone, cc1)) {
                        if (cc[i + 2] == "nh" && !HasOto(cc[i + 2], syllable.tone)) {
                            TryAddPhoneme(phonemes, syllable.tone, ncc2); ;
                        }
                        // like [V C1] [C1 C2] [C2 ..]
                    } else if (!HasOto(cc2, syllable.tone)
                        && HasOto(cc[i], syllable.tone)
                        && !syllable.IsStartingCVWithMoreThanOneConsonant
                        && !burstConsonants.Contains(cc[i])
                        && !specialClusters.Contains(string.Join("", cc.Skip(i)))) {
                        phonemes.Add(cc[i]);
                    } else if (burstConsonants.Contains(cc[i]) && !specialClusters.Contains(string.Join("", cc.Skip(i)))) {
                        // like [V C1] [C1] [C2 ..]
                        TryAddPhoneme(phonemes, syllable.tone, cc[i], $"{cc[i]} -");
                    }
                } else {
                    // like [V C1] [C1 C2]  [C2 ..] or like [V C1] [C1 -] [C3 ..]
                    TryAddPhoneme(phonemes, syllable.tone, cc1);
                    if (cc[i + 1] == "nh" && !HasOto(cc[i + 1], syllable.tone)) {
                        TryAddPhoneme(phonemes, syllable.tone, ncc1);
                    }
                    if (!HasOto(cc1, syllable.tone)
                        && HasOto(cc[i], syllable.tone)
                        && !syllable.IsStartingCVWithMoreThanOneConsonant
                        && !burstConsonants.Contains(cc[i])
                        && !specialClusters.Contains(string.Join("", cc.Skip(i)))) {
                        phonemes.Add(cc[i]);
                    } else if (burstConsonants.Contains(cc[i]) && !syllable.IsStartingCVWithMoreThanOneConsonant && !specialClusters.Contains(string.Join("", cc.Skip(i)))) {
                        TryAddPhoneme(phonemes, syllable.tone, cc[i], $"{cc[i]} -");
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
            var vr = $"{v} -";
            if (ending.IsEndingV && HasOto(vr, ending.tone)) {   // ending V
                phonemes.Add(vr);
            } else if (ending.IsEndingVCWithOneConsonant) {   // ending VC
                var vcr = $"{v} {cc[0]}-";
                var vc1 = $"{v} {cc[0]}";
                var vc2 = $"{v}{cc[0]}";
                if (HasOto(vcr, ending.tone)) {   // applies ending VC
                    phonemes.Add(vcr);
                } else if (!HasOto(vcr, ending.tone) && (HasOto(vc1, ending.tone))) {   // if no ending VC, then regular VC
                    phonemes.Add(vc1);
                } else if (!HasOto(vc1, ending.tone) && HasOto(vc2, ending.tone)) {
                    phonemes.Add(vc2);
                }
            } else if (ending.IsEndingVCWithMoreThanOneConsonant) {   // ending VCC (very rare, usually only occurs in words ending with "x")
                var vccr = $"{v} {string.Join("", cc)}";
                if (HasOto(vccr, ending.tone)) {   // applies ending VCC
                    phonemes.Add(vccr);
                } else if (!HasOto(vccr, ending.tone)) {   // if no ending VCC, then CC transitions
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
                            } else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} {cc[i + 2]}")) {
                                // like [C1 C2][C2 ...]
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
                            } else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i]} {cc[i + 1]}")) {
                                // like [C1 C2][C2 -]
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
            foreach (var c in new[] { "rr" }) {
                if (alias.EndsWith(c)) {
                    return base.GetTransitionBasicLengthMs() * 1.0;
                }
            }
            foreach (var c in new[] { "r" }) {
                if (alias.EndsWith(c)) {
                    return base.GetTransitionBasicLengthMs() * 0.75;
                }
            }
            foreach (var c in longConsonants) {
                if (alias.EndsWith(c)) {
                    return base.GetTransitionBasicLengthMs() * 1.5;
                }
            }
            return base.GetTransitionBasicLengthMs();
        }
    }
}

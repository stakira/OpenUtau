using System;
using System.Collections.Generic;
using System.Text;
using OpenUtau.Api;
using OpenUtau.Core.G2p;
using System.Linq;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Spanish VCCV Phonemizer", "ES VCCV", "Lotte V", language: "ES")]
    public class SpanishVCCVPhonemizer : SyllableBasedPhonemizer {
        /// <summary>
        /// Based on the nJokis method.
        /// Supports automatic consonant substitutes, such as seseo, through ValidateAlias.
        ///</summary>

        private readonly string[] vowels = "a,e,i,o,u,BB,DD,ff,GG,ll,mm,nn,rrr,ss,xx".Split(',');
        private readonly string[] consonants = "b,B,ch,d,D,E,f,g,G,h,I,jj,k,l,L,m,n,nJ,p,r,rr,s,sh,t,U,w,x,y,z".Split(',');
        private readonly string[] shortConsonants = "r".Split(",");
        private readonly string[] longConsonants = "ch,k,p,s,sh,t".Split(",");
        private readonly Dictionary<string, string> dictionaryReplacements = ("a=a;e=e;i=i;o=o;u=u;" +
                "b=b;ch=ch;d=d;f=f;g=g;gn=nJ;k=k;l=l;ll=jj;m=m;n=n;p=p;r=r;rr=rr;s=s;t=t;w=w;x=x;y=y;z=z;I=I;U=U;B=B;D=D;G=G;Y=y").Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);
        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => consonants;
        protected override string GetDictionaryName() => "cmudict_es.txt";
        protected override IG2p LoadBaseDictionary() => new SpanishG2p();
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => dictionaryReplacements;

        protected override List<string> ProcessSyllable(Syllable syllable) {
            string prevV = syllable.prevV;
            string[] cc = syllable.cc;
            string v = syllable.v;

            string basePhoneme;
            var phonemes = new List<string>();
            var lastC = cc.Length - 1;
            var firstC = 0;

            if (syllable.IsStartingV) {
                var rcv = $"- {v}";
                var rcv2 = $"-{v}";
                if (HasOto(rcv, syllable.vowelTone)) {
                    basePhoneme = rcv;
                } else {
                    basePhoneme = rcv2;
                }
            } else if (syllable.IsVV) {
                var vv = $"{prevV} {v}";
                var vv2 = $"{prevV}{v}";
                if (!CanMakeAliasExtension(syllable)) {
                    if (HasOto(vv, syllable.vowelTone)) {
                        basePhoneme = vv;
                    } else if (!HasOto(vv, syllable.vowelTone) && HasOto(ValidateAlias(vv2), syllable.vowelTone)) {
                        basePhoneme = ValidateAlias(vv2);
                    } else {
                        basePhoneme = v;
                    }
                } else {
                    // the previous alias will be extended
                    basePhoneme = null;
                }
            } else if (syllable.IsStartingCVWithOneConsonant) {
                // TODO: move to config -CV or -C CV
                // TODO: move to config -CV or -C CV
                var rcv = $"-{cc[0]}{v}";
                var cv = $"{cc[0]}{v}";
                if (HasOto(rcv, syllable.vowelTone)) {
                    basePhoneme = rcv;
                } else {
                    basePhoneme = cv;
                    if (consonants.Contains(cc[0])) {
                        TryAddPhoneme(phonemes, syllable.tone, $"-{cc[0]}", ValidateAlias($"-{cc[0]}"));
                    }
                }
            } else if (syllable.IsStartingCVWithMoreThanOneConsonant) {
                // try RCCV
                var rccv = $"-{string.Join("", cc)}{v}";
                if (HasOto(rccv, syllable.vowelTone)) {
                    basePhoneme = rccv;
                } else if (HasOto(ValidateAlias(rccv), syllable.vowelTone)) {
                    basePhoneme = ValidateAlias(rccv);
                } else {
                    var ccv = $"{string.Join("", cc)}{v}";
                    var _cv = $"_{cc.Last()}{v}";
                    if (HasOto(ccv, syllable.vowelTone)) {
                        basePhoneme = ccv;
                    } else if (HasOto(ValidateAlias(ccv), syllable.tone) && !ValidateAlias(ccv).StartsWith("B") && !ValidateAlias(ccv).StartsWith("D") && !ValidateAlias(ccv).StartsWith("G")) {
                        basePhoneme = ValidateAlias(ccv);
                    } else if (!HasOto(ValidateAlias(ccv), syllable.tone) && !HasOto(ValidateAlias(ValidateAlias(ccv)), syllable.tone) && HasOto(_cv, syllable.tone)) {
                        basePhoneme = _cv;
                    } else {
                        basePhoneme = $"{cc.Last()}{v}";
                    }
                    // try RCC
                    for (var i = cc.Length; i > 1; i--) {
                        if (HasOto(_cv, syllable.vowelTone)) {
                            TryAddPhoneme(phonemes, syllable.tone, $"-{string.Join("", cc.Take(i))}", ValidateAlias($"-{string.Join("", cc.Take(i))}"), $"-{cc[0]}", ValidateAlias($"-{cc[0]}"));
                            if (!HasOto($"-{string.Join("", cc.Take(i))}", syllable.tone) && !HasOto(ValidateAlias($"-{string.Join("", cc.Take(i))}"), syllable.tone) && !HasOto(ccv, syllable.vowelTone) && !HasOto(ValidateAlias(ccv), syllable.vowelTone)) {
                                TryAddPhoneme(phonemes, syllable.tone, $"{string.Join("", cc.Take(i))}", ValidateAlias($"{string.Join("", cc.Take(i))}"));
                            }
                            firstC = i;
                        } else {
                            TryAddPhoneme(phonemes, syllable.tone, $"-{cc[0]}", ValidateAlias($"-{cc[0]}"));
                            firstC = i;
                            break;
                        }
                    }
                }
            } else {
                var cv = cc.Last() + v;
                basePhoneme = cv;
                if (!HasOto(cv, syllable.vowelTone)) {
                    cv = ValidateAlias(cv);
                    basePhoneme = cv;
                }
                // try CCV
                if (cc.Length - firstC > 1) {
                    for (var i = firstC; i < cc.Length; i++) {
                        var ccv = $"{string.Join("", cc.Skip(i))}{v}";
                        var rccv = $"-{string.Join("", cc)}{v}";
                        if (HasOto(ccv, syllable.vowelTone)) {
                            lastC = i;
                            basePhoneme = ccv;
                            break;
                        } else if (!HasOto(ccv, syllable.vowelTone) && HasOto(ValidateAlias(ccv), syllable.vowelTone)) {
                            ccv = ValidateAlias(ccv);
                            lastC = i;
                            basePhoneme = ccv;
                            break;
                        } else if (!HasOto(ValidateAlias(ccv), syllable.vowelTone) && HasOto(rccv, syllable.vowelTone)) {
                            lastC = i;
                            basePhoneme = rccv;
                            break;
                        } else if (!HasOto(rccv, syllable.vowelTone) && HasOto(ValidateAlias(rccv), syllable.vowelTone)) {
                            rccv = ValidateAlias(rccv);
                            lastC = i;
                            basePhoneme = rccv;
                            break;
                        }
                    }
                }
                for (var i = lastC + 1; i >= 0; i--) {
                    var vcc = $"{prevV} {string.Join("", cc.Take(i))}";
                    var vcc2 = $"{prevV}{string.Join("", cc.Take(i))}";
                    var vcc3 = $"{prevV}{string.Join(" ", cc.Take(2))}";
                    var vcc4 = $"{prevV}{string.Join(" ", cc.Take(1))}";
                    var vcc5 = $"{prevV} {string.Join("", cc.Take(2))}";
                    var cc1 = $"{string.Join(" ", cc.Take(2))}";
                    var cc2 = $"{string.Join("", cc.Take(2))}";
                    var vc = $"{prevV} {cc[0]}";
                    var vc2 = $"{prevV}{cc[0]}";
                    if (i == 0) {
                        TryAddPhoneme(phonemes, syllable.tone, ValidateAlias(vcc), ValidateAlias(vc));
                    } else if (HasOto(vcc, syllable.tone)) {
                        phonemes.Add(vcc);
                        break;
                    } else if (!HasOto(vcc, syllable.tone) && HasOto(ValidateAlias(vcc), syllable.tone)) {
                        vcc = ValidateAlias(vcc);
                        phonemes.Add(vcc);
                        break;
                    } else if (HasOto(vcc2, syllable.tone)) {
                        phonemes.Add(vcc2);
                        break;
                    } else if (!HasOto(vcc2, syllable.tone) && HasOto(ValidateAlias(vcc2), syllable.tone)) {
                        vcc2 = ValidateAlias(vcc2);
                        phonemes.Add(vcc2);
                        break;
                    } else if (HasOto(vcc3, syllable.tone) && !HasOto(cc1, syllable.tone) && !HasOto(ValidateAlias(cc1), syllable.tone) && !HasOto(cc2, syllable.tone) && !HasOto(ValidateAlias(cc2), syllable.tone)) {
                        phonemes.Add(vcc3);
                        break;
                    } else if (!HasOto(vcc3, syllable.tone) && HasOto(ValidateAlias(vcc3), syllable.tone) && !HasOto(cc1, syllable.tone) && !HasOto(ValidateAlias(cc1), syllable.tone) && !HasOto(cc2, syllable.tone) && !HasOto(ValidateAlias(cc2), syllable.tone)) {
                        vcc3 = ValidateAlias(vcc3);
                        phonemes.Add(vcc3);
                        break;
                    } else if (HasOto(vcc4, syllable.tone)) {
                        phonemes.Add(vcc4);
                        break;
                    } else if (!HasOto(vcc4, syllable.tone) && HasOto(ValidateAlias(vcc4), syllable.tone)) {
                        vcc4 = ValidateAlias(vcc4);
                        phonemes.Add(vcc4);
                        break;
                    } else if (HasOto(vcc5, syllable.tone)) {
                        phonemes.Add(vcc5);
                        break;
                    } else if (!HasOto(vcc5, syllable.tone) && HasOto(ValidateAlias(vcc5), syllable.tone)) {
                        vcc5 = ValidateAlias(vcc5);
                        phonemes.Add(vcc5);
                        break;
                    } else if (HasOto(vc, syllable.tone)) {
                        phonemes.Add(vc);
                        break;
                    } else if (!HasOto(vc, syllable.tone) && HasOto(ValidateAlias(vc), syllable.tone)) {
                        vc = ValidateAlias(vc);
                        phonemes.Add(vc);
                        break;
                    } else if (HasOto(vc2, syllable.tone) && HasOto(ValidateAlias(vc2), syllable.tone)) {
                        phonemes.Add(vc2);
                        break;
                    } else {
                        continue;
                    }
                }
            }
            for (var i = firstC; i < lastC; i++) {
                // we could use some CCV, so lastC is used
                // we could use -CC so firstC is used
                var cc1 = $"{string.Join("", cc.Skip(i))}";
                var ccv = string.Join("", cc.Skip(i)) + v;
                var ucv = $"_{cc.Last()}{v}";
                var rccv = $"-{string.Join("", cc)}{v}";
                if (!HasOto(rccv, syllable.vowelTone) && !HasOto(ValidateAlias(rccv), syllable.vowelTone) && !vowels.Contains(cc1)) {
                    if (!HasOto(cc1, syllable.tone)) {
                        cc1 = ValidateAlias(cc1);
                    }
                    if (!HasOto(cc1, syllable.tone)) {
                        cc1 = $"{cc[i]} {cc[i + 1]}";
                    }
                    if (!HasOto(cc1, syllable.tone)) {
                        cc1 = ValidateAlias(cc1);
                    }
                    if (!HasOto(cc1, syllable.tone)) {
                        cc1 = $"{cc[i]}{cc[i + 1]}";
                    }
                    if (!HasOto(cc1, syllable.tone)) {
                        cc1 = ValidateAlias(cc1);
                    }
                    if (HasOto(ccv, syllable.vowelTone)) {
                        basePhoneme = ccv;
                        if (!HasOto(ccv, syllable.vowelTone)) {
                            ccv = ValidateAlias(ccv);
                            basePhoneme = ccv;
                        }
                    } else if (HasOto(ucv, syllable.vowelTone) && HasOto(cc1, syllable.vowelTone) && !cc1.Contains($"{cc[i]} {cc[i + 1]}")) {
                        basePhoneme = ucv;
                        if (!HasOto(ucv, syllable.vowelTone)) {
                            ucv = ValidateAlias(ucv);
                            basePhoneme = ucv;
                        }
                    }
                    var cc2 = $"{string.Join("", cc.Skip(i))}";
                    if (i + 1 < lastC && !vowels.Contains(cc2)) { 
                        if (!HasOto(cc2, syllable.tone)) {
                            cc2 = ValidateAlias(cc2);
                        }
                        if (!HasOto(cc2, syllable.tone)) {
                            cc2 = $"{cc[i + 1]} {cc[i + 2]}";
                        }
                        if (!HasOto(cc2, syllable.tone)) {
                            cc2 = ValidateAlias(cc2);
                        }
                        if (!HasOto(cc2, syllable.tone)) {
                            cc2 = $"{cc[i + 1]}{cc[i + 2]}";
                        }
                        if (!HasOto(cc2, syllable.tone)) {
                            cc2 = ValidateAlias(cc2);
                        }
                        if (cc2.Contains($"{string.Join("", cc.Skip(i))}") || cc2.Contains(ValidateAlias($"{string.Join("", cc.Skip(i))}"))) {
                            i++;
                        }
                        if (HasOto(ccv, syllable.vowelTone)) {
                            basePhoneme = ccv;
                            if (!HasOto(ccv, syllable.vowelTone)) {
                                ccv = ValidateAlias(ccv);
                                basePhoneme = ccv;
                            }
                        } else if (HasOto(ucv, syllable.vowelTone) && HasOto(cc2, syllable.vowelTone) && !cc2.Contains($"{cc[i + 1]} {cc[i + 2]}")) {
                            basePhoneme = ucv;
                            if (!HasOto(ucv, syllable.vowelTone)) {
                                ucv = ValidateAlias(ucv);
                                basePhoneme = ucv;
                            }
                        }
                        if (HasOto(cc1, syllable.tone) && HasOto(cc2, syllable.tone) && !cc1.Contains($"{string.Join("", cc.Skip(i))}") && !cc1.Contains(ValidateAlias($"{string.Join("", cc.Skip(i))}"))) {
                            // like [V C1] [C1 C2] [C2 C3] [C3 ..]
                            phonemes.Add(cc1);
                        } else if (TryAddPhoneme(phonemes, syllable.tone, cc1)) {
                            // like [V C1] [C1 C2] [C2 ..]
                            if (cc1.Contains($"{string.Join("", cc.Skip(i))}") || cc1.Contains(ValidateAlias($"{string.Join("", cc.Skip(i))}"))) {
                                i++;
                            }
                        }
                    } else {
                        // like [V C1] [C1 C2]  [C2 ..] or like [V C1] [C1 -] [C3 ..]
                        TryAddPhoneme(phonemes, syllable.tone, cc1);
                        if (cc1.Contains($"{string.Join("", cc.Skip(i))}") || cc1.Contains(ValidateAlias($"{string.Join("", cc.Skip(i))}"))) {
                            i++;
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
            if (ending.IsEndingV) {
                TryAddPhoneme(phonemes, ending.tone, $"{v}-", ValidateAlias($"{v}-"));
            } else if (ending.IsEndingVCWithOneConsonant) {
                var vcr = $"{v}{cc[0]}-";
                if (HasOto(vcr, ending.tone)) {
                    phonemes.Add(vcr);
                } else {
                    phonemes.Add($"{v} {cc[0]}");
                    TryAddPhoneme(phonemes, ending.tone, $"{cc[0]}-", ValidateAlias($"{cc[0]}-"));
                }
            } else {
                var vcc1 = $"{v} {string.Join("", cc)}";
                var vcc2 = $"{v}{string.Join(" ", cc)}";
                var vc1 = $"{v}{cc[0]}";
                var vc2 = $"{v} {cc[0]}";
                if (HasOto(vcc1, ending.tone)) {
                    phonemes.Add(vcc1);
                } else if (!HasOto(vcc1, ending.tone) && HasOto(ValidateAlias(vcc1), ending.tone)) {
                    phonemes.Add(ValidateAlias(vcc1));
                } else if (!HasOto(ValidateAlias(vcc1), ending.tone) && HasOto(vcc2, ending.tone)) {
                    phonemes.Add(vcc2);
                } else if (!HasOto(vcc2, ending.tone) && HasOto(ValidateAlias(vcc2), ending.tone)) {
                    phonemes.Add(ValidateAlias(vcc2));
                } else if (!HasOto(ValidateAlias(vcc2), ending.tone) && HasOto(vc1, ending.tone)) {
                    phonemes.Add(vc1);
                } else if (!HasOto(vc1, ending.tone) && HasOto(ValidateAlias(vc1), ending.tone)) {
                    phonemes.Add(ValidateAlias(vc1));
                } else if (!HasOto(ValidateAlias(vc1), ending.tone) && HasOto(vc2, ending.tone)) {
                    phonemes.Add(vc2);
                } else if (!HasOto(vc2, ending.tone) && HasOto(ValidateAlias(vc2), ending.tone)) {
                    phonemes.Add(ValidateAlias(vc2));
                }
                for (var i = 0; i < cc.Length - 1; i++) {
                    var cc1 = $"{cc[i]} {cc[i + 1]}";
                    if (!HasOto(cc1, ending.tone)) {
                        cc1 = ValidateAlias(cc1);
                    }
                    if (!HasOto(cc1, ending.tone)) {
                        cc1 = $"{cc[i]}{cc[i + 1]}";
                    }
                    if (!HasOto(cc1, ending.tone)) {
                        cc1 = ValidateAlias(cc1);
                    }
                    TryAddPhoneme(phonemes, ending.tone, cc1, ValidateAlias(cc1));
                    TryAddPhoneme(phonemes, ending.tone, $"{cc.Last()}-", ValidateAlias($"{cc.Last()}-"));
                }
            }
            return phonemes;
        }
        protected override string ValidateAlias(string alias) {
            foreach (var consonant in new[] { "w" }) {
                alias = alias.Replace("w", "u");
            }
            foreach (var consonant in new[] { "y" }) {
                alias = alias.Replace("y", "i");
            }
            foreach (var consonant in new[] { "I" }) {
                alias = alias.Replace("I", "y");
            }
            foreach (var consonant in new[] { "U" }) {
                alias = alias.Replace("U", "w");
            }
            foreach (var vowel in new[] { "BB" }) {
                alias = alias.Replace("BB", "B");
            }
            foreach (var vowel in new[] { "DD" }) {
                alias = alias.Replace("DD", "D");
            }
            foreach (var vowel in new[] { "ff" }) {
                alias = alias.Replace("ff", "f");
            }
            foreach (var vowel in new[] { "GG" }) {
                alias = alias.Replace("GG", "G");
            }
            foreach (var vowel in new[] { "l-" }) {
                alias = alias.Replace("ll", "l");
            }
            foreach (var vowel in new[] { "mm" }) {
                alias = alias.Replace("mm", "m");
            }
            foreach (var vowel in new[] { "nn" }) {
                alias = alias.Replace("nn", "n");
            }
            foreach (var vowel in new[] { "rrr" }) {
                alias = alias.Replace("rrr", "rr");
            }
            foreach (var vowel in new[] { "ss" }) {
                alias = alias.Replace("ss", "s");
            }
            foreach (var consonant in new[] { "E" }) {
                alias = alias.Replace("E", "e");
            }
            foreach (var CC in new[] { " k", " p", " ch" }) {
                alias = alias.Replace(CC, " t");
            }
            foreach (var consonant in new[] { "b" }) {
                alias = alias.Replace("b", "B");
            }
            foreach (var consonant in new[] { "d" }) {
                alias = alias.Replace("d", "D");
            }
            foreach (var consonant in new[] { "g" }) {
                alias = alias.Replace("g", "G");
            }
            foreach (var consonant in new[] { "z" }) {
                alias = alias.Replace("z", "s");
            }
            foreach (var consonant in new[] { "jj" }) {
                alias = alias.Replace("jj", "sh");
            }
            foreach (var consonant in new[] { "jj" }) {
                alias = alias.Replace("jj", "L");
            }
            foreach (var consonant in new[] { "x" }) {
                alias = alias.Replace("x", "h");
            }
            foreach (var vowel in new[] { "xx" }) {
                alias = alias.Replace("h", "x");
                alias = alias.Replace("xx", "x");
            }
            return alias;
        }

        protected override double GetTransitionBasicLengthMs(string alias = "") {
            foreach (var c in shortConsonants ) {
                if (alias.Contains(c) && !alias.Contains("rr") && !alias.StartsWith(c) && !alias.Contains("ar") && !alias.Contains("er") && !alias.Contains("ir") && !alias.Contains("or") && !alias.Contains("ur")) {
                    return base.GetTransitionBasicLengthMs() * 0.50;
                }
            }
            foreach (var c in longConsonants) {
                if (alias.Contains(c) && !alias.StartsWith(c) && !alias.StartsWith("-" + c)) {
                    return base.GetTransitionBasicLengthMs() * 2.0;
                }
            }
            return base.GetTransitionBasicLengthMs();
        }
    }
}

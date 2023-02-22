using System;
using System.Collections.Generic;
using System.Text;
using OpenUtau.Api;
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
        private readonly Dictionary<string, string> dictionaryReplacements = ("a=a;e=e;i=i;o=o;u=u;" +
                "b=b;ch=ch;d=d;f=f;g=g;gn=nJ;k=k;l=l;ll=jj;m=m;n=n;p=p;r=r;rr=rr;s=s;t=t;w=w;x=x;y=y;z=z;I=I;U=U;B=B;D=D;G=G;Y=y").Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);
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
                if (!CanMakeAliasExtension(syllable)) {
                    if (HasOto(vv, syllable.vowelTone)) {
                        basePhoneme = vv;
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
                } else {
                    var _cv = $"_{cc.Last()}{v}";
                    if (HasOto(_cv, syllable.tone)) {
                        basePhoneme = _cv;
                    } else {
                        basePhoneme = $"{cc.Last()}{v}";
                    }
                    // try RCC
                    for (var i = cc.Length; i > 1; i--) {
                        if (TryAddPhoneme(phonemes, syllable.tone, $"-{string.Join("", cc.Take(i))}")) {
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
                            if (!HasOto(ccv, syllable.vowelTone)) {
                                ccv = ValidateAlias(ccv);
                                lastC = i;
                                basePhoneme = ccv;
                                break;
                            }
                            break;
                        } else if (HasOto(rccv, syllable.vowelTone)) {
                            lastC = i;
                            basePhoneme = rccv;
                            if (!HasOto(rccv, syllable.vowelTone)) {
                                rccv = ValidateAlias(rccv);
                                lastC = i;
                                basePhoneme = rccv;
                                break;
                            }
                            break;
                        }
                    }
                }
                for (var i = lastC + 1; i >= 0; i--) {
                    var vcc = $"{prevV} {string.Join("", cc.Take(i))}";
                    var vcc2 = $"{prevV}{string.Join("", cc.Take(i))}";
                    var vcc3 = $"{prevV}{string.Join(" ", cc.Take(2))}";
                    var vcc4 = $"{prevV}{string.Join(" ", cc.Take(i))}";
                    var cc1 = $"{string.Join(" ", cc.Take(2))}";
                    var cc2 = $"{string.Join("", cc.Take(2))}";
                    var vc = $"{prevV} {cc[0]}";
                    var vc2 = $"{prevV}{cc[0]}";
                    if (i == 0) {
                        TryAddPhoneme(phonemes, syllable.tone, ValidateAlias(vcc), ValidateAlias(vc));
                    } else
                    if (HasOto(vcc, syllable.tone) && !HasOto(cc1, syllable.tone) && !HasOto(cc2, syllable.tone)) {
                        phonemes.Add(vcc);
                        firstC = i - 1;
                        if (!HasOto(vcc, syllable.tone)) {
                            vcc = ValidateAlias(vcc);
                            phonemes.Add(vcc);
                            firstC = i - 1;
                            break;
                        }
                        break;
                    } else if (HasOto(vcc2, syllable.tone)) {
                        phonemes.Add(vcc2);
                        firstC = i - 1;
                        if (!HasOto(vcc2, syllable.tone)) {
                            vcc2 = ValidateAlias(vcc2);
                            phonemes.Add(vcc2);
                            firstC = i - 1;
                            break;
                        }
                        break;
                    } else if (HasOto(vcc3, syllable.tone) && !HasOto(cc1, syllable.tone) && !HasOto(cc2, syllable.tone)) {
                        phonemes.Add(vcc3);
                        firstC = i - 2;
                        if (!HasOto(vcc3, syllable.tone)) {
                            vcc3 = ValidateAlias(vcc3);
                            phonemes.Add(vcc3);
                            firstC = i - 2;
                            break;
                        }
                        break;
                    } else if (HasOto(vcc4, syllable.tone)) {
                        phonemes.Add(vcc4);
                        firstC = i - 1;
                        if (!HasOto(vcc4, syllable.tone)) {
                            vcc4 = ValidateAlias(vcc4);
                            phonemes.Add(vcc4);
                            firstC = i - 1;
                            break;
                        }
                        break;
                    } else if (HasOto(vc, syllable.tone)) {
                        phonemes.Add(vc);
                        if (!HasOto(vc, syllable.tone)) {
                            vc = ValidateAlias(vc);
                            phonemes.Add(vc);
                            break;
                        }
                        break;
                    } else if (HasOto(vc2, syllable.tone)) {
                        phonemes.Add(vc2);
                        if (!HasOto(vc2, syllable.tone)) {
                            vc2 = ValidateAlias(vc2);
                            phonemes.Add(vc2);
                            break;
                        }
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
                if (!syllable.IsStartingCVWithMoreThanOneConsonant) {
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
                    if (i + 1 < lastC) {
                        var cc2 = $"{string.Join("", cc.Skip(i))}";
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
                        if (HasOto(cc1, syllable.tone) && HasOto(cc2, syllable.tone) && !cc1.Contains($"{string.Join("", cc.Skip(i))}")) {
                            // like [V C1] [C1 C2] [C2 C3] [C3 ..]
                            phonemes.Add(cc1);
                        } else if (TryAddPhoneme(phonemes, syllable.tone, cc1)) {
                            // like [V C1] [C1 C2] [C2 ..]
                            if (cc1.Contains($"{string.Join("", cc.Skip(i))}")) {
                                i++;
                            }
                        }
                    } else {
                        // like [V C1] [C1 C2]  [C2 ..] or like [V C1] [C1 -] [C3 ..]
                        TryAddPhoneme(phonemes, syllable.tone, cc1);
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
                TryAddPhoneme(phonemes, ending.tone, $"{v}-");
            } else if (ending.IsEndingVCWithOneConsonant) {
                var vcr = $"{v}{cc[0]}-";
                if (HasOto(vcr, ending.tone)) {
                    phonemes.Add(vcr);
                } else {
                    phonemes.Add($"{v} {cc[0]}");
                    TryAddPhoneme(phonemes, ending.tone, $"{cc[0]}-");
                }
            }
            return phonemes;
        }
        protected override string ValidateAlias(string alias) {
            foreach (var consonant in new[] { "I" }) {
                alias = alias.Replace("I", "y");
            }
            foreach (var consonant in new[] { "U" }) {
                alias = alias.Replace("U", "w");
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
            return alias;
        }
    }
}

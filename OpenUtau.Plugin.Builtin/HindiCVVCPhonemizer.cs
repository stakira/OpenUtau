using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenUtau.Api;
using System.Threading.Tasks;
using Serilog;
using System.Security.Cryptography;
using OpenUtau.Core.G2p;

namespace OpenUtau.Plugin.Builtin
{
    [Phonemizer("Hindi CVVC Phonemizer", "HI CVVC", "Lotte V", language:"HI")]
    public class HindiCVVCPhonemizer : SyllableBasedPhonemizer
    {
        /// <summary>
        /// Hindi CVVC phonemizer.
        /// This phonemizer only supports Devanagari input, powered by a dictionary.
        /// However, you can also input lyrics phonetically, if you prefer that.
        /// The phonetic notation is based on the ITRANS scheme: https://en.wikipedia.org/wiki/ITRANS
        /// </summary>

        private readonly string[] vowels = "a,aa,i,ii,u,uu,e,ai,o,au,a.n,aa.n,i.n,ii.n,u.n,uu.n,e.n,ai.n,o.n,au.n".Split(',');
        private readonly string[] consonants = "k,kh,g,gh,~N,ch,Ch,j,jh,~n,T,Th,D,Dh,N,t,th,d,dh,n,p,ph,b,bh,m,y,r,l,v,sh,Sh,s,h,q,K,G,z,Z,f,R,Rh,B,H,・".Split(',');
        private readonly Dictionary<string, string> dictionaryReplacements = ("a=a;aa=aa;i=i;ii=ii;u=u;uu=uu;e=e;E=ai;o=o;O=au;a~=a.n;aa~=aa.n;i~=i.n;ii~=ii.n;u~=u.n;uu~=uu.n;e~=e.n;E~=ai.n;o~=o.n;O~=au.n;" +
            "k=k;kh=kh;g=g;gh=gh;ng=~N;ch=ch;chh=Ch;j=j;jh=jh;~n=~n;tt=T;tth=Th;dd=D;ddh=Dh;nn=N;t=t;th=th;d=d;dh=dh;n=n;p=p;ph=ph;b=b;bh=bh;m=m;y=y;r=r;l=l;v=v;sh=sh;ssh=Sh;s=s;h=h;" +
            "q=q;x=K;Gh=G;z=z;f=f;rr=R;rrh=Rh").Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);
        private readonly string[] longConsonants = "k,kh,gh,ch,Ch,jh,T,Th,Dh,t,th,dh,p,ph,bh,sh,Sh,s,q,K,f".Split(',');
        private readonly string[] shortConsonants = "R".Split(',');
        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => consonants;
        protected override string GetDictionaryName() => "hi_dict.txt";
        //protected override IG2p LoadBaseDictionary() => new HindiG2p();
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => dictionaryReplacements;

        protected override List<string> ProcessSyllable(Syllable syllable)
        {
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
                if (!CanMakeAliasExtension(syllable)) {
                    basePhoneme = $"{prevV} {v}";
                } else {
                    // the previous alias will be extended
                    basePhoneme = null;
                }
            } else if (syllable.IsStartingCVWithOneConsonant) {
                var rcv = $"- {cc[0]}{v}";
                basePhoneme = rcv;
                if (!HasOto(rcv, syllable.vowelTone)) {
                    rcv = ValidateAlias(rcv);
                    basePhoneme = rcv;
                }
            } else if (syllable.IsStartingCVWithMoreThanOneConsonant) {
                var cv = $"{cc.Last()}{v}";
                basePhoneme = cv;
                if (!HasOto(cv, syllable.vowelTone)) {
                    cv = ValidateAlias(cv);
                    basePhoneme = cv;
                }
                // try RCC
                for (var i = cc.Length; i > 1; i--) {
                    if (TryAddPhoneme(phonemes, syllable.tone, $"- {string.Join("", cc.Take(i))}", ValidateAlias($"- {string.Join("", cc.Take(i))}"))) {
                        firstC = i;
                        break;
                    }
                }
                if (phonemes.Count == 0) {
                    TryAddPhoneme(phonemes, syllable.tone, $"- {cc[0]}", ValidateAlias($"- {cc[0]}"));
                }
            } else {
                basePhoneme = $"{cc.Last()}{v}";
                phonemes.Add($"{prevV} {cc[0]}");
            }
            for (var i = firstC; i < lastC; i++) {
                // we could use some CCV, so lastC is used
                // we could use -CC so firstC is used
                var cc1 = $"{string.Join("", cc.Skip(i))}";
                if (!HasOto(cc1, syllable.tone)) {
                    cc1 = ValidateAlias(cc1);
                }
                if (!HasOto(cc1, syllable.tone)) {
                    cc1 = $"{cc[i]}{cc[i + 1]}";
                }
                if (!HasOto(cc1, syllable.tone)) {
                    cc1 = ValidateAlias(cc1);
                }
                if (!HasOto(cc1, syllable.tone)) {
                    cc1 = $"{cc[i]} {cc[i + 1]}";
                }
                if (!HasOto(cc1, syllable.tone)) {
                    cc1 = ValidateAlias(cc1);
                }
                if (i + 1 < lastC) {
                    var cc2 = $"{string.Join("", cc.Skip(i))}";
                    if (!HasOto(cc2, syllable.tone)) {
                        cc2 = ValidateAlias(cc2);
                    }
                    if (!HasOto(cc2, syllable.tone)) {
                        cc2 = $"{cc[i + 1]}{cc[i + 2]}";
                    }
                    if (!HasOto(cc2, syllable.tone)) {
                        cc2 = ValidateAlias(cc2);
                    }
                    if (!HasOto(cc2, syllable.tone)) {
                         cc2 = $"{cc[i + 1]} {cc[i + 2]}";
                    }
                    if (!HasOto(cc2, syllable.tone)) {
                        cc2 = ValidateAlias(cc2);
                    }
                    if (HasOto(cc1, syllable.tone) && HasOto(cc2, syllable.tone) && !cc1.Contains($"{string.Join("", cc.Skip(i))}")) {
                        // like [V C1] [C1 C2] [C2 C3] [C3 ..]
                        phonemes.Add(cc1);
                    } else if (TryAddPhoneme(phonemes, syllable.tone, cc1)) {
                        // like [V C1] [C1 C2] [C2 ..]
                        if (cc1.Contains($"{string.Join("", cc.Skip(i))}")) {
                            i++;
                        }
                    } else if (TryAddPhoneme(phonemes, syllable.tone, $"{cc[i]} {cc[i + 1]}-", ValidateAlias($"{cc[i]} {cc[i + 1]}-"))) {
                        // like [V C1] [C1 C2-] [C3 ..]
                    } else if (!cc.First().Contains(cc[i + 1])) {
                        // like [C1][C2 ...]
                        TryAddPhoneme(phonemes, syllable.tone, cc[i + 1], ValidateAlias(cc[i + 1]));
                        i++;
                    }
                } else {
                    // like [V C1] [C1 C2]  [C2 ..] or like [V C1] [C1 -] [C3 ..]
                    TryAddPhoneme(phonemes, syllable.tone, cc1, ValidateAlias(cc1));
                    if (!HasOto(cc1, syllable.tone) && !cc.First().Contains(cc[i])) {
                        TryAddPhoneme(phonemes, syllable.tone, cc[i], ValidateAlias(cc[i]));
                    }
                    i++;
                }
            }
            phonemes.Add(basePhoneme);
            return phonemes;
        }

        protected override List<string> ProcessEnding(Ending ending)
        {
            string[] cc = ending.cc;
            string v = ending.prevV;

            var phonemes = new List<string>();
            if (ending.IsEndingV) {
                phonemes.Add($"{v} -");
            } else if (ending.IsEndingVCWithOneConsonant) {
                phonemes.Add($"{v} {cc[0]}-");
            } else {
                phonemes.Add($"{v} {cc[0]}");
                // all CCs except the first one are /C1C2/, the last one is /C1 C2-/
                // but if there is no /C1C2/, we try /C1 C2-/, vise versa for the last one
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
                    if (!HasOto(cc1, ending.tone)) {
                        cc1 = $"{cc[i]} {cc[i + 1]}-";
                    }
                    if (!HasOto(cc1, ending.tone)) {
                        cc1 = ValidateAlias(cc1);
                    }
                    if (i < cc.Length - 2) {
                        var cc2 = $"{cc[i]} {cc[i + 1]}{cc[i + 2]}-";
                        var cc3 = $"{cc[i + 1]} {cc[i + 2]}-";
                        if (!HasOto(cc2, ending.tone)) {
                            cc2 = ValidateAlias(cc2);
                        }
                        if (HasOto(cc2, ending.tone)) {
                            phonemes.Add(cc2);
                            i++;
                        }
                        if (!HasOto(cc2, ending.tone)) {
                            cc2 = ValidateAlias(cc2);
                            phonemes.Add(cc2);
                            i++;
                        } else if (HasOto(cc3, ending.tone)) {
                            phonemes.Add(cc3);
                            i++;
                            if (!HasOto(cc3, ending.tone)) {
                                cc3 = ValidateAlias(cc3);
                                phonemes.Add(cc3);
                                i++;
                            }
                        } else {
                            if (HasOto(cc1, ending.tone)) {
                                phonemes.Add(cc1);
                                if (!HasOto(cc1, ending.tone)) {
                                    cc1 = ValidateAlias(cc1);
                                    phonemes.Add(cc1);
                                }
                            } else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} {cc[i + 2]}-", ValidateAlias($"{cc[i + 1]} {cc[i + 2]}-"))) {
                                // like [C1 C2-][C2 ...]
                                i++;
                            } else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} {cc[i + 2]}", ValidateAlias($"{cc[i + 1]} {cc[i + 2]}"))) {
                                // like [C1 C2][C3 ...]
                            } else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]}{cc[i + 2]}", ValidateAlias($"{cc[i + 1]}{cc[i + 2]}"))) {
                                // like [C1C2][C3 ...]
                            } else if (!cc.First().Contains(cc[i + 1]) || !cc.First().Contains(cc[i + 2])) {
                                // like [C1][C2 ...]
                                TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]}-", ValidateAlias($"{cc[i + 1]}-"), cc[i + 1], ValidateAlias(cc[i + 1]));
                                TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 2]}-", ValidateAlias($"{cc[i + 2]}-"), cc[i + 2], ValidateAlias(cc[i + 2]));
                                i++;
                            } else if (!cc.First().Contains(cc[i])) {
                                // like [C1][C2 ...]
                                TryAddPhoneme(phonemes, ending.tone, $"{cc[i]}-", ValidateAlias($"{cc[i]}-"), cc[i], ValidateAlias(cc[i]));
                                i++;
                            }
                        }
                    } else {
                        // like [V C1] [C1 C2]  [C2 ..] or like [V C1] [C1 -] [C3 ..]
                        TryAddPhoneme(phonemes, ending.tone, $"{cc[i]} {cc[i + 1]}-", ValidateAlias($"{cc[i]} {cc[i + 1]}-"));
                        if (!HasOto($"{cc[i]} {cc[i + 1]}-", ending.tone)) {
                            TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]}-", ValidateAlias($"{cc[i + 1]}-"), cc[i + 1], ValidateAlias(cc[i + 1]));
                        }
                        i++;
                    }
                }
            }
            return phonemes;
        }

        protected override string ValidateAlias(string alias)
        {
            foreach (var consonant in new[] { "q" })
            {
                alias = alias.Replace(consonant, "k");
            }
            foreach (var consonant in new[] { "K" })
            {
                alias = alias.Replace(consonant, "kh");
            }
            foreach (var consonant in new[] { "G" })
            {
                alias = alias.Replace(consonant, "gh");
            }
            foreach (var consonant in new[] { "z" })
            {
                alias = alias.Replace(consonant, "j");
            }
            foreach (var consonant in new[] { "Z" })
            {
                alias = alias.Replace(consonant, "jh");
            }
            foreach (var consonant in new[] { "f" })
            {
                alias = alias.Replace(consonant, "ph");
            }
            foreach (var consonant in new[] { "R" })
            {
                alias = alias.Replace(consonant, "D");
            }
            foreach (var consonant in new[] { "Rh" })
            {
                alias = alias.Replace(consonant, "Dh");
            }
            foreach (var consonant in new[] { "Sh" })
            {
                alias = alias.Replace(consonant, "sh");
            }
            return alias;
        }

        protected override double GetTransitionBasicLengthMs(string alias = "") {
            foreach (var c in longConsonants) {
                if (alias.EndsWith(c)) {
                    return base.GetTransitionBasicLengthMs() * 2.0;
                }
            }
            foreach (var c in shortConsonants) {
                if (alias.EndsWith(c)) {
                    return base.GetTransitionBasicLengthMs() * 0.50;
                }
            }
            return base.GetTransitionBasicLengthMs();
        }
    }
}

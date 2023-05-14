using System;
using System.Collections.Generic;
using System.Text;
using OpenUtau.Api;
using OpenUtau.Core.G2p;
using System.Linq;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Italian Syllable-Based Phonemizer", "IT SYL", "Lotte V", language:"IT")]
    public class ItalianSyllableBasedPhonemizer : SyllableBasedPhonemizer {
        /// <summary>
        /// Italian syllable-based phonemizer.
        /// As opposed to the already existing Italian phonemizer, this one is dictionary-based.
        /// Similarly to that one, it's based on Makkusan's list.
        /// It also has support for extra Spanish consonants, but only through phonetic input.
        /// </summary>

        private readonly string[] vowels = "a,e,i,o,u,3,0".Split(',');
        private readonly string[] consonants = "b,d,dz,dZ,f,g,gn,j,k,l,m,M,n,N,p,r,rr,s,S,t,ts,tS,v,w,y,z,B,D,G,h,T,x,Y,'".Split(',');
        private readonly Dictionary<string, string> dictionaryReplacements = ("a=a;e=e;i=i;o=o;u=u;EE=3;OO=0;" + "a1=a;e1=e;i1=i;o1=o;u1=u;" +
                "b=b;d=d;dz=dz;dZZ=dZ;f=f;g=g;JJ=gn;k=k;l=l;LL=j;m=m;n=n;nf=M;ng=N;p=p;r=r;rr=rr;s=s;SS=S;t=t;ts=ts;tSS=tS;v=v;w=w;y=y;z=z").Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);

        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => consonants;
        protected override string GetDictionaryName() => "cmudict_it.txt";
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => dictionaryReplacements;

        protected override IG2p LoadBaseDictionary() => new ItalianG2p();

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
                basePhoneme = $"-{v}";
            } else if (syllable.IsVV) {
                if (!CanMakeAliasExtension(syllable)) {
                    basePhoneme = $"{prevV} {v}";
                } else {
                    // the previous alias will be extended
                    basePhoneme = null;
                }
            } else if (syllable.IsStartingCVWithOneConsonant) {
                // TODO: move to config -CV or -C CV
                var rcv = $"-{cc[0]}{v}";
                if (HasOto(rcv, syllable.vowelTone)) {
                    basePhoneme = rcv;
                } else {
                    basePhoneme = $"{cc[0]}{v}";
                }
            } else if (syllable.IsStartingCVWithMoreThanOneConsonant) {
                // try RCCV
                var rccv = $"-{string.Join("", cc)}{v}";
                if (HasOto(rccv, syllable.vowelTone) && !rccv.Contains("-bre")) {
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
                basePhoneme = cc.Last() + v;
                    // try CCV
                    if (cc.Length - firstC > 1)
                    {
                        for (var i = firstC; i < cc.Length; i++)
                        {
                            var ccv = $"{string.Join("", cc.Skip(i))}{v}";
                            if (HasOto(ccv, syllable.vowelTone) && !ccv.Contains("bre"))
                            {
                                lastC = i;
                                basePhoneme = ccv;
                                break;
                            }
                        }
                    }
                phonemes.Add($"{prevV} {cc[0]}");
            }
            for (var i = firstC; i < lastC; i++) {
                var rccv = $"-{string.Join("", cc)}{v}";
                if (!HasOto(rccv, syllable.vowelTone)) {
                    // we could use some CCV, so lastC is used
                    // we could use -CC so firstC is used
                    var _cv = $"_{cc.Last()}{v}";
                    var cc1 = $"{string.Join("", cc.Skip(i))}";
                    if (!HasOto(cc1, syllable.tone)) {
                        cc1 = $"{cc[i]}{cc[i + 1]}";
                    }
                    if (!HasOto($"{cc[i]}{cc[i + 1]}", syllable.tone) && !HasOto($"{string.Join("", cc.Skip(i))}", syllable.tone)) {
                        cc1 = $"{cc[i]} {cc[i + 1]}";
                    }
                    if (HasOto(_cv, syllable.vowelTone) && HasOto(cc1, syllable.tone) && !cc1.Contains($"{cc[i]} {cc[i + 1]}")) {
                        basePhoneme = _cv;
                    }
                    if (i + 1 < lastC) {
                        var cc2 = $"{string.Join("", cc.Take(i))}";
                        if (!HasOto(cc2, syllable.tone)) {
                           cc2 = $"{cc[i + 1]}{cc[i + 2]}";
                        }
                        if (!HasOto($"{cc[i + 1]}{cc[i + 2]}", syllable.tone) && !HasOto($"{string.Join("", cc.Skip(i))}", syllable.tone)) {
                            cc2 = $"{cc[i + 1]} {cc[i + 2]}";
                        }
                        if (HasOto($"_{cc.Last()}{v}", syllable.vowelTone) && HasOto(cc2, syllable.vowelTone) && !cc2.Contains($"{cc[i + 1]} {cc[i + 2]}")) {
                            basePhoneme = $"_{cc.Last()}{v}";
                        }
                        if (HasOto(cc1, syllable.tone) && HasOto(cc2, syllable.tone) && !cc1.Contains($"{string.Join("", cc.Skip(i))}")) {
                            // like [V C1] [C1 C2] [C2 C3] [C3 ..]
                            phonemes.Add(cc1);
                        } else if (TryAddPhoneme(phonemes, syllable.tone, cc1)) {
                            // like [V C1] [C1 C2] [C2 ..]
                            i++;
                        } else if (TryAddPhoneme(phonemes, syllable.tone, $"{cc[i]}{cc[i + 1]}-")) {
                            // like [V C1] [C1 C2-] [C3 ..]
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

        protected override List<string> ProcessEnding(Ending ending)
        {
            string[] cc = ending.cc;
            string v = ending.prevV;

            var phonemes = new List<string>();
            if (ending.IsEndingV) {
                phonemes.Add($"{v} R");
            } else if (ending.IsEndingVCWithOneConsonant) {
                var vcr = $"{v}{cc[0]}-";
                if (HasOto(vcr, ending.tone)) {
                    phonemes.Add(vcr);
                } else {
                    phonemes.Add($"{v} {cc[0]}");
                }
            } else if (ending.IsEndingVCWithMoreThanOneConsonant) {
                phonemes.Add($"{v} {cc[0]}");
                for (var i = 0; i < cc.Length - 1; i++)
                {
                    var cc1 = $"{cc[i]}{cc[i + 1]}-";
                    if (HasOto(cc1, ending.tone))
                    {
                        phonemes.Add(cc1);
                    }
                }
            }
            return phonemes;
        }
    }
}

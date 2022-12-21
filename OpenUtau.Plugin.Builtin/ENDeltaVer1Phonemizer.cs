using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.G2p;

namespace OpenUtau.Plugin.Builtin
{
    [Phonemizer("Delta English (Version 1) Phonemizer", "EN Delta (Ver1)", "Lotte V", language:"EN")]
    public class ENDeltaVer1Phonemizer : SyllableBasedPhonemizer
    {
        /// <summary>
        /// General English phonemizer for Delta list (X-SAMPA) voicebanks.
        /// The difference between this phonemizer and the Teto English phonemizer is that this one was made to support all Delta list-based banks.
        /// However, it should be fully compatible with Kasane Teto's English voicebank regardless.
        /// This is the version based on the first version of the list, with "full" diphthongs.
        /// There is also a second version of the phonemizer with split diphthongs, based on the third version of the list.
        /// It also has some support for sounds not found in the "classic" Delta list.
        /// Due to the flexibility of X-SAMPA, it was easy to add those.
        /// They are mostly based on sounds based on Cz's English VCCV list, just written differently. They are mostly found in North-American dialects.
        /// All of these sounds are optional and should be inserted manually/phonetically, if the voicebank supports them.
        ///</summary>

        private readonly string[] vowels = "a,A,@,{,V,O,aU,aI,E,3,eI,I,i,oU,OI,U,u,Q,Ol,aUn,e@,eN,IN,e,o,Ar,Er,Ir,Or,Ur,ir,ur,@l,@m,@n,@N,1,e@m,e@n".Split(',');
        private readonly string[] consonants = "b,tS,d,D,4,f,g,h,dZ,k,l,m,n,N,p,r,s,S,t,T,v,w,W,j,z,Z,t_},・,_".Split(',');
        private readonly string[] affricates = "tS,dZ".Split(',');
        private readonly string[] shortConsonants = "4".Split(",");
        private readonly string[] longConsonants = "tS,f,dZ,k,p,s,S,t,T,t_}".Split(",");
        private readonly string[] normalConsonants = "b,d,D,g,h,l,m,n,N,r,v,w,W,j,z,Z,・".Split(',');
        private readonly Dictionary<string, string> dictionaryReplacements = ("aa=A;ae={;ah=V;ao=O;aw=aU;ax=@;ay=aI;" +
            "b=b;ch=tS;d=d;dh=D;dx=4;eh=E;el=@l;em=@m;en=@n;eng=@N;er=3;ey=eI;f=f;g=g;hh=h;ih=I;iy=i;jh=dZ;k=k;l=l;m=m;n=n;ng=N;ow=oU;oy=OI;" +
            "p=p;q=・;r=r;s=s;sh=S;t=t;th=T;uh=U;uw=u;v=v;w=w;y=j;z=z;zh=Z").Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);
        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => consonants;
        protected override string GetDictionaryName() => "cmudict-0_7b.txt";
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => dictionaryReplacements;

        protected override IG2p LoadBaseDictionary() => new ArpabetG2p();

        protected override List<string> ProcessSyllable(Syllable syllable)
        {
            string prevV = syllable.prevV;
            string[] cc = syllable.cc;
            string v = syllable.v;

            string basePhoneme;
            var phonemes = new List<string>();
            var lastC = cc.Length - 1;
            var firstC = 0;
            var rv = $"- {v}";
            if (syllable.IsStartingV)
            {
                if (HasOto(rv, syllable.vowelTone)) {
                    basePhoneme = rv;
                    if (rv.Contains("V") && !HasOto(rv, syllable.vowelTone) && HasOto($"- A", syllable.vowelTone)) {
                        basePhoneme = $"- A";
                    }
                }
                else {
                    basePhoneme = v;
                    if (v.Contains("V") && !HasOto(v, syllable.vowelTone) && HasOto($"A", syllable.vowelTone)) {
                        basePhoneme = $"A";
                    }
                }
            }
            else if (syllable.IsVV)
            {
                if (!CanMakeAliasExtension(syllable))
                {
                    basePhoneme = $"{prevV} {v}";
                }
                else
                {
                    // the previous alias will be extended
                    basePhoneme = null;
                }
                if (!HasOto($"{prevV} {v}", syllable.vowelTone)) {
                    if (prevV == "V" && !HasOto(prevV, syllable.vowelTone)) {
                        syllable.prevV = "A";
                    } else if (v == "V" && !HasOto(v, syllable.vowelTone)) {
                        syllable.v = "A";
                    } else {
                        basePhoneme = $"{v}";
                    }
                }
            }
            else if (syllable.IsStartingCVWithOneConsonant)
            {
                // TODO: move to config -CV or -C CV
                var rcv = $"- {cc[0]}{v}";
                var cv = $"{cc[0]}{v}";
                if (HasOto(rcv, syllable.vowelTone))
                {
                    basePhoneme = rcv;
                }
                else if (v == "V" && !HasOto(rcv, syllable.vowelTone) && HasOto($"- {cc[0]}A", syllable.vowelTone))
                {
                    basePhoneme = $"- {cc[0]}A";
                }
                else
                {
                    basePhoneme = cv;
                    if (v == "V" && !HasOto(rcv, syllable.vowelTone) && !HasOto(cv, syllable.vowelTone) && HasOto($"{cc[0]}A", syllable.vowelTone)) {
                        basePhoneme = $"{cc[0]}A";
                    }
                    if (consonants.Contains(cc[0]))
                    {
                        TryAddPhoneme(phonemes, syllable.tone, $"- {cc[0]}");
                    }
                }
            }
            else if (syllable.IsStartingCVWithMoreThanOneConsonant)
            {
                // try RCCV
                var rccv = $"- {string.Join("", cc)}{v}";
                if (HasOto(rccv, syllable.vowelTone))
                {
                    basePhoneme = rccv;
                }
                else
                {
                    basePhoneme = $"{cc.Last()}{v}";
                    if (!HasOto($"{cc.Last()}V", syllable.vowelTone) && HasOto($"{cc.Last()}A", syllable.vowelTone)) {
                        basePhoneme = $"{cc.Last()}A";
                    }
                    if (HasOto($"_{cc.Last()}{v}", syllable.vowelTone)) {
                        basePhoneme = $"_{cc.Last()}{v}";
                    }
                    // try RCC
                    for (var i = cc.Length; i > 1; i--)
                    {
                        if (TryAddPhoneme(phonemes, syllable.tone, $"- {string.Join("", cc.Take(i))}"))
                        {
                            firstC = i;
                            break;
                        }
                    }
                    if (phonemes.Count == 0) {
                        TryAddPhoneme(phonemes, syllable.tone, $"- {cc[0]}");
                    }
                    // try CCV
                    for (var i = firstC; i < cc.Length - 1; i++)
                    {
                        var ccv = string.Join("", cc.Skip(i)) + v;
                        if (HasOto(ccv, syllable.tone))
                        {
                            basePhoneme = ccv;
                            lastC = i;
                            break;
                        }
                    }
                }
            }
            else
            { // VCV
                var vcv = $"{prevV} {cc[0]}{v}";
                var vccv = $"{prevV} {string.Join("", cc)}{v}";
                if (syllable.IsVCVWithOneConsonant && HasOto(vcv, syllable.vowelTone))
                {
                    basePhoneme = vcv;
                }
                else if (syllable.IsVCVWithMoreThanOneConsonant && HasOto(vccv, syllable.vowelTone))
                {
                    basePhoneme = vccv;
                }
                else if (prevV == "V" && !HasOto($"V {cc[0]}", syllable.vowelTone) && !HasOto(vcv, syllable.vowelTone) && !HasOto(vccv, syllable.vowelTone))
                {
                    basePhoneme = $"{cc.Last()}{v}";
                    phonemes.Add($"A {cc[0]}");
                }
                else
                {
                    basePhoneme = cc.Last() + v;
                    // try CCV
                    if (cc.Length - firstC > 1)
                    {
                        for (var i = firstC; i < cc.Length; i++)
                        {
                            var ccv = $"{string.Join("", cc.Skip(i))}{v}";
                            var rccv = $"- {string.Join("", cc.Skip(i))}{v}";
                            if (HasOto(ccv, syllable.vowelTone))
                            {
                                lastC = i;
                                basePhoneme = ccv;
                                break;
                            }
                            else if (HasOto(rccv, syllable.vowelTone) && (!HasOto(ccv, syllable.vowelTone)))
                            {
                                lastC = i;
                                basePhoneme = rccv;
                                break;
                            }
                        }
                    }
                    // try vcc
                    for (var i = lastC + 1; i >= 0; i--)
                    {
                        var vcc = $"{prevV} {string.Join("", cc.Take(i))}";
                        var vcc2 = $"{prevV}{string.Join(" ", cc.Take(2))}";
                        var vcc3 = $"{prevV}{string.Join(" ", cc.Take(i))}";
                        var cc1 = $"{string.Join(" ", cc.Take(2))}";
                        var cc2 = $"{string.Join("", cc.Take(2))}";
                        if (i == 0) {
                            phonemes.Add($"{prevV} -");
                        } else if (HasOto(vcc, syllable.tone)) {
                            phonemes.Add(vcc);
                            firstC = i - 1;
                            break;
                        } else if (HasOto(vcc2, syllable.tone) && !(HasOto(cc1, syllable.tone)) && !(HasOto(cc2, syllable.tone))) {
                            phonemes.Add(vcc2);
                            firstC = i - 2;
                            break;
                        } else if (HasOto(vcc3, syllable.tone)) {
                            phonemes.Add(vcc3);
                            firstC = i - 1;
                            break;
                        } else {
                            phonemes.Add($"{prevV} {cc[0]}");
                            break;
                        }
                    }
                }
            }
            for (var i = firstC; i < lastC; i++) {
                // we could use some CCV, so lastC is used
                // we could use -CC so firstC is used
                var cc1 = $"{string.Join("", cc.Skip(i))}";
                var ccv = string.Join("", cc.Skip(i)) + v;
                if (!HasOto($"- {string.Join("", cc)}{v}", syllable.vowelTone)) {
                    if (!HasOto(cc1, syllable.tone)) {
                        cc1 = $"{cc[i]}{cc[i + 1]}";
                    }
                    if (!HasOto($"{string.Join("", cc.Skip(i))}", syllable.tone) && !HasOto($"{cc[i]}{cc[i + 1]}", syllable.tone)) {
                        cc1 = $"{cc[i]} {cc[i + 1]}";
                    }
                    if (HasOto(ccv, syllable.vowelTone)) {
                        basePhoneme = ccv;
                    } else if (HasOto($"_{cc.Last()}{v}", syllable.vowelTone) && HasOto(cc1, syllable.vowelTone) && !cc1.Contains($"{cc[i]} {cc[i + 1]}")) {
                        basePhoneme = $"_{cc.Last()}{v}";
                    }
                    if (i + 1 < lastC) {
                        var cc2 = $"{string.Join("", cc.Skip(i))}";
                        if (!HasOto(cc2, syllable.tone)) {
                            cc2 = $"{cc[i + 1]}{cc[i + 2]}";
                        }
                        if (!HasOto($"{cc[i + 1]}{cc[i + 2]}", syllable.tone) && !HasOto($"{string.Join("", cc.Skip(i))}", syllable.tone)) {
                            cc2 = $"{cc[i + 1]} {cc[i + 2]}";
                        }
                        if (HasOto(ccv, syllable.vowelTone)) {
                            basePhoneme = ccv;
                        } else if (HasOto($"_{cc.Last()}{v}", syllable.vowelTone) && HasOto(cc2, syllable.vowelTone) && !cc2.Contains($"{cc[i + 1]} {cc[i + 2]}")) {
                            basePhoneme = $"_{cc.Last()}{v}";
                        } if (HasOto(cc1, syllable.tone) && HasOto(cc2, syllable.tone) && !cc1.Contains($"{string.Join("", cc.Skip(i))}")) {
                            // like [V C1] [C1 C2] [C2 C3] [C3 ..]
                            phonemes.Add(cc1);
                        } else if (TryAddPhoneme(phonemes, syllable.tone, cc1)) {
                            // like [V C1] [C1 C2] [C2 ..]
                            if (cc1.Contains($"{string.Join("", cc.Skip(i))}")) {
                                i++;
                            }
                        } else if (TryAddPhoneme(phonemes, syllable.tone, $"{cc[i]} {cc[i + 1]}-")) {
                            // like [V C1] [C1 C2-] [C3 ..]
                            if (affricates.Contains(cc[i + 1])) {
                                i++;
                            } else {
                                // continue as usual
                            }
                        } else if (affricates.Contains(cc[i])) {
                            // like [V C1] [C1] [C2 ..]
                            TryAddPhoneme(phonemes, syllable.tone, cc[i], $"{cc[i]} -");
                            //if (cc[i] == cc.Last() && !affricates.Contains(cc[i])) {
                            //    phonemes.Remove(cc[i]);
                            //    phonemes.Remove($"{cc[i]} -");
                            //}
                        }
                    } else {
                        // like [V C1] [C1 C2]  [C2 ..] or like [V C1] [C1 -] [C3 ..]
                        TryAddPhoneme(phonemes, syllable.tone, cc1);
                        if (affricates.Contains(cc[i]) && !HasOto(cc1, syllable.tone)) {
                            TryAddPhoneme(phonemes, syllable.tone, cc[i], $"{cc[i]} -");
                            //if (!affricates.Contains(cc[i]) && cc[i] == cc.Last()) {
                            //    phonemes.Remove(cc[i]);
                            //    phonemes.Remove($"{cc[i]} -");
                            //}
                        }
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
                 if (HasOto($"{v} -", ending.tone)) {
                    phonemes.Add($"{v} -");
                } else if (v == "V" && !HasOto($"{v} -", ending.tone) && HasOto($"A -", ending.tone)) {
                    phonemes.Add($"A -");
                } else {
                    //continue as usual
                }
            }
            else if (ending.IsEndingVCWithOneConsonant)
            {
                var vcr = $"{v} {cc[0]}-";
                if (HasOto(vcr, ending.tone)) {
                    phonemes.Add(vcr);
                } else if (HasOto($"{v}{cc[0]} -", ending.tone)) {
                    phonemes.Add($"{v}{cc[0]} -");
                } else if (v == "V" && !HasOto(vcr, ending.tone) && HasOto($"A {cc[0]}-", ending.tone)) {
                    phonemes.Add($"A {cc[0]}-");
                } else {
                    phonemes.Add($"{v} {cc[0]}");
                    if (v == "V" && !HasOto($"{v} {cc[0]}", ending.tone) && HasOto($"A {cc[0]}", ending.tone)) {
                        v.Replace("V", "A");
                    }
                    if (affricates.Contains(cc[0])) {
                        TryAddPhoneme(phonemes, ending.tone, $"{cc[0]} -", cc[0]);
                    } else {
                        TryAddPhoneme(phonemes, ending.tone, $"{cc[0]} -");
                    }
                }
            } else {
                var vcc1 = $"{v} {string.Join("", cc)}-";
                var vcc2 = $"{v}{string.Join(" ", cc)}-";
                var vcc3 = $"{v}{cc[0]} {cc[0 + 1]}-";
                var vcc4 = $"{v}{cc[0]} {cc[0 + 1]}";
                if (HasOto(vcc1, ending.tone)) {
                    phonemes.Add(vcc1);
                } else if (HasOto(vcc2, ending.tone)) {
                    phonemes.Add(vcc2);
                } else if (HasOto(vcc3, ending.tone)) {
                    phonemes.Add(vcc3);
                } else {
                    if (HasOto(vcc4, ending.tone)) {
                        phonemes.Add(vcc4);
                    } else if (!HasOto(vcc4, ending.tone)) {
                        phonemes.Add($"{v} {cc[0]}");
                    }
                    // all CCs except the first one are /C1C2/, the last one is /C1 C2-/
                    // but if there is no /C1C2/, we try /C1 C2-/, vise versa for the last one
                    for (var i = 0; i < cc.Length - 1; i++) {
                        var cc1 = $"{cc[i]} {cc[i + 1]}";
                        if (!HasOto(cc1, ending.tone)) {
                            cc1 = $"{cc[i]}{cc[i + 1]}";
                        }
                        if (!HasOto(cc1, ending.tone) && !HasOto($"{cc[i]}{cc[i + 1]}", ending.tone)) {
                            cc1 = $"{cc[i]} {cc[i + 1]}-";
                        }
                        if (i < cc.Length - 2) {
                            var cc2 = $"{cc[i]} {string.Join("", cc.Skip(i))}-";
                            var cc3 = $"{cc[i]} {cc[i + 1]}{cc[i + 2]}-";
                            if (HasOto(cc2, ending.tone)) {
                                phonemes.Add(cc2);
                                i++;
                            } else if (HasOto(cc3, ending.tone)) {
                                // like [C1 C2][C2 ...]
                                phonemes.Add(cc3);
                                i++;
                            } else {
                                if (HasOto(cc1, ending.tone) && (!HasOto(vcc4, ending.tone))) {
                                    // like [C1 C2][C2 ...]
                                    phonemes.Add(cc1);
                                } else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} {cc[i + 2]}-")) {
                                    // like [C1 C2-][C2 ...]
                                    i++;
                                } else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} {cc[i + 2]}")) {
                                    // like [C1 C2][C3 ...]
                                    if (cc[i + 2] == cc.Last()) {
                                        TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 2]} -");
                                        i++;
                                    } else {
                                        continue;
                                    }
                                } else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]}{cc[i + 2]}")) {
                                    // like [C1C2][C3 ...]
                                } else if (!cc.First().Contains(cc[i + 1]) || !cc.First().Contains(cc[i + 2])) {
                                    // like [C1][C2 ...]
                                    if (affricates.Contains(cc[i]) && (!HasOto(vcc4, ending.tone))) {
                                        TryAddPhoneme(phonemes, ending.tone, cc[i], $"{cc[i]} -");
                                    }
                                    TryAddPhoneme(phonemes, ending.tone, cc[i + 1], $"{cc[i + 1]} -");
                                    TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 2]} -", cc[i + 2]);
                                    i++;
                                } else if (!cc.First().Contains(cc[i])) {
                                    // like [C1][C2 ...]
                                    TryAddPhoneme(phonemes, ending.tone, cc[i], $"{cc[i]} -");
                                    i++;
                                }
                            }
                        } else {
                            if (!HasOto(vcc4, ending.tone)) {
                                if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i]} {cc[i + 1]}-")) {
                                // like [C1 C2-]
                                i++;
                            } else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i]}{cc[i + 1]}")) {
                                // like [C1C2][C2 -]
                                if (affricates.Contains(cc[i + 1])) {
                                    TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} -", cc[i + 1]);
                                } else {
                                    TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} -");
                                }
                                i++;
                            } else if (TryAddPhoneme(phonemes, ending.tone, cc1)) {
                                // like [C1 C2][C2 -]
                                if (affricates.Contains(cc[i + 1])) {
                                   TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} -", cc[i + 1]);
                                } else {
                                   TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} -");
                                }
                                i++;
                            } else {
                                // like [C1][C2 -]
                                if (!HasOto(vcc4, ending.tone)) {
                                    TryAddPhoneme(phonemes, ending.tone, cc[i], $"{cc[i]} -");
                                    if (!affricates.Contains(cc[0])) {
                                        phonemes.Remove(cc[0]);
                                    }
                                    TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} -", cc[i + 1]);
                                    i++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return phonemes;
        }

        protected override string ValidateAlias(string alias)
        {
            foreach (var vowel in new[] { "V" })
            {
                alias = alias.Replace(vowel, "A");
            }
            return alias;
        }

        protected override double GetTransitionBasicLengthMs(string alias = "") {
            foreach (var c in longConsonants) {
                if (alias.Contains(c)) {
                    if (!alias.StartsWith(c)) {
                        return base.GetTransitionBasicLengthMs() * 2.0;
                    }
                }
            }
            foreach (var c in normalConsonants) {
                if (!alias.Contains("_D")) {
                    if (alias.Contains(c)) {
                        if (!alias.StartsWith(c)) {
                            return base.GetTransitionBasicLengthMs();
                        }
                    }   
                }
            }

            foreach (var c in shortConsonants) {
                if (alias.Contains(c)) {
                    if (!alias.Contains(" _")) {
                        return base.GetTransitionBasicLengthMs() * 0.50;
                    }
                }
            }
            return base.GetTransitionBasicLengthMs();
        }
    }
}

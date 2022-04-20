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
        /// Based on Teren000's reclist, but with some adjustments.
        /// (To be precise, it's based on Hoshino Hanami "Mariposa" Spanish.)
        /// Supports both CVVC and VCV if the voicebank has it.
        /// There's some ValidateAlias support in this phonemizer, but it's not complete yet.
        /// For now, typing ex. [n i n y o] (in the brackets) after the lyric "niño" should work.
        /// (This currently does work automatically with ValidateAlias, but only for CVVC, not VCV.)
        /// Same with typing [s] instead of [z] for voicebanks with "seseo" (Latin-American) accents.
        /// I also want to add using "u" instead of "w" and "i" instead of "y" depending on the voicebank.
        /// Ex. "kua" instead of "kwa".
        ///</summary>

        private readonly string[] vowels = "a,e,i,o,u".Split(',');
        private readonly string[] consonants = "b,ch,d,dz,f,g,h,hh,j,k,l,ll,m,n,nh,p,r,rr,s,sh,t,ts,w,y,z,zz,zh".Split(',');
        private readonly Dictionary<string, string> dictionaryReplacements = ("a=a;e=e;i=i;o=o;u=u;" +
                "b=b;ch=ch;d=d;dz=dz;f=f;g=g;gn=nh;h=h;k=k;l=l;ll=j;m=m;n=n;p=p;r=r;rr=rr;s=s;sh=sh;t=t;ts=ts;w=w;y=y;z=z;zz=zz;zh=zh;I=i;U=u").Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);

        private readonly string[] longConsonants = "ch,dz,k,p,s,sh,t,ts,z,l,m,n".Split(',');

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
            if (syllable.IsStartingV) {
                basePhoneme = rcv;
                if (!HasOto(rcv, syllable.vowelTone)) {
                    basePhoneme = $"{v}";
                }
            } else if (syllable.IsVV) {
                basePhoneme = $"{prevV} {v}";
                if (!HasOto(basePhoneme, syllable.vowelTone)) {
                    basePhoneme = $"{v}";
                }
            } else if (syllable.IsStartingCVWithOneConsonant) {
                // TODO: move to config -CV or -C CV
                var rc = $"- {cc[0]}{v}";
                if (HasOto(rc, syllable.vowelTone)) {
                    basePhoneme = rc;
                } else {
                    basePhoneme = $"{cc[0]}{v}";
                    if (consonants.Contains(cc[0])) {
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
                    // for (var i = cc.Length; i > 1; i--) {
                    // if (TryAddPhoneme(phonemes, syllable.tone, $"- {string.Join("", cc.Take(i))}")) {
                    // firstC = i;
                    // break;
                    // }
                    // }
                    // if (phonemes.Count == 0) {
                    // TryAddPhoneme(phonemes, syllable.tone, $"- {cc[0]}");
                    // }
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
                if (HasOto(vcv, syllable.vowelTone) && (syllable.IsVCVWithOneConsonant)) {
                    basePhoneme = vcv;
                } else if (HasOto(vccv, syllable.vowelTone) && (syllable.IsVCVWithMoreThanOneConsonant)) {
                    basePhoneme = vccv;
                } else {
                        // try vcc
                        for (var i = lastC + 1; i >= 0; i--) {
                        if (i == 0) {
                            phonemes.Add($"{prevV} {cc[0]}");
                            break;
                        }
                        var vcc = $"{prevV} {string.Join("", cc.Take(i))}";
                        if (HasOto(vcc, syllable.tone)
                            && string.Join("", cc.Take(i)) != "dz"
                            && string.Join("", cc.Take(i)) != "nh"
                            && string.Join("", cc.Take(i)) != "sh"
                            && string.Join("", cc.Take(i)) != "zh") {
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
                            if (HasOto(ccv, syllable.vowelTone)
                                && string.Join("", cc.Skip(i)) != "dz"
                                && string.Join("", cc.Skip(i)) != "nh"
                                && string.Join("", cc.Skip(i)) != "sh"
                                && string.Join("", cc.Skip(i)) != "zh") {
                                lastC = i;
                                basePhoneme = ccv;
                                break;
                            }
                            else if (HasOto(ccv2, syllable.vowelTone)) {
                                lastC = i;
                                basePhoneme = ccv2;
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
                // if (!HasOto(cc1, syllable.tone))
                // {
                // cc1 = $"{cc[i]}{cc[i + 1]}";
                // }
                if (i + 1 < lastC) {
                    var cc2 = $"{cc[i + 1]} {cc[i + 2]}";
                    // if (!HasOto(cc2, syllable.tone))
                    // {
                    // cc2 = $"{cc[i + 1]}{cc[i + 2]}";
                    // }
                    if (HasOto(cc1, syllable.tone) && HasOto(cc2, syllable.tone)) {
                        // like [V C1] [C1 C2] [C2 C3] [C3 ..]
                        phonemes.Add(cc1);
                    } else if (TryAddPhoneme(phonemes, syllable.tone, $"{cc[i]}{cc[i + 1]}-")) {
                        // like [V C1] [C1 C2-] [C3 ..]
                        i++;
                    } else if (TryAddPhoneme(phonemes, syllable.tone, cc1)) {
                        // like [V C1] [C1 C2] [C2 ..]
                    } else {
                        // like [V C1] [C1] [C2 ..]
                        TryAddPhoneme(phonemes, syllable.tone, cc[i], $"{cc[i]}-");
                    }
                } else if (!syllable.IsStartingCVWithMoreThanOneConsonant) {
                    // like [V C1] [C1 C2]  [C2 ..] or like [V C1] [C1 -] [C3 ..]
                    TryAddPhoneme(phonemes, syllable.tone, cc1, cc[i], $"{cc[i]}-");
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
            var vr = $"{v} -";
            if (ending.IsEndingV && HasOto(vr, ending.tone))
            {   // ending V
                phonemes.Add(vr);
            } else if (ending.IsEndingVCWithOneConsonant)
            {   // ending VC
                var vcr = $"{v} {cc[0]}-";
                if (HasOto(vcr, ending.tone))
                {   // applies ending VC
                    phonemes.Add(vcr);
                } else
                {   // if no ending VC, then regular VC
                    phonemes.Add($"{v} {cc[0]}");
                }
            } else if (ending.IsEndingVCWithMoreThanOneConsonant)
            {   // ending VCC (very rare, usually only occurs in words ending with "x")
                var vccr = $"{v} {string.Join("", cc)}";
                if (HasOto(vccr, ending.tone))
                {   // applies ending VCC
                    phonemes.Add(vccr);
                } else if (!HasOto(vccr, ending.tone))
                {   // if no ending VCC, then CC transitions
                    phonemes.Add($"{v} {cc[0]}");
                    // all CCs except the first one are /C1C2/, the last one is /C1 C2-/
                    // but if there is no /C1C2/, we try /C1 C2-/, vise versa for the last one
                    for (var i = 0; i < cc.Length - 1; i++)
                    {
                        var cc1 = $"{cc[i]} {cc[i + 1]}";
                        if (i < cc.Length - 2)
                        {
                            var cc2 = $"{cc[i + 1]} {cc[i + 2]}";
                            if (HasOto(cc1, ending.tone) && HasOto(cc2, ending.tone))
                            {
                                // like [C1 C2][C2 ...]
                                phonemes.Add(cc1);
                            }
                            else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} {cc[i + 2]}"))
                            {
                                // like [C1 C2][C2 ...]
                            }
                            else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} {cc[i + 2]}-"))
                            {
                                // like [C1 C2-][C3 ...]
                                i++;
                            }
                            else
                            {
                                // like [C1][C2 ...]
                                TryAddPhoneme(phonemes, ending.tone, cc[i], $"{cc[i]} -");
                            }
                        }
                        else
                        {
                            if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i]} {cc[i + 1]}-"))
                            {
                                // like [C1 C2-]
                                i++;
                            }
                            else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i]} {cc[i + 1]}"))
                            {
                                // like [C1 C2][C2 -]
                                TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} -", cc[i + 1]);
                                i++;
                            }
                            else if (TryAddPhoneme(phonemes, ending.tone, cc1))
                            {
                                // like [C1 C2][C2 -]
                                TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} -", cc[i + 1]);
                                i++;
                            }
                            else
                            {
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
            foreach (var consonant in new[] { "nh" }) {
                foreach (var vowel in vowels) {
                    alias = alias.Replace(consonant + vowel, "ny" + vowel);
                }
            }
            if (alias == "a nh" ||
                alias == "e nh" ||
                alias == "i nh" ||
                alias == "o nh" ||
                alias == "u nh" ||
                alias == "l nh" ||
                alias == "m nh") {
                return alias.Replace("nh", "n");
            }
            if (alias == "gwa" ||
                alias == "gwe" ||
                alias == "gwi" ||
                alias == "gwo" )
                {
                return alias.Replace("w", "u");
            }
            foreach (var consonant in new[] { "z" }) {
                foreach (var vowel in vowels) {
                    alias = alias.Replace(consonant + vowel, "s" + vowel);
                }
            }
            foreach (var consonant in new[] { "w" }) {
                foreach (var vowel in vowels) {
                    alias = alias.Replace(consonant + vowel, "u" + vowel);
                }
            }
            foreach (var consonant in new[] { "y" }) {
                foreach (var vowel in vowels) {
                    alias = alias.Replace(consonant + vowel, "i" + vowel);
                }
            }
            return alias;
        }

        protected override double GetTransitionBasicLengthMs(string alias = "")
        {
            foreach (var c in new[] { "rr" }) {
                if (alias.EndsWith(c)) {
                    return base.GetTransitionBasicLengthMs() * 1.0;
                }
            }
            foreach (var c in new[] { "r" }) {
                if (alias.EndsWith(c))
                {
                    return base.GetTransitionBasicLengthMs() * 0.75;
                }
            }
            foreach (var c in longConsonants)
            {
                if (alias.EndsWith(c))
                {
                    return base.GetTransitionBasicLengthMs() * 2.0;
                }
            }
            return base.GetTransitionBasicLengthMs();
        }

    }
}

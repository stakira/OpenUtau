using System.Collections.Generic;
using System.Linq;
using OpenUtau.Plugin.Builtin;

    namespace OpenUtau.Api {
    [Phonemizer("Spanish Syllable-Based Phonemizer", "ES SYL", "Lotte V")]
    public class SpanishSyllableBasedPhonemizer : SyllableBasedPhonemizer {

        /// <summary>
        /// Spanish syllable-based phonemizer by Lotte V
        /// Based on Teren000's reclist, but with some adjustments
        /// Supports both CVVC and VCV
        /// </summary>

        private readonly string[] vowels = "a,e,i,o,u,L,M,N".Split(',');
        private readonly string[] consonants = "b,ch,d,dz,f,g,h,hh,j,k,l,m,n,nh,p,r,rr,s,sh,t,ts,v,vv,w,y,z,zh,zz".Split(',');
        private readonly Dictionary<string, string> dictionaryReplacements = ("a=a;e=e;i=i;o=o;u=u;" +
                "b=b;ch=ch;d=d;dz=dz;f=f;g=g;h=h;hh=hh;k=k;l=l;ll=j;m=m;n=n;gn=nh;p=p;r=r;rr=rr;s=s;sh=sh;t=t;ts=ts;v=v;vv=vv;w=w;x=ks;y=y;z=z;zh=zh;zz=zz").Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);

        private readonly string[] shortConsonants = "r".Split(',');
        private readonly string[] longConsonants = "ch,dz,f,h,k,p,s,sh,t,ts,z".Split(',');

        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => consonants;
        protected override string GetDictionaryName() => "cmudict_es.txt";

        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => dictionaryReplacements;

        protected override string ValidateAlias(string alias)
        {
            if (alias == "L")
                alias = "l";
            if (alias == "M")
                alias = "m";
            if (alias == "N")
                alias = "n";
            if (alias == "nh")
                alias = "n" + "y";
            if (alias == "z")
                alias = "s";
            if (alias == "b" + "y")
                alias = "b" + "i";
            if (alias == "b" + "w")
                alias = "b" + "u";
            if (alias == "ch" + "w")
                alias = "ch" + "u";
            if (alias == "d" + "y")
                alias = "d" + "i";
            if (alias == "d" + "w")
                alias = "d" + "u";
            if(alias == "f" + "y")
                alias = "f" + "i";
            if (alias == "f" + "w")
                alias = "f" + "u";
            if (alias == "g" + "y")
                alias = "g" + "i";
            if (alias == "g" + "w")
                alias = "g" + "u";
            if (alias == "h" + "y")
                alias = "h" + "i";
            if (alias == "h" + "w")
                alias = "h" + "u";
            if (alias == "j" + "y")
                alias = "j" + "i";
            if (alias == "j" + "w")
                alias = "j" + "u";
            if (alias == "k" + "y")
                alias = "k" + "i";
            if (alias == "k" + "w")
                alias = "k" + "u";
            if (alias == "l" + "y")
                alias = "l" + "i";
            if (alias == "l" + "w")
                alias = "l" + "u";
            if (alias == "m" + "y")
                alias = "m" + "i";
            if (alias == "m" + "w")
                alias = "m" + "u";
            if (alias == "n" + "y")
                alias = "n" + "i";
            if (alias == "n" + "w")
                alias = "n" + "u";
            if (alias == "p" + "y")
                alias = "p" + "i";
            if (alias == "p" + "w")
                alias = "p" + "u";
            if (alias == "r" + "y")
                alias = "r" + "i";
            if (alias == "r" + "w")
                alias = "r" + "u";
            if (alias == "rr" + "y")
                alias = "rr" + "i";
            if (alias == "rr" + "w")
                alias = "rr" + "u";
            if (alias == "s" + "y")
                alias = "s" + "i";
            if (alias == "s" + "w")
                alias = "s" + "u";
            if (alias == "t" + "y")
                alias = "t" + "i";
            if (alias == "t" + "w")
                alias = "t" + "u";
            if (alias == "v" + "y")
                alias = "v" + "i";
            if (alias == "v" + "w")
                alias = "v" + "u";
            if (alias == "z" + "y")
                alias = "z" + "i";
            if (alias == "z" + "w")
                alias = "z" + "u";
            if (alias == "k" + "s")
                alias = "x";
            return alias;

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
                var vcv = $"{prevV} {cc[0]}{v}";
                if (HasOto(vcv, syllable.vowelTone) && (syllable.IsVCVWithOneConsonant)) {
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
            } else {
                phonemes.Add($"{v} {cc[0]}-");
                for (var i = 1; i < cc.Length; i++) {
                    var cr = $"{cc[i]} -";
                    phonemes.Add(HasOto(cr, ending.tone) ? cr : cc[i]);
                }
            }

            return phonemes;
        }

        protected override double GetTransitionBasicLengthMs(string alias = "") {
            foreach (var c in shortConsonants) {
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

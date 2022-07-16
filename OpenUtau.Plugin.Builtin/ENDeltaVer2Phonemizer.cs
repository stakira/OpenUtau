using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenUtau.Api;
using Serilog;

namespace OpenUtau.Plugin.Builtin
{
    [Phonemizer("Delta English (Version 2) Phonemizer", "EN Delta (Ver2)", "Lotte V")]
    public class ENDeltaVer2Phonemizer : SyllableBasedPhonemizer
    {
        /// <summary>
        /// General English phonemizer for Delta list (X-SAMPA) voicebanks.
        /// This version is based on the third version of Delta's list, with split diphthongs.
        /// There is also a version of the phonemizer based on the first version of the list, with "whole" diphthongs.
        /// This version of the phonemizer also supports slightly less extra sounds, for practical reasons.
        /// But it still contains some support for North-American sounds.
        ///</summary>

        private readonly string[] vowels = "a,A,@,{,V,O,aU,aI,E,3,eI,I,i,oU,OI,U,u,Q,{~,I~,e,o,l＝,m＝,n＝,N＝".Split(',');
        private readonly string[] consonants = "b,tS,d,D,4,f,g,h,dZ,k,l,m,n,N,p,r,s,S,t,T,v,w,j,z,Z,t_},・".Split(',');
        private readonly string[] burstConsonants = "b,tS,d,dZ,4,g,k,p,t".Split(',');
        private readonly string[] longConsonants = "tS,dZ,s,S,k,p,t,T,z,Z,l,m,n,N,t_}".Split(',');
        private readonly string[] shortConsonants = "4".Split(',');
        private readonly string[] affricates = "tS,dZ".Split(',');
        private readonly Dictionary<string, string> dictionaryReplacements = ("aa=A;ae={;ah=V;ao=O;aw=aU;ax=@;ay=aI;" +
            "b=b;ch=tS;d=d;dh=D;dx=4;eh=E;el=l＝;em=m＝;en=n＝;eng=ng＝;er=3;ey=eI;f=f;g=g;hh=h;ih=I;iy=i;jh=dZ;k=k;l=l;m=m;n=n;ng=N;ow=oU;oy=OI;" +
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

        protected override string[] GetSymbols(Note note)
        {
            string[] original = base.GetSymbols(note);
            if (original == null)
            {
                return null;
            }
            List<string> modified = new List<string>();
            string[] diphthongs = new[] { "aI", "eI", "OI", "aU", "oU" };
            foreach (string s in original)
            {
                if (diphthongs.Contains(s))
                {
                    modified.AddRange(new string[] { s[0].ToString(), s[1].ToString() });
                }
                else
                {
                    modified.Add(s);
                }
            }
            return modified.ToArray();
        }

        protected override List<string> ProcessSyllable(Syllable syllable)
        {
            string prevV = syllable.prevV;
            string[] cc = syllable.cc;
            string v = syllable.v;

            string basePhoneme;
            var phonemes = new List<string>();
            var lastC = cc.Length - 1;
            var firstC = 0;
            if (syllable.IsStartingV)
            {
                basePhoneme = $"- {v}";
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
                if (!HasOto($"{prevV} {v}", syllable.vowelTone))
                {
                    basePhoneme = $"{v}";
                }
            }
            else if (syllable.IsStartingCVWithOneConsonant)
            {
                // TODO: move to config -CV or -C CV
                var rcv = $"- {cc[0]}{v}";
                if (HasOto(rcv, syllable.vowelTone))
                {
                    basePhoneme = rcv;
                }
                else if (v == "V" && !HasOto(rcv, syllable.vowelTone) && HasOto($"- {cc[0]}@", syllable.vowelTone))
                {
                    basePhoneme = $"- {cc[0]}@";
                }
                else
                {
                    basePhoneme = $"{cc[0]}{v}";
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
                    // try RCC
                    for (var i = cc.Length; i > 1; i--)
                    {
                        if (TryAddPhoneme(phonemes, syllable.tone, $"- {string.Join("", cc.Take(i))}"))
                        {
                            firstC = i;
                            break;
                        }
                    }
                    if (phonemes.Count == 0)
                    {
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
                else if (prevV == "V" && !HasOto($"V {cc[0]}", syllable.vowelTone) && !HasOto(vcv, syllable.vowelTone) && !HasOto(vccv, syllable.vowelTone) && HasOto($"@ {string.Join("", cc)}", syllable.vowelTone))
                {
                    basePhoneme = $"{cc.Last()}{v}";
                    phonemes.Add($"@ {string.Join("", cc)}");
                }
                else
                {
                    // try CCV
                    basePhoneme = cc.Last() + v;
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
                        if (i == 0)
                        {
                            phonemes.Add($"{prevV} -");
                            break;
                        }
                        var vcc = $"{prevV} {string.Join("", cc.Take(i))}";
                        if (HasOto(vcc, syllable.tone))
                        {
                            phonemes.Add(vcc);
                            firstC = i - 1;
                            break;
                        }
                    }
                }
            }
            for (var i = firstC; i < lastC; i++)
            {
                // we could use some CCV, so lastC is used
                // we could use -CC so firstC is used
                var cc1 = $"{cc[i]} {cc[i + 1]}";
                if (!HasOto(cc1, syllable.tone))
                {
                    cc1 = $"{cc[i]}{cc[i + 1]}";
                }
                if (i + 1 < lastC)
                {
                    var cc2 = $"{cc[i + 1]} {cc[i + 2]}";
                    if (!HasOto(cc2, syllable.tone))
                    {
                        cc2 = $"{cc[i + 1]}{cc[i + 2]}";
                    }
                    if (HasOto(cc1, syllable.tone) && HasOto(cc2, syllable.tone))
                    {
                        // like [V C1] [C1 C2] [C2 C3] [C3 ..]
                        phonemes.Add(cc1);
                    }
                    else if (TryAddPhoneme(phonemes, syllable.tone, cc1))
                    {
                        // like [V C1] [C1 C2] [C2 ..]
                    }
                    else if (TryAddPhoneme(phonemes, syllable.tone, $"{cc[i]} {cc[i + 1]}-"))
                    {
                        // like [V C1] [C1 C2-] [C3 ..]
                        i++;
                    }
                    else if (burstConsonants.Contains(cc[i]) && !syllable.IsStartingCVWithMoreThanOneConsonant)
                    {
                        // like [V C1] [C1] [C2 ..]
                        TryAddPhoneme(phonemes, syllable.tone, cc[i], $"{cc[i]} -");
                        if (cc[i] == cc.Last() && !affricates.Contains(cc[i]))
                        {
                            phonemes.Remove(cc[i]);
                            phonemes.Remove($"{cc[i]} -");
                            //if ("tS" == cc.Last() || "tS" == cc[1])
                            //{
                            //    TryAddPhoneme(phonemes, syllable.tone, "tS", $"tS -");
                            //}
                            //else if ("dZ" == cc.Last() || "dZ" == cc[1])
                            //{
                            //    TryAddPhoneme(phonemes, syllable.tone, "dZ", $"dZ -");
                            //}
                        }
                    }
                }
                else //if (burstConsonants.Contains(cc[i]) && !syllable.IsStartingCVWithMoreThanOneConsonant)
                {
                    // like [V C1] [C1 C2]  [C2 ..] or like [V C1] [C1 -] [C3 ..]
                    TryAddPhoneme(phonemes, syllable.tone, cc1);
                    if (!HasOto(cc1, syllable.tone) && burstConsonants.Contains(cc[i]) && !syllable.IsStartingCVWithMoreThanOneConsonant)
                    {
                        TryAddPhoneme(phonemes, syllable.tone, cc[i], $"{cc[i]} -");
                    }
                    if (cc[i] == cc.Last() && !affricates.Contains(cc[i]))
                    {
                        phonemes.Remove(cc[i]);
                        phonemes.Remove($"{cc[i]} -");
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
            if (ending.IsEndingV)
            {
                phonemes.Add($"{v} -");
            }
            else if (ending.IsEndingVCWithOneConsonant)
            {
                var vcr = $"{v} {cc[0]}-";
                if (HasOto(vcr, ending.tone))
                {
                    phonemes.Add(vcr);
                }
                else if (v == "V" && !HasOto(vcr, ending.tone) && HasOto($"@ {cc[0]}-", ending.tone))
                {
                    phonemes.Add($"@ {cc[0]}-");
                }
                else
                {
                    phonemes.Add($"{v} {cc[0]}");
                    TryAddPhoneme(phonemes, ending.tone, $"{cc[0]} -");
                    if (burstConsonants.Contains(cc[0]))
                    {
                        TryAddPhoneme(phonemes, ending.tone, $"{cc[0]} -", cc[0]);
                    }
                }
            }
            else
            {
                phonemes.Add($"{v} {cc[0]}");
                // all CCs except the first one are /C1C2/, the last one is /C1 C2-/
                // but if there is no /C1C2/, we try /C1 C2-/, vise versa for the last one
                for (var i = 0; i < cc.Length - 1; i++)
                {
                    var cc1 = $"{cc[i]} {cc[i + 1]}";
                    if (i < cc.Length - 2)
                    {
                        //var cc2 = $"{cc[i + 1]} {cc[i + 2]}";
                        if (HasOto(cc1, ending.tone)) //&& HasOto(cc2, ending.tone))
                        {
                            // like [C1 C2][C2 ...]
                            phonemes.Add(cc1);
                        }
                        else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]}{cc[i + 2]}"))
                        {
                            // like [C1C2][C2 ...]
                        }
                        else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} {cc[i + 2]}-"))
                        {
                            // like [C1 C2-][C3 ...]
                            i++;
                        }
                        else if (!cc.First().Contains(cc[i]))
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
                        else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i]}{cc[i + 1]}")) //&& !HasOto(cc1, ending.tone))
                        {
                            // like [C1C2][C2 -]
                            TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} -", cc[i + 1]);
                            i++;
                        }
                        else if (TryAddPhoneme(phonemes, ending.tone, cc1)) //&& !HasOto(cc1, ending.tone))
                        {
                            // like [C1 C2][C2 -]
                            TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} -", cc[i + 1]);
                            i++;
                        }
                        else //if (!$"{v} {cc[0]}".Contains(cc[i]))
                        {
                            // like [C1][C2 -]
                            TryAddPhoneme(phonemes, ending.tone, cc[i], $"{cc[i]} -");
                            phonemes.Remove(cc[0]);
                            TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} -", cc[i + 1]);
                            i++;
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
                alias = alias.Replace(vowel, "@");
            }
            return alias;
        }

        protected override double GetTransitionBasicLengthMs(string alias = "")
        {
            foreach (var c in shortConsonants)
            {
                if (alias.EndsWith(c))
                {
                    return base.GetTransitionBasicLengthMs() * 0.50;
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

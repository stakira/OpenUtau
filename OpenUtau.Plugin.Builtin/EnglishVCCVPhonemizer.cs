using System;
using System.Collections.Generic;
using System.Text;
using OpenUtau.Api;
using System.Linq;


namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("English VCCV Phonemizer", "EN VCCV", "Mim")]
    // This is a temporary solution until Cz's comes out with their own.
    public class EnglishVCCVPhonemizer : SyllableBasedPhonemizer {

        private readonly string[] vowels = "a,@,u,0,8,I,e,3,A,i,E,O,Q,6,o,1ng".Split(",");
        private readonly string[] consonants = "b,ch,d,dh,f,g,h,j,k,l,m,n,ng,p,r,s,sh,t,th,v,w,y,z,zh".Split(",");
        private readonly Dictionary<string, string> dictionaryReplacements = ("aa=a;ae=@;ah=u;ao=0;aw=8;ay=I;" +
            "b=b;ch=ch;d=d;dh=dh;eh=e;er=3;ey=A;f=f;g=g;hh=h;ih=i;iy=E;jh=j;k=k;l=l;m=m;n=n;ng=ng;ow=O;oy=Q;" +
            "p=p;r=r;s=s;sh=sh;t=t;th=th;uh=6;uw=o;v=v;w=w;y=y;z=z;zh=zh;").Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);

        private readonly Dictionary<string, string> vcExceptions =
            new Dictionary<string, string>() {
                {"i ng","1ng"},
                {"ing","1ng"},
                {"0 r","0r"},
                {"0r","0r"},
                {"e r","Ar"},
                {"er","Ar"},
                {"0 l","0l"},
                {"0l","0l"},
                {"@ m","&m"},
                {"@m","&m"},
                {"& m","&m"},
                {"&m","&m"},
                {"@ n","&n"},
                {"@n","&n"},
                {"& n","&n"},
                {"&n","&n"},
                {"8 n","8n"},
                {"8n","8n"},
            };

        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => consonants;
        protected override string GetDictionaryName() => "cmudict-0_7b.txt";
        protected override IG2p LoadBaseDictionary() => new ArpabetG2p();
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => dictionaryReplacements;


        protected override List<string> ProcessSyllable(Syllable syllable) {
            string prevV = syllable.prevV;
            string[] cc = syllable.cc;
            string v = syllable.v;
            var lastC = cc.Length - 1;

            string basePhoneme = null;
            var phonemes = new List<string>();
            // --------------------------- STARTING V ------------------------------- //
            if (syllable.IsStartingV) {
                // if starting V -> -V
                basePhoneme = $"-{v}";

                // --------------------------- STARTING VV ------------------------------- //
            } else if (syllable.IsVV) {  // if VV
                if (!CanMakeAliasExtension(syllable)) {
                    //try V V
                    basePhoneme = $"{prevV}{v}";
                    //else try _V
                    if (!HasOto(basePhoneme, syllable.vowelTone)) {
                        basePhoneme = $"{prevV} {v}";
                        if (!HasOto(basePhoneme, syllable.vowelTone)) {
                            basePhoneme = $"_{v}";
                        }
                    }
                    if ($"{prevV}" == $"{v}") {
                        basePhoneme = $"{v}";
                    }
                }
                // --------------------------- STARTING CV ------------------------------- //
            } else if (syllable.IsStartingCVWithOneConsonant) {
                //if starting CV -> -CV
                basePhoneme = $"-{cc[0]}{v}";

                // --------------------------- STARTING CCV ------------------------------- //
            } else if (syllable.IsStartingCVWithMoreThanOneConsonant) {

                basePhoneme = $"_{cc.Last()}{v}";
                if (!HasOto(basePhoneme, syllable.tone)) {
                    basePhoneme = $"{cc.Last()}{v}";
                }
                // add -CC

                // try CCVs

                var ccv = $"";
                if (cc.Length == 2) {
                    ccv = $"-{cc[0]}{cc[1]}{v}";
                    if (HasOto(ccv, syllable.tone)) {
                        basePhoneme = ccv;
                    }
                }
                if (cc.Length == 3) {
                    ccv = $"-{cc[0]}{cc[1]}{cc[2]}";
                    if (HasOto(ccv, syllable.tone)) {
                        phonemes.Add(ccv);
                    }
                }

                if (!HasOto(ccv, syllable.tone)) {
                    // other CCs
                    for (var i = 0; i < lastC; i++) {
                        var currentCc = $"{cc[i]}{cc[i + 1]}";
                        if (i == 0 && HasOto($"-{cc[i]}{cc[i + 1]}", syllable.tone)) {
                            currentCc = $"-{cc[i]}{cc[i + 1]}";
                        }
                        if (HasOto(currentCc, syllable.tone)) {
                            phonemes.Add(currentCc);
                        }
                    }
                }
            }
                // --------------------------- IS VCV ------------------------------- //
                else {
                //try _CV else add CV 
                if (syllable.IsVCVWithOneConsonant) {
                    basePhoneme = $"{cc.Last()}{v}";

                    var vc = $"{prevV} {cc[0]}";

                    vc = CheckVCExceptions(vc);
                    phonemes.Add(vc);

                } else {
                    basePhoneme = $"_{cc.Last()}{v}";
                    if (!HasOto(basePhoneme, syllable.tone)) {
                        basePhoneme = $"{cc.Last()}{v}";
                    }

                    var vc = $"{prevV} {cc[0]}";


                    vc = CheckVCExceptions(vc);

                    // replace "V C" with "VC" if theres no CC transition
                    if (!HasOto($"{cc[0]}{cc[1]}", syllable.tone) && !HasOto($"{cc[0]} {cc[1]}", syllable.tone)) {
                        vc = $"{prevV}{cc[0]}";
                    }

                    phonemes.Add(vc);

                    var ccv = $"";
                    if (cc.Length == 2) {
                        ccv = $"{cc[0]}{cc[1]}{v}";
                        if (HasOto(ccv, syllable.tone)) {
                            basePhoneme = ccv;
                        }
                    }
                    if (cc.Length == 3) {
                        ccv = $"{cc[0]}{cc[1]}{cc[2]}";
                        if (HasOto(ccv, syllable.tone)) {
                            phonemes.Add(ccv);
                        }
                    }

                    if (!HasOto(ccv, syllable.tone)) {
                        for (var i = 0; i < lastC; i++) {
                            var currentCc = $"";

                            if(i == 0) {
                                var xccv = $"{vc} {cc[i + 1]}";
                                if (HasOto(xccv, syllable.tone)) {
                                    phonemes.Add(xccv);
                                    continue;
                                }
                            }

                            if (i == lastC - 2) {
                                currentCc = $"{cc[i]}{cc[i + 1]}{cc[i + 2]}";
                                if (HasOto(ccv, syllable.tone)) {
                                    phonemes.Add(currentCc);
                                    break;
                                }
                            }
                            if (i == lastC - 1) {
                                ccv = $"{cc[i]}{cc[i + 1]}{v}";
                                if (HasOto(ccv, syllable.tone)) {
                                    basePhoneme = ccv;
                                    break;
                                }
                            }

                            currentCc = $"{cc[i]} {cc[i + 1]}";
                            if (!HasOto(currentCc, syllable.tone)) {
                                currentCc = $"{cc[i]}{cc[i + 1]}";
                            }

                            if (HasOto(currentCc, syllable.tone)) {
                                phonemes.Add(currentCc);
                            }

                        }
                    }
                }

            }

            if (!HasOto(basePhoneme, syllable.tone)) {
                basePhoneme = $"-{v}";
            }
            phonemes.Add(basePhoneme);
            return phonemes;
        }

        protected override List<string> ProcessEnding(Ending ending) {
            string[] cc = ending.cc;
            string v = ending.prevV;
            var lastC = cc.Length - 1;

            var phonemes = new List<string>();
            // --------------------------- ENDING V ------------------------------- //
            if (ending.IsEndingV) {
                // try V- else no ending
                TryAddPhoneme(phonemes, ending.tone, $"{v}-");

            } else {
                // --------------------------- ENDING VC ------------------------------- //
                if (ending.IsEndingVCWithOneConsonant) {
                    var vc = $"{v}{cc[0]}";
                    
                    vc = CheckVCExceptions(vc);
                    vc += "-";
                    phonemes.Add(vc);

                } else {
                    var vc = $"{v} {cc[0]}";

                    vc = CheckVCExceptions(vc);
                    phonemes.Add(vc);

                    // --------------------------- ENDING VCC ------------------------------- //
                    for (var i = 0; i < lastC - 1; i++) {
                        var currentCc = $"{cc[i]} {cc[i + 1]}";
                        if (!HasOto(currentCc, ending.tone)) {
                            currentCc = $"{cc[i]}{cc[i + 1]}";
                        }
                        if (HasOto(currentCc, ending.tone)) {
                            phonemes.Add(currentCc);
                        }
                    }

                    TryAddPhoneme(phonemes, ending.tone, $"{cc[lastC - 1]}{cc[lastC]}-");

                }
            }

            // ---------------------------------------------------------------------------------- //

            return phonemes;
        }




        //protected override double GetTransitionBasicLengthMs(string alias = "") {
        //    //foreach (var c in shortConsonants) {
        //    //    if (alias.EndsWith(c)) {
        //    //        return base.GetTransitionBasicLengthMs() * 0.75;
        //    //    }
        //    //}
        //    //foreach (var c in longConsonants) {
        //    //    if (alias.EndsWith(c)) {
        //    //        return base.GetTransitionBasicLengthMs() * 1.5;
        //    //    }
        //    //}
        //    return base.GetTransitionBasicLengthMs() * 0.75;
        //}

        private string CheckVCExceptions(string vc) {
            if(vcExceptions.ContainsKey(vc)) {
                vc = vcExceptions[vc];
            }
            return vc;
        }

    }
}

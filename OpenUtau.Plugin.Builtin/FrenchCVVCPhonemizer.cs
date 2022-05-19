using System;
using System.Collections.Generic;
using System.Text;
using OpenUtau.Api;
using System.Linq;


namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("French CVVC Phonemizer", "FR CVVC", "Mim")]
    // Contributed by Mim with the help of Heiden.BZR & nago's phonemizers

    //This is a first implementation and I'm already working on optimization 
    public class FrenchCVVCPhonemizer : SyllableBasedPhonemizer {

        private readonly string[] vowels = "ah,ae,eh,ee,oe,ih,oh,oo,ou,uh,en,in,on,oi,ui".Split(",");
        private readonly string[] consonants = "b,d,f,g,j,k,l,m,n,p,r,s,sh,t,v,w,y,z,gn".Split(",");
        private readonly Dictionary<string, string> dictionaryReplacements = (
            "aa=ah;ai=ae;ei=eh;eu=ee;ee=ee;oe=oe;ii=ih;au=oh;oo=oo;ou=ou;uu=uh;an=en;in=in;un=in;on=on;uy=ui;" +
            "bb=b;dd=d;ff=f;gg=g;jj=j;kk=k;ll=l;mm=m;nn=n;pp=p;rr=r;ss=s;ch=sh;tt=t;vv=v;ww=w;yy=y;zz=z;gn=gn;").Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);


        private string[] shortConsonants = "r".Split(",");
        private string[] longConsonants = "t,k,g,p,s,sh,j".Split(",");
        private readonly string[] burstConsonants = "t,k,p,b,g,d".Split(",");

        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => consonants;
        protected override string GetDictionaryName() => "cmudict_fr.txt";
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => dictionaryReplacements;

        protected override List<string> ProcessSyllable(Syllable syllable) {
            string prevV = syllable.prevV;
            string[] cc = syllable.cc;
            string v = syllable.v;
            var lastC = cc.Length - 1;
            var firstC = 0;

            string basePhoneme;
            var phonemes = new List<string>();
            // --------------------------- STARTING V ------------------------------- //
            if (syllable.IsStartingV) {

                // try -V, - V then defaults to V
                basePhoneme = CheckAliasFormatting(v, "cv", syllable.vowelTone, "");

                // --------------------------- STARTING VV ------------------------------- //
            } else if (syllable.IsVV) {  // if VV
                if (!CanMakeAliasExtension(syllable)) {
                    basePhoneme = CheckAliasFormatting(v, "vv", syllable.vowelTone, prevV);
                } else {
                    // the previous alias will be extended
                    basePhoneme = null;
                }
                // --------------------------- STARTING CV ------------------------------- //
            } else if (syllable.IsStartingCVWithOneConsonant) {

                var cv = $"{cc[0]}{v}";
                basePhoneme = CheckAliasFormatting(cv, "cv", syllable.tone, "");

                if (!cv.Contains("-")) {
                    TryAddPhoneme(phonemes, syllable.tone, CheckAliasFormatting($"{cc[0]}", "rcv", syllable.tone, ""));
                }

                // --------------------------- STARTING CCV ------------------------------- //
            } else if (syllable.IsStartingCVWithMoreThanOneConsonant) {


                var rccv = $"{string.Join("", cc)}{v}";

                rccv = CheckAliasFormatting(rccv, "rcv", syllable.tone, "");
                if (HasOto(rccv, syllable.vowelTone)) {
                    basePhoneme = rccv;
                } else {
                    //try _CV else add CV
                    if (HasOto($"_{cc.Last()}{v}", syllable.vowelTone) && cc.Length == syllable.prevWordConsonantsCount + 1) {
                        basePhoneme = $"_{cc.Last()}{v}";
                    } else { basePhoneme = $"{cc.Last()}{v}"; }



                    int max = cc.Length;
                    int min = 0;

                    // try -CC of all lengths
                    for (int i = 0; i < cc.Length; i++) {
                        string rcc = "";

                        for (int k = 0; k < max; k++) {
                            rcc += $"{cc[k]}";
                        }
                        rcc = CheckAliasFormatting(rcc, "rcv", syllable.tone, "");
                        if (HasOto(rcc, syllable.tone)) {
                            phonemes.Add(rcc);
                            min = i+1;
                            break;
                        }
                        max--;
                    }

                    bool hasBroken = false;
                    //try CCV of all lengths, if there's one, jump the CC of all lengths
                    for (var i = min; i < cc.Length; i++) {
                        rccv = "";

                        for (var k = min; k < cc.Length; k++) {
                            rccv += $"{cc[k]}";
                        }

                        rccv += $"{v}";

                        phonemes.Add(rccv);

                        if (HasOto(rccv, syllable.vowelTone)) {
                            basePhoneme = rccv;
                            hasBroken = true;
                            break;
                        }
                        min++;
                    }

                    // reset min if no CCV has been placed
                    if (hasBroken) {
                        min = cc.Length - 1 - min;
                    } else min = cc.Length - 1;

                    if (max <= 0) { max = 1; }
                    var dec = cc.Length;

                    // try CC of all lengths
                    for (int i = max-1; i < cc.Length; i++) {
                        string rcc = "";

                        for (int k = max-1; k < dec; k++) {
                            rcc += $"{cc[k]}";
                        }
                        // dec condition removes unique "s"

                        if (HasOto(rcc, syllable.tone) && dec != 1) {
                            phonemes.Add(rcc);
                            break;
                        }
                        dec--;
                    }
                    
                    var index = 0;

                    for (int i = 0; i < phonemes.Count; i++) {
                        index = 0;
                        for (int k = 0; k < cc.Length; k++) {
                            if (!phonemes[i].Contains($"{cc[k]}")) {
                                break;
                            }
                            index++;
                        }

                    }

                    phonemes.Add($"{min}");
                    //add remaining CC
                    for (int i = index; i < min; i++) {
                        var ccc = $"{cc[i]}";
                        ccc = CheckAliasFormatting(ccc, "endccOe", syllable.tone, $"{cc[i + 1]}");
                        if (HasOto(ccc, syllable.tone)) {
                            phonemes.Add(ccc);
                            continue;
                        }
                    }
                }
            }
                // --------------------------- IS VCV ------------------------------- //
                else if (syllable.IsVCVWithOneConsonant) {

                // try VCV
                var vcv = $"{prevV} {cc[0]}{v}";
                if (HasOto(vcv, syllable.vowelTone)) {
                    basePhoneme = vcv;
                } else {
                    var cv = $"{cc[0]}{v}";
                    basePhoneme = cv;

                    var vc = CheckAliasFormatting(cc[0], "vc", syllable.tone, prevV);
                    phonemes.Add(vc);
                }
            } else {
                //try _CV else add CV
                if (HasOto($"_{cc.Last()}{v}", syllable.vowelTone) && cc.Length == syllable.prevWordConsonantsCount + 1) {
                    basePhoneme = $"_{cc.Last()}{v}";
                } else { basePhoneme = $"{cc.Last()}{v}"; }



                // try VCC else VC

                bool usedCC = false;
                for (var i = cc.Length; i >= 0; i--) {

                    var vcc = $"{string.Join("", cc.Take(i))}";

                    vcc = CheckAliasFormatting(vcc, "vc", syllable.tone, prevV);
                    if (HasOto(vcc, syllable.tone)) {
                        phonemes.Add(vcc);
                        //TODO: CLEAN FIRST C
                        firstC = i - 1;
                        break;
                    }
                }


                //if (phonemes.Count == 0 && $"{cc[0]}" != "gn") {
                //    phonemes.Add($"{prevV}{cc[0]}");
                //} else if (phonemes.Count == 0 && $"{cc[0]}" == "gn") {
                //    phonemes.Add($"{prevV}n");
                //}


                //for (var i = 0; i < cc.Length - 1; i++) {
                //    var currentCc = $"{cc[i]}{cc[i + 1]}";


                //    // french exclusion of "w" consonant, 
                //    if ($"{cc[i + 1]}" == "w" && $"{v}" == "ah") {
                //        continue;
                //    }

                //    //handling of exception "gn" sound
                //    if ($"{cc[i]}" == "gn" && cc[i] != cc.Last()) {
                //        phonemes.Add($"noe");
                //        phonemes.Add($"yoe");

                //        continue;
                //    }


                //    if (usedCC) {
                //        usedCC = false;
                //        continue;
                //    }

                //    //if (!HasOto(currentCc, syllable.tone) && cc[i + 1] != cc.Last()) {
                //    if (!HasOto(currentCc, syllable.tone)) {

                //        //remove first C
                //        if (i == 0) {
                //            continue;
                //        }

                //        phonemes.Add($"{cc[i]}oe");

                //        continue;
                //    }


                usedCC = true;
                //phonemes.Add(currentCc);
                //}

                //try CCV
                //TODO: clean
                //for (var i = firstC; i < cc.Length - 1; i++) {
                //    var ccv = string.Join("", cc.Skip(i)) + v;
                //    if (HasOto(ccv, syllable.tone)) {
                //        basePhoneme = ccv;
                //        break;
                //    }
                //}

            }

            phonemes.Add(basePhoneme);
            return phonemes;
        }
        protected override List<string> ProcessEnding(Ending ending) {
            string[] cc = ending.cc;
            string v = ending.prevV;

            var phonemes = new List<string>();
            bool hasEnding = false;
            // --------------------------- ENDING V ------------------------------- //
            if (ending.IsEndingV) {
                // try V- else no ending

                var endV = CheckAliasFormatting(v, "end", ending.tone, "");
                TryAddPhoneme(phonemes, ending.tone, endV);

            } else {
                // --------------------------- ENDING VC ------------------------------- //
                if (ending.IsEndingVCWithOneConsonant) {

                    var vc = CheckAliasFormatting($"{v}{cc[0]}", "endVc", ending.tone, "");
                    if (HasOto(vc, ending.tone)) {
                        phonemes.Add(vc);
                    } else {
                        vc = CheckAliasFormatting(cc[0], "vc", ending.tone, v);
                        phonemes.Add(vc);
                    }

                    if (!vc.Contains("-")) {
                        TryAddPhoneme(phonemes, ending.tone, CheckAliasFormatting($"{cc[0]}", "end", ending.tone, ""));
                    }

                    //hasEnding = TryAddPhoneme(phonemes, ending.tone, $"{v}{cc[0]}-");
                    //if ($"{cc[0]}" == "gn") {
                    //    phonemes.Add($"{v}n");
                    //    phonemes.Add($"yoe");
                    //    return phonemes;
                    //}

                } else {

                    // --------------------------- ENDING VCC ------------------------------- //
                    hasEnding = TryAddPhoneme(phonemes, ending.tone, $"{v}{cc[0]}{cc[1]}-");
                    if (!hasEnding) {
                        if (!TryAddPhoneme(phonemes, ending.tone, $"{v}{cc[0]}{cc[1]}")) {
                            phonemes.Add($"{v}{cc[0]}");
                        }
                    }

                    // add C1C2 or C2oe
                    for (var i = 0; i < cc.Length - 1; i++) {
                        var currentCc = $"{cc[i]}{cc[i + 1]}";
                        if (!HasOto(currentCc, ending.tone)) {
                            // french exclusion of "w" consonant
                            if ($"{cc[i + 1]}" == "w") {
                                continue;
                            }
                            if (i == 0) {
                                continue;

                            }
                            phonemes.Add($"{cc[i]}oe");
                            continue;
                        }
                        phonemes.Add(currentCc);
                    }

                }


            }

            //TODO: cleanup the whole CC conflict
            if (!hasEnding && cc.Length > 1) {
                TryAddPhoneme(phonemes, ending.tone, $"{cc.Last()}oe");
            }


            // ---------------------------------------------------------------------------------- //

            return phonemes;
        }


        protected override string ValidateAlias(string alias) {
            if (alias == "gn")
                alias = "n" + "y";
            return alias;

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
            return base.GetTransitionBasicLengthMs() * 1.25;
        }

        private string CheckAliasFormatting(string alias, string type, int tone, string prevV) {

            var aliasFormat = "";
            string[] aliasFormats = new string[] { "-", "- ", "", "-", " -", "", prevV, prevV + " ", "_", "", prevV, " " + prevV, "", "oe"};
            var startingI = 0;
            var endingI = aliasFormats.Length;

            if (type == "end") {
                startingI = 3;
                endingI = startingI + 1;
            }

            if (type == "endC") {
                startingI = 2;
                endingI = startingI + 1;
            }

            if (type == "endVc") {
                startingI = 3;
                endingI = startingI + 2;
            }

            if (type == "vc") {
                startingI = 6;
                endingI = startingI + 1;
            }

            //if (type == "cv") {
            //    startingI = 0;
            //    endingI = startingI + 1;
            //}

            if (type == "rcv") {
                startingI = 0;
                endingI = startingI + 1;
            }

            if (type == "vv") {
                startingI = 6;
                endingI = startingI + 3;
            }

            if (type == "cc") {
                startingI = 6;
                endingI = startingI + 1;
            }


            if (type == "endccOe") {
                startingI = 10;
                endingI = startingI + 3;
            }

            for (int i = startingI; i <= endingI; i++) {
                if (type.Contains("end")) {
                    aliasFormat = alias + aliasFormats[i];
                } else aliasFormat = aliasFormats[i] + alias;

                if (HasOto(aliasFormat, tone)) {
                    alias = aliasFormat;
                    return alias;
                }
            }

            return "no alias found";
        }



    }
}

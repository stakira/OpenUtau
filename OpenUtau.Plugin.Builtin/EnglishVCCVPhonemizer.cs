using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.G2p;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("English VCCV Phonemizer", "EN VCCV", "Mim", language: "EN")]
    // This is a temporary solution until Cz's comes out with their own.
    // Feel free to use the Lyric Parser plugin for more accurate pronunciations & support of ConVel.

    // Thanks to cubialpha, Cz, Halo/BagelHero and nago for their help.
    public class EnglishVCCVPhonemizer : SyllableBasedPhonemizer {

        private readonly string[] vowels = "a,@,u,0,8,I,e,3,A,i,E,O,Q,6,o,1ng,9,&,x,1".Split(",");
        private readonly string[] consonants = "b,ch,d,dh,f,g,h,j,k,l,m,n,ng,p,r,s,sh,t,th,v,w,y,z,zh,dd,hh,sp,st".Split(",");
        private readonly Dictionary<string, string> dictionaryReplacements = ("aa=a;ae=@;ah=u;ao=9;aw=8;ay=I;" +
            "b=b;ch=ch;d=d;dh=dh;eh=e;er=3;ey=A;f=f;g=g;hh=h;ih=i;iy=E;jh=j;k=k;l=l;m=m;n=n;ng=ng;ow=O;oy=Q;" +
            "p=p;r=r;s=s;sh=sh;t=t;th=th;uh=6;uw=o;v=v;w=w;y=y;z=z;zh=zh;dx=dd;").Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);

        private readonly Dictionary<string, string> vcExceptions =
            new Dictionary<string, string>() {
                {"i ng","1ng"},
                {"ing","1ng"},
                {"0 r","0r"},
                {"9 r","0r"},
                {"9r","0r"},
                {"e r","Ar"},
                {"er","Ar"},
                {"0 l","0l"},
                {"0l","0l"},
                {"9 l","9l"},
                {"@ m","&m"},
                {"@m","&m"},
                {"& m","&m"},
                {"@ n","& n"},
                {"@n","&n"},
                {"@ ng","Ang"},
                {"@ng","Ang"},
                {"& n","&n"},
                {"8 n","8n"},
                {"0 n","9n"},
                {"0n","9n"},
                {"0 s","9s"},
                {"0s","9s"},
                {"O l","0l"},
                {"Ol","0l"},
                {"6 l","6l"},
                {"i r","Er"},
                {"ir","Er"},
            };

        private readonly Dictionary<string, string> vvExceptions =
            new Dictionary<string, string>() {
                {"o","w"},
                {"0","w"},
                {"O","w"},
                {"8","w"},
                {"A","y"},
                {"I","y"},
                {"E","y"},
                {"Q","y"},
                {"i","y"},
                {"3","r"},
            };

        private readonly string[] ccExceptions = { "th", "ch", "dh", "zh", "sh", "ng" };
        private readonly string[] stopCs = { "b","d","g","k","p","t" };


        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => consonants;
        protected override string GetDictionaryName() => "cmudict-0_7b.txt";
        protected override IG2p LoadBaseDictionary() {
            var g2ps = new List<IG2p>();

            // Load dictionary from plugin folder.
            string path = Path.Combine(PluginDir, "envccv.yaml");
            if (!File.Exists(path)) {
                Directory.CreateDirectory(PluginDir);
                File.WriteAllBytes(path, Data.Resources.envccv_template);
            }
            g2ps.Add(G2pDictionary.NewBuilder().Load(File.ReadAllText(path)).Build());

            // Load dictionary from singer folder.
            if (singer != null && singer.Found && singer.Loaded) {
                string file = Path.Combine(singer.Location, "envccv.yaml");
                if (File.Exists(file)) {
                    try {
                        g2ps.Add(G2pDictionary.NewBuilder().Load(File.ReadAllText(file)).Build());
                    } catch (Exception e) {
                        Log.Error(e, $"Failed to load {file}");
                    }
                }
            }
            g2ps.Add(new ArpabetG2p());
            return new G2pFallbacks(g2ps.ToArray());
        }
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => dictionaryReplacements;


        protected override List<string> ProcessSyllable(Syllable syllable) {
            string prevV = syllable.prevV;
            string[] cc = syllable.cc;
            string v = syllable.v;
            var lastC = cc.Length - 1;
            var lastCPrevWord = syllable.prevWordConsonantsCount;

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

                        if (vvExceptions.ContainsKey(prevV)) {
                            var vc = $"{prevV} {vvExceptions[prevV]}";
                            if (!HasOto(vc, syllable.vowelTone)) {
                                vc = $"{prevV}{vvExceptions[prevV]}";
                            }
                            phonemes.Add(vc);
                            basePhoneme = $"{vvExceptions[prevV]}{v}";
                        }
                        if (!HasOto(basePhoneme, syllable.vowelTone)) {
                            basePhoneme = $"{v}";
                        }
                    }
                } else {
                    // the previous alias will be extended
                    basePhoneme = null;
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

                cc = ValidateCC(cc);

                if (syllable.IsVCVWithOneConsonant) {
                    basePhoneme = $"{cc.Last()}{v}";
                    if (!HasOto(basePhoneme, syllable.vowelTone)) {
                        if($"{cc.Last()}" == "h") {
                            basePhoneme = $"hh{v}";
                        }
                        else basePhoneme = $"_{v}";
                    }

                    var vc = $"{prevV} {cc.Last()}";

                    vc = CheckVCExceptions(vc);

                    phonemes.Add(vc);

                } else if (syllable.IsVCVWithMoreThanOneConsonant) {
                    basePhoneme = $"_{cc.Last()}{v}";
                    if (!HasOto(basePhoneme, syllable.tone) || cc.Length == lastCPrevWord + 1) {
                        basePhoneme = $"{cc.Last()}{v}";
                    }
                    if (cc.Length == lastCPrevWord && stopCs.Contains($"{cc.Last()}"))
                        {
                        basePhoneme = $"-{v}";
                    }

                    var startingC = 0;

                    var vc = $"{prevV}{cc[startingC]}-";

                    if (!HasOto(vc, syllable.vowelTone)) {
                        vc = $"{prevV}{cc[startingC]}";
                        if (!HasOto(vc, syllable.vowelTone)) {
                            vc = $"{prevV} {cc[startingC]}";
                        }
                    }

                    if (lastCPrevWord == 1) {
                        vc = $"{prevV}{cc[startingC]}";
                        startingC = 1;
                    }


                    vc = CheckVCExceptions(vc);

                    if (cc.Length > 1) {
                        var vcc = vc + $" {cc[1]}";
                        if (HasOto(vcc, syllable.tone)) {
                            vc = vcc;
                            startingC = 1;
                        }
                    }


                    if (lastCPrevWord == 0 && prevV == "3") {

                        vc = $"{prevV} {cc[0]}";
//                        vc = $"{prevV}-";
                    }

                    phonemes.Add(vc);
                    var cca = "null";

                    for (int i = startingC; i < cc.Length - 1; i++) {

                        cca = $"{cc[i]}{cc[i + 1]}";

                        if (HasOto(cca, syllable.tone)) {
                            phonemes.Add(cca);

                            //if end of word, skip next CC
                            if (lastCPrevWord == i + 2) {
                                i++;
                                continue;
                            }

                            continue;
                        } else {
                            cca = $"{cc[i]} {cc[i + 1]}";
                            if (HasOto(cca, syllable.tone)) {
                                phonemes.Add(cca);
                                continue;
                            }
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
            var lastC = cc.Length - 1;

            var phonemes = new List<string>();
            // --------------------------- ENDING V ------------------------------- //
            if (ending.IsEndingV) {
                // try V- else no ending
                TryAddPhoneme(phonemes, ending.tone, $"{v}-");

            } else {
                var vc = $"{v}{cc[0]}";
                // --------------------------- ENDING VC ------------------------------- //
                if (ending.IsEndingVCWithOneConsonant) {

                    vc = CheckVCExceptions(vc);
                    vc += "-";
                    phonemes.Add(vc);

                } else {
                    vc = $"{v} {cc[0]}";
                    vc = CheckVCExceptions(vc);
                    // "1nks" exception, start CC loop later 
                    var startingC = 0;
                    var vcc = $"{v} {cc[0]}{cc[1]}";
                    bool hasEnding = false;
                    if (vcc == "i ngk") {
                        vc = "1nk";
                        startingC = 1;
                        if (cc.Length == 2) {
                            vc = "1nk-";
                            hasEnding = true;
                        }
                    }
                    if (cc.Length > 2) {
                        vcc = $"{v} {cc[0]}{cc[1]}{cc[2]}";
                        if (vcc == "i ngks") {
                            vc = "1nks";
                            startingC = 2;
                            if (cc.Length == 3) {
                                vc = "1nks-";
                                hasEnding = true;
                            }
                        }
                    }

                    // V CCs handling

                    var v_cc = $"{v} {cc[0]}{cc[1]}";
                    if (HasOto(v_cc, ending.tone)) {
                        vc = v_cc;
                        startingC = 1;
                    }
                    if (cc.Length > 2) {
                        v_cc = $"{v} {cc[0]}{cc[1]}{cc[2]}";
                        if (HasOto(v_cc, ending.tone)) {
                            vc = v_cc;
                            startingC = 2;
                        }
                    }
                    phonemes.Add(vc);

                    // --------------------------- ENDING VCC ------------------------------- //


                    for (var i = startingC; i < lastC - 1; i++) {
                        var currentCc = $"{cc[i]} {cc[i + 1]}";
                        if (!HasOto(currentCc, ending.tone)) {
                            currentCc = $"{cc[i]}{cc[i + 1]}";
                        }
                        if (HasOto(currentCc, ending.tone)) {
                            phonemes.Add(currentCc);
                        }

                    }

                    if (!hasEnding) {
                        TryAddPhoneme(phonemes, ending.tone, $"{cc[lastC - 1]}{cc[lastC]}-");
                    }

                }
            }

            // ---------------------------------------------------------------------------------- //

            return phonemes;
        }


        private string CheckVCExceptions(string vc) {
            if (vcExceptions.ContainsKey(vc)) {
                vc = vcExceptions[vc];
            }
            return vc;
        }
        private bool CheckCCExceptions(string cc) {
            for (int i = 0; i < ccExceptions.Length; i++) {
                if (cc.Contains(ccExceptions[i])) {
                    return true;
                }
            }

            return false;
        }

        protected override string ValidateAlias(string alias) {
            foreach (var consonant in new[] { "h" }) {
                alias = alias.Replace(consonant, "hh");
            }
            foreach (var consonant in new[] { "6r" }) {
                alias = alias.Replace(consonant, "3");
            }

            return alias;
        }

        private string[] ValidateCC(string[] cc) {
            List<string> ccList = cc.ToList();
            List<string> newCc = new List<string>();

            string buffer = "";
            string bufferCCC = "";

            if (ccList.Count == 1) {
                return cc;
            }
            //Check for CC & CCC
            for (var i = 0; i < ccList.Count; i++) {
                string letter = $"{ccList[i]}";

                if (bufferCCC == "sp") {
                    if (letter == "l" || letter == "r") {
                        newCc.Remove("sp");
                        newCc.Add("s");
                        newCc.Add("p" + letter);
                        newCc.Add(letter);
                        bufferCCC = "";
                        continue;
                    }
                }
                if (bufferCCC == "st") {
                    if (letter == "r") {
                        newCc.Remove("st");
                        newCc.Add("s");
                        newCc.Add("tr");
                        newCc.Add("r");
                        bufferCCC = "";
                        continue;
                    }
                }
                if (buffer == "s") {
                    if (letter == "p" || letter == "t" || letter == "m" || letter == "k") {
                        buffer += letter;
                        bufferCCC = "s" + letter;
                        newCc.Add(buffer);
                        newCc.Add(letter);
                        buffer = "";
                        continue;
                    } else {
                        newCc.Add("s");
                        newCc.Add(letter);
                        buffer = "";
                        continue;
                    }
                }
                if (buffer == "hh") {
                    if (letter == "y") {
                        newCc.Add(buffer);
                        buffer = "hhy";
                        newCc.Add(buffer);
                        buffer = "";
                        continue;
                    } else {
                        newCc.Add(buffer);
                        newCc.Add(letter);
                        buffer = "";
                        continue;
                    }
                }
                if (letter == "s" && i != ccList.Count - 1) {
                    buffer = letter;
                    continue;
                }

                if (letter == "h" && i != ccList.Count - 1) {
                    buffer = "hh";
                    continue;
                }
                if (letter == "hh" && i != ccList.Count - 1) {
                    buffer = letter;
                    continue;
                }
                newCc.Add(letter);
            }

            string[] ccModified = newCc.ToArray();

            return ccModified;
        }


    }
}

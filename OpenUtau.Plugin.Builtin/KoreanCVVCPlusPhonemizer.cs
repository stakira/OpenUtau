using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Korean CVVC+ Phonemizer", "KO CVVC+", "Lotte V", language: "KO")]

    /// <summary>
    /// Custom Korean CVVC phonemizer.
    /// Based on NANA's Korean CVC phonemizer, with some adjustments.
    /// I've based this off of my own method and reclist, which are loosely based on Syeon's Korean CVVC reclist, albeit more extensive (e.g. more precise VC/CC notes).
    /// I know there are already plenty of those. Sorry about that.
    /// </summary>

    public class KoreanCVVCPlusPhonemizer : Phonemizer {

        static readonly string[] naPlainVowels = new string[] { "a", "e", "a", "e", "eo", "e", "eo", "e", "o", "a", "e", "e", "o", "u", "eo", "e", "i", "u", "eu", "i", "i" };

        static readonly string[] naConsonants = new string[] {
            "ㄱ:g","ㄲ:gg","ㄴ:n","ㄷ:d","ㄸ:dd","ㄹ:r","ㅁ:m","ㅂ:b","ㅃ:bb","ㅅ:s","ㅆ:ss","ㅇ:","ㅈ:j","ㅉ:jj","ㅊ:ch","ㅋ:k","ㅌ:t","ㅍ:p","ㅎ:h"
        };
        static readonly string[] naVowels = new string[] {
            "ㅏ:a","ㅐ:e","ㅑ:ya","ㅒ:ye","ㅓ:eo","ㅔ:e","ㅕ:yeo","ㅖ:ye","ㅗ:o","ㅘ:wa","ㅙ:we","ㅚ:we","ㅛ:yo","ㅜ:u","ㅝ:weo","ㅞ:we","ㅟ:wi","ㅠ:yu","ㅡ:eu","ㅢ:eui","ㅣ:i"
        };
        static readonly string[] naFinals = new string[] {
            ":","ㄱ:K","ㄲ:K","ㄳ:K","ㄴ:N","ㄵ:N","ㄶ:N","ㄷ:T","ㄹ:L","ㄺ:K","ㄻ:M","ㄼ:L","ㄽ:L","ㄾ:L","ㄿ:P","ㅀ:L","ㅁ:M","ㅂ:P","ㅄ:P","ㅅ:T","ㅆ:T","ㅇ:NG","ㅈ:T","ㅊ:T","ㅋ:K","ㅌ:T","ㅍ:P","ㅎ:T"
        };
        private const int hangeulStartIndex = 0xAC00;
        private const int hangeulEndIndex = 0xD7A3;

        // ======================================================================================


        static readonly string[] plainVowels = new string[] { "eu", "eo", "a", "i", "u", "e", "o", "er", "N", "L", "M", "NG", "・" };

        static readonly string[] plainDiphthongs = new string[] { "eui" };

        static readonly string[] plainConsonants = new string[] { "K", "T", "P" };

        static readonly string[] vowelEndings = new string[] { "-", "R", "H", "B" };

        static readonly string[] vowels = new string[] {
            "eu=geu,neu,deu,reu,leu,meu,beu,seu,eu,jeu,cheu,keu,teu,peu,heu,ggeu,ddeu,bbeu,sseu,jjeu,feu,veu,zeu,theu,rreu",
            "eo=geo,neo,deo,reo,leo,meo,beo,seo,eo,jeo,cheo,keo,teo,peo,heo,ggeo,ddeo,bbeo,sseo,jjeo,feo,veo,zeo,theo,rreo,gyeo,nyeo,dyeo,ryeo,lyeo,myeo,byeo,syeo,yeo,jyeo,chyeo,kyeo,tyeo,pyeo,hyeo,ggyeo,ddyeo,bbyeo,ssyeo,jjyeo,fyeo,vyeo,zyeo,thyeo,gweo,nweo,dweo,rweo,lweo,mweo,bweo,sweo,weo,jweo,chweo,kweo,tweo,pweo,hweo,ggweo,ddweo,bbweo,ssweo,jjweo,fweo,vweo,zweo,thweo",
            "a=ga,na,da,ra,la,ma,ba,sa,a,ja,cha,ka,ta,pa,ha,gga,dda,bba,ssa,jja,fa,va,za,tha,rra,gya,nya,dya,rya,lya,mya,bya,sya,ya,jya,chya,kya,tya,pya,hya,ggya,ddya,bbya,ssya,jjya,fya,vya,zya,thya,gwa,nwa,dwa,rwa,lwa,mwa,bwa,swa,wa,jwa,chwa,kwa,twa,pwa,hwa,ggwa,ddwa,bbwa,sswa,jjwa,fwa,vwa,zwa,thwa",
            "e=ge,ne,de,re,le,me,be,se,e,je,che,ke,te,pe,he,gge,dde,bbe,sse,jje,fe,ve,ze,the,rre,gye,nye,dye,rye,lye,mye,bye,sye,ye,jye,chye,kye,tye,pye,hye,ggye,ddye,bbye,ssye,jjye,fye,vye,zye,thye,gwe,nwe,dwe,rwe,lwe,mwe,bwe,swe,we,jwe,chwe,kwe,twe,pwe,hwe,ggwe,ddwe,bbwe,sswe,jjwe,fwe,vwe,zwe,thwe",
            "i=gi,ni,di,ri,li,mi,bi,si,i,ji,chi,ki,ti,pi,hi,ggi,ddi,bbi,ssi,jji,fi,vi,zi,thi,rri,gwi,nwi,dwi,rwi,lwi,mwi,bwi,swi,wi,jwi,chwi,kwi,twi,pwi,hwi,ggwi,ddwi,bbwi,sswi,jjwi,fwi,vwi,zwi,thwi",
            "o=go,no,do,ro,lo,mo,bo,so,o,jo,cho,ko,to,po,ho,ggo,ddo,bbo,sso,jjo,fo,vo,zo,tho,rro,gyo,nyo,dyo,ryo,lyo,myo,byo,syo,yo,jyo,chyo,kyo,tyo,pyo,hyo,ggyo,ddyo,bbyo,ssyo,jjyo,fyo,vyo,zyo,thyo",
            "u=gu,nu,du,ru,lu,mu,bu,su,u,ju,chu,ku,tu,pu,hu,ggu,ddu,bbu,ssu,jju,fu,vu,zu,thu,rru,gyu,nyu,dyu,ryu,lyu,myu,byu,syu,yu,jyu,chyu,kyu,tyu,pyu,hyu,ggyu,ddyu,bbyu,ssyu,jjyu,fyu,vyu,zyu,thyu",
            "NG=NG",
            "N=N",
            "M=M",
            "L=L",
            "P=P",
            "T=T",
            "K=K",
            "er=er",
        };

        static readonly string[] consonants = new string[] {
            "ggy=ggi,ggya,ggyu,ggye,ggyo,ggyeo",
            "ggw=ggo,ggu,ggwa,ggwi,ggwe,ggweo",
            "gg=gg,gga,gge,ggeu,ggeo",
            "ddy=ddi,ddya,ddyu,ddye,ddyo,ddyeo",
            "ddw=ddo,ddu,ddwa,ddwi,ddwe,ddweo",
            "dd=dd,dda,dde,ddeu,ddeo",
            "bby=bbi,bbya,bbyu,bbye,bbyo,bbyeo",
            "bbw=bbo,bbu,bbwa,bbwi,bbwe,bbweo",
            "bb=bb,bba,bbe,bbeu,bbeo",
            "ssy=ssy,ssi,ssya,ssyu,ssye,ssyo,ssyeo",
            "ssw=sso,ssu,sswa,sswi,sswe,ssweo",
            "ss=ss,ssa,sse,sseu,sseo",

            "f=f,fa,fi,fu,fe,fo,feu,feo,fya,fyu,fye,fyo,fyeo,fwa,fwi,fwe,fweo",
            "v=v,va,vi,vu,ve,vo,veu,veo,vya,vyu,vye,vyo,vyeo,vwa,vwi,vwe,vweo",
            "z=z,za,zi,zu,ze,zo,zeu,zeo,zya,zyu,zye,zyo,zyeo,zwa,zwi,zwe,zweo",
            "th=th,tha,thi,thu,the,tho,theu,theo,thya,thyu,thye,thyo,thyeo,thwa,thwi,thwe,thweo",
            "rr=rr,rra,rri,rru,rre,rro,rreu,rreo",

            "gy=gya,gyu,gye,gyo,gyeo",
            "gw=go,gu,gwa,gwi,gwe,gweo",
            "g=g,ga,ge,geu,geo",
            "ny=ni,nya,nyu,nye,nyo,nyeo",
            "nw=no,nu,nwa,nwi,nwe,nweo",
            "n=n,na,ne,neu,neo",
            "dy=di,dya,dyu,dye,dyo,dyeo",
            "dw=do,du,dwa,dwi,dwe,dweo",
            "d=d,da,de,deu,deo",
            "ry=ri,rya,ryu,rye,ryo,ryeo",
            "rw=ro,ru,rwa,rwi,rwe,rweo",
            "r=r,ra,re,reu,reo",
            "my=mi,mya,myu,mye,myo,myeo",
            "mw=mo,mu,mwa,mwi,mwe,mweo",
            "m=m,ma,me,meu,meo",
            "by=bi,bya,byu,bye,byo,byeo",
            "bw=bo,bu,bwa,bwi,bwe,bweo",
            "b=b,ba,be,beu,beo",
            "sy=si,sya,syu,sye,syo,syeo",
            "sw=so,su,swa,swi,swe,sweo",
            "s=s,sa,se,seu,seo",
            "jy=ji,jya,jyu,jye,jyo,jyeo",
            "jw=jo,ju,jwa,jwi,jwe,jweo",
            "j=j,ja,je,jeu,jeo",
            "chy=chi,chya,chyu,chye,chyo,chyeo",
            "chw=cho,chu,chwa,chwi,chwe,chweo",
            "ch=ch,cha,che,cheu,cheo",
            "ky=ki,kya,kyu,kye,kyo,kyeo",
            "kw=ko,ku,kwa,kwi,kwe,kweo",
            "k=k,ka,ke,keu,keo",
            "ty=ti,tya,tyu,tye,tyo,tyeo",
            "tw=to,tu,twa,twi,twe,tweo",
            "t=t,ta,te,teu,teo",
            "py=pi,pya,pyu,pye,pyo,pyeo",
            "pw=po,pu,pwa,pwi,pwe,pweo",
            "p=p,pa,pe,peu,peo",
            "hy=hi,hya,hyu,hye,hyo,hyeo",
            "hw=ho,hu,hwa,hwi,hwe,hweo",
            "h=h,ha,he,heu,heo",
            "jjy=jji,jjya,jjyu,jjye,jjyo,jjyeo",
            "jjw=jjo,jju,jjwa,jjwi,jjwe,jjweo",
            "jj=jj,jja,jje,jjeu,jjeo",
            "ly=li,lya,lyu,lye,lyo,lyeo",
            "lw=lo,lu,lwa,lwi,lwe,lweo",
            "l=l,la,le,leu,leo"
            };


        static readonly Dictionary<string, string> vowelLookup;
        static readonly Dictionary<string, string> consonantLookup;

        string getConsonant(string str) {
            str = str.Replace('a', ' ');
            str = str.Replace('i', ' ');
            str = str.Replace('u', ' ');
            str = str.Replace('e', ' ');
            str = str.Replace('o', ' ');
            str = str.Replace('w', ' ');
            str = str.Replace('y', ' ');
            str = str.Trim();

            return str;
        }

        bool isAlphaCon(string consStr) {
            String str = consStr.Replace('w', ' ');
            str = consStr.Replace('y', ' ');
            str = str.Trim();

            if (str == "gg") { return true; } 
            else if (str == "dd") { return true; }
            else if (str == "bb") { return true; }
            else if (str == "ss") { return true; } 
            else if (str == "f") { return true; } 
            else if (str == "v") { return true; } 
            else if (str == "z") { return true; } 
            else if (str == "th") { return true; } 
            else if (str == "rr") { return true; } 
            else if (str == "g") { return true; } 
            else if (str == "n") { return true; } 
            else if (str == "d") { return true; } 
            else if (str == "r") { return true; } 
            else if (str == "m") { return true; } 
            else if (str == "b") { return true; } 
            else if (str == "s") { return true; } 
            else if (str == "j") { return true; } 
            else if (str == "ch") { return true; } 
            else if (str == "k") { return true; } 
            else if (str == "t") { return true; } 
            else if (str == "p") { return true; } 
            else if (str == "h") { return true; } 
            else if (str == "jj") { return true; }
            else if (str == "l") { return true; } 
            else { return false; }
        }

        static KoreanCVVCPlusPhonemizer() {
            vowelLookup = vowels.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
            consonantLookup = consonants.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
        }


        // ======================================================================================


        private USinger singer;
        public override void SetSinger(USinger singer) => this.singer = singer;

        // make it quicker to check multiple oto occurrences at once rather than spamming if else if
        private bool checkOtoUntilHit(string[] input, Note note, out UOto oto) {
            oto = default;

            var attr0 = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
            var attr1 = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 1) ?? default;

            foreach (string test in input) {
                if (singer.TryGetMappedOto(test, note.tone + attr0.toneShift, attr0.voiceColor, out oto)) {
                    return true;
                }
            }

            return false;
        }

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            var note = notes[0];
            var currentUnicode = ToUnicodeElements(note.lyric); // Unicode of current lyrics
            string currentLyric = note.lyric; // current lyrics
            var attr0 = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
            var attr1 = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 1) ?? default;

            // Separation of initial, middle, and final consonants in lyrics
            // P(re)Lconsonant, PLvowel, PLfinal / C(urrent)Lconsonant, CLvowel, CLfinal / N(ext)Lconsonant, NLvowel, NLfinal

            int CLconsonant = 0;
            int CLvowel = 0;
            int CLfinal = 0; // The consonant, vowel, and final index of the current note
            string[] TCLtemp;
            string TCLconsonant = "";
            string TCLvowel = "";
            string TCLfinal = "";
            string TCLplainvowel = ""; // Simplification of consonant, vowel, final, and vowel in the current note
            string TCLplainfinal = "";

            int TCLsemivowel = 0; // semi vowel is 'y', 'w'. [0 means "there's no semi vowel], [1 means "there is 'y'"], [2 means "there is 'w'"]]

            // ↓ use these for generating phonemes in phonemizers 
            string TCLconsonantCVVC = "";

            int NLconsonant = 0;
            int NLvowel = 0;
            int NLfinal = 0;
            string[] TNLtemp;
            string TNLconsonant = "";
            string TNLvowel = "";
            string TNLfinal = "";
            string TNLplainvowel = "";

            // ↓ use these for generating phonemes in phonemizers 
            string TNLconsonantCVVC = "";

            int TNLsemivowel = 0; // semi vowel is 'y', 'w'. [0 means "there's no semi vowel], [1 means "there is 'y'"], [2 means "there is 'w'"]]

            int PLconsonant = 0;
            int PLvowel = 0;
            int PLfinal = 0;
            string[] TPLtemp;
            string TPLconsonant = "";
            string TPLvowel = "";
            string TPLfinal = "";
            string TPLplainvowel = "";
            string TPLplainfinal = "";

            bool currentHangeul = false;
            bool prevHangeul = false;
            bool nextHangeul = false;

            bool prevExist = false;
            bool nextExist = false;

            char firstCL, firstPL, firstNL;
            int lCL, lPL, lNL;
            int uCL, uPL, uNL;
            lCL = 0; lPL = 0; lNL = 0;
            bool prevIsBreath = false;


            // Check the first letter of the current note
            firstCL = currentLyric[0];
            if (firstCL == 'ㄹ') { lCL = 1; firstCL = currentLyric[1]; }
            uCL = (int) firstCL;
            if ((uCL >= hangeulStartIndex) && (uCL <= hangeulEndIndex)) {
                currentHangeul = true;
                CLconsonant = (uCL - hangeulStartIndex) / (21 * 28);
                CLvowel = (uCL - hangeulStartIndex) % (21 * 28) / 28;
                CLfinal = (uCL - hangeulStartIndex) % 28;


                TCLtemp = naConsonants[CLconsonant].Split(':');
                TCLconsonant = TCLtemp[1];

                TCLtemp = naVowels[CLvowel].Split(':');
                TCLvowel = TCLtemp[1];
                TCLplainvowel = naPlainVowels[CLvowel];

                if (TCLvowel.StartsWith("y") || (TCLvowel == "i")) { TCLsemivowel = 1; } else if (TCLvowel.StartsWith("w") || (TCLvowel == "o") || (TCLvowel == "u")) { TCLsemivowel = 2; } else {
                    TCLsemivowel = 0;
                }

                TCLtemp = naFinals[CLfinal].Split(':');
                TCLfinal = TCLtemp[1];

                TCLplainfinal = TCLfinal;

                // TCLconsonant: String note initial tone    TCLvowel: String note neutral    TCLfinal: String note final tone

            }

            // Check if the previous note exists + Check the first letter of the previous note
            if (prevNeighbour != null) {
                firstPL = (prevNeighbour?.lyric)[0]; // get lyrics
                prevExist = true; // if previous note exists
                if (firstPL == 'ㄹ') { lPL = 1; firstPL = (prevNeighbour?.lyric)[1]; } // if note contains ㄹㄹ ("l")
                uPL = (int) firstPL; // convert lyrics to int

                if ((uPL >= hangeulStartIndex) && (uPL <= hangeulEndIndex)) {
                    prevHangeul = true;

                    PLconsonant = (uPL - hangeulStartIndex) / (21 * 28);
                    PLvowel = (uPL - hangeulStartIndex) % (21 * 28) / 28;
                    PLfinal = (uPL - hangeulStartIndex) % 28;


                    TPLtemp = naConsonants[PLconsonant].Split(':');
                    TPLconsonant = TPLtemp[1];

                    TPLtemp = naVowels[PLvowel].Split(':');
                    TPLvowel = TPLtemp[1];
                    TPLplainvowel = naPlainVowels[PLvowel];

                    TPLtemp = naFinals[PLfinal].Split(':');
                    TPLfinal = TPLtemp[1];
                    TPLplainfinal = TPLfinal;
                }
            }

            // Check if the next note exists + Check the first letter of the next note
            if (nextNeighbour != null) {
                firstNL = (nextNeighbour?.lyric)[0];
                nextExist = true;
                if (firstNL == 'ㄹ') { lNL = 1; firstNL = (nextNeighbour?.lyric)[1]; }
                uNL = (int) firstNL;

                if ((uNL >= hangeulStartIndex) && (uNL <= hangeulEndIndex)) {
                    nextHangeul = true;

                    NLconsonant = (uNL - hangeulStartIndex) / (21 * 28);
                    NLvowel = (uNL - hangeulStartIndex) % (21 * 28) / 28;
                    NLfinal = (uNL - hangeulStartIndex) % 28;


                    TNLtemp = naConsonants[NLconsonant].Split(':');
                    TNLconsonant = TNLtemp[1];

                    TNLtemp = naVowels[NLvowel].Split(':');
                    TNLvowel = TNLtemp[1];
                    TNLplainvowel = naPlainVowels[NLvowel];

                    if (TNLvowel.StartsWith("y") || TNLvowel == "i") { TNLsemivowel = 1; } else if (TNLvowel.StartsWith("w") || TNLvowel == "o" || TNLvowel == "u") { TNLsemivowel = 2; }

                    TNLtemp = naFinals[NLfinal].Split(':');
                    TNLfinal = TNLtemp[1];
                }
            }

            if (currentHangeul) {
                // Application of phonetic rules
                if (currentHangeul) {

                    // 1. linking rule
                    string tempTCLconsonant = "";
                    string tempTCLfinal = "";
                    bool yeoneum = false;
                    bool yeoneum2 = false;

                    if (prevExist && prevHangeul && (CLconsonant == 11) && (TPLfinal != "")) {
                        int temp = PLfinal;
                        if (temp == 1) { TCLtemp = naConsonants[0].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 2) { TCLtemp = naConsonants[1].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 3) { TCLtemp = naConsonants[10].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 4) { TCLtemp = naConsonants[2].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 5) { TCLtemp = naConsonants[12].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 6) { TCLtemp = naConsonants[18].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 7) { TCLtemp = naConsonants[3].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 8) { TCLtemp = naConsonants[5].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 9) { TCLtemp = naConsonants[0].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 10) { TCLtemp = naConsonants[6].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 11) { TCLtemp = naConsonants[7].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 12) { TCLtemp = naConsonants[9].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 13) { TCLtemp = naConsonants[16].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 14) { TCLtemp = naConsonants[17].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 15) { TCLtemp = naConsonants[18].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 16) { TCLtemp = naConsonants[6].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 17) { TCLtemp = naConsonants[7].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 18) { TCLtemp = naConsonants[9].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 19) { TCLtemp = naConsonants[9].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 20) { TCLtemp = naConsonants[10].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 21) { tempTCLconsonant = ""; yeoneum = true; } else if (temp == 22) { TCLtemp = naConsonants[12].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 23) { TCLtemp = naConsonants[14].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 24) { TCLtemp = naConsonants[15].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 25) { TCLtemp = naConsonants[16].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 26) { TCLtemp = naConsonants[17].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 27) { TCLtemp = naConsonants[18].Split(':'); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                    }

                    if (nextExist && nextHangeul && (TCLfinal != "") && (TNLconsonant == "")) {
                        int temp = CLfinal;

                        if (temp == 1) { TCLtemp = naConsonants[0].Split(':'); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; TCLplainfinal = ""; yeoneum2 = true; } else if (temp == 2) { TCLtemp = naConsonants[1].Split(':'); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; TCLplainfinal = ""; yeoneum2 = true; } else if (temp == 3) { TCLfinal = "K"; TCLplainfinal = ""; yeoneum2 = true; } else if (temp == 4) { TCLtemp = naConsonants[2].Split(':'); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; TCLplainfinal = ""; yeoneum2 = true; } else if (temp == 5) { TCLfinal = "N"; TCLplainfinal = "N"; yeoneum2 = true; } else if (temp == 6) { TCLfinal = "N"; TCLplainfinal = "N"; yeoneum2 = true; } else if (temp == 7) { TCLtemp = naConsonants[3].Split(':'); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; TCLplainfinal = ""; yeoneum2 = true; } else if (temp == 8) { TCLtemp = naConsonants[5].Split(':'); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; TCLplainfinal = ""; yeoneum2 = true; } else if (temp == 9) { TCLfinal = "L"; TCLplainfinal = "L"; yeoneum2 = true; } else if (temp == 10) { TCLfinal = "L"; TCLplainfinal = "L"; yeoneum2 = true; } else if (temp == 11) { TCLfinal = "L"; TCLplainfinal = "L"; yeoneum2 = true; } else if (temp == 12) { TCLfinal = "L"; TCLplainfinal = "L"; yeoneum2 = true; } else if (temp == 13) { TCLfinal = "L"; TCLplainfinal = "L"; yeoneum2 = true; } else if (temp == 14) { TCLfinal = "L"; TCLplainfinal = "L"; yeoneum2 = true; } else if (temp == 15) { TCLfinal = "L"; TCLplainfinal = "L"; yeoneum2 = true; } else if (temp == 16) { TCLtemp = naConsonants[6].Split(':'); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; TCLplainfinal = ""; yeoneum2 = true; } else if (temp == 17) { TCLtemp = naConsonants[7].Split(':'); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; TCLplainfinal = ""; yeoneum2 = true; } else if (temp == 18) { TCLfinal = "P"; TCLplainfinal = ""; yeoneum2 = true; } else if (temp == 19) { TCLtemp = naConsonants[9].Split(':'); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; TCLplainfinal = ""; yeoneum2 = true; } else if (temp == 20) { TCLtemp = naConsonants[10].Split(':'); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; TCLplainfinal = ""; yeoneum2 = true; }
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             //else if (temp == 21) { TCLtemp = naConsonants[11].Split(':'); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; TCLplainfinal = ""; yeoneum2 = true; }
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             else if (temp == 22) { TCLtemp = naConsonants[12].Split(':'); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; TCLplainfinal = ""; yeoneum2 = true; } else if (temp == 23) { TCLtemp = naConsonants[14].Split(':'); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; TCLplainfinal = ""; yeoneum2 = true; } else if (temp == 24) { TCLtemp = naConsonants[15].Split(':'); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; TCLplainfinal = ""; yeoneum2 = true; } else if (temp == 25) { TCLtemp = naConsonants[16].Split(':'); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; TCLplainfinal = ""; yeoneum2 = true; } else if (temp == 26) { TCLtemp = naConsonants[17].Split(':'); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; TCLplainfinal = ""; yeoneum2 = true; } else if (temp == 27) { TCLtemp = naConsonants[18].Split(':'); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; TCLplainfinal = ""; yeoneum2 = true; }

                    }
                    if (yeoneum) { TCLconsonant = tempTCLconsonant; }
                    if (yeoneum2) { TNLconsonant = tempTCLfinal; }


                    // 2. aspirated / organic sound / become harsh (sandhi)
                    if (prevExist && prevHangeul && (TPLfinal != "")) {
                        if (((PLfinal == 27) && (CLconsonant == 0)) || ((PLfinal == 6) && (CLconsonant == 0)) || ((PLfinal == 15) && (CLconsonant == 0))) { TCLconsonant = "k"; } else if (((PLfinal == 27) && (CLconsonant == 3)) || ((PLfinal == 6) && (CLconsonant == 3)) || ((PLfinal == 15) && (CLconsonant == 3))) { TCLconsonant = "t"; } else if (((PLfinal == 27) && (CLconsonant == 12)) || ((PLfinal == 6) && (CLconsonant == 12)) || ((PLfinal == 15) && (CLconsonant == 12))) { TCLconsonant = "ch"; } else if (((PLfinal == 27) && (CLconsonant == 9)) || ((PLfinal == 6) && (CLconsonant == 9)) || ((PLfinal == 15) && (CLconsonant == 9))) { TCLconsonant = "ss"; }

                        if ((PLfinal == 1) && (CLconsonant == 18)) { TCLconsonant = "k"; } else if ((PLfinal == 7) && (CLconsonant == 18)) { TCLconsonant = "t"; } else if ((PLfinal == 17) && (CLconsonant == 18)) { TCLconsonant = "p"; } else if ((PLfinal == 22) && (CLconsonant == 18)) { TCLconsonant = "ch"; }
                    }
                    if (nextExist && nextHangeul && (TCLfinal != "")) {
                        if ((NLconsonant == 0) && (CLfinal == 27)) { TCLfinal = ""; TCLplainfinal = ""; TNLconsonant = "k"; } else if ((NLconsonant == 0) && (CLfinal == 6)) { TCLfinal = "N"; TCLplainfinal = "N"; TNLconsonant = "k"; } else if ((NLconsonant == 0) && (CLfinal == 15)) { TCLfinal = "L"; TCLplainfinal = "L"; TNLconsonant = "K"; } else if ((NLconsonant == 3) && (CLfinal == 27)) { TCLfinal = ""; TCLplainfinal = ""; TNLconsonant = "t"; } else if ((NLconsonant == 3) && (CLfinal == 6)) { TCLfinal = "N"; TCLplainfinal = "N"; TNLconsonant = "t"; } else if ((NLconsonant == 3) && (CLfinal == 15)) { TCLfinal = "L"; TCLplainfinal = "L"; TNLconsonant = "t"; } else if ((NLconsonant == 12) && (CLfinal == 27)) { TCLfinal = ""; TCLplainfinal = ""; TNLconsonant = "ch"; } else if ((NLconsonant == 12) && (CLfinal == 6)) { TCLfinal = "N"; TCLplainfinal = "N"; TNLconsonant = "ch"; } else if ((NLconsonant == 12) && (CLfinal == 15)) { TCLfinal = "L"; TCLplainfinal = "L"; TNLconsonant = "ch"; } else if ((NLconsonant == 9) && (CLfinal == 27)) { TCLfinal = ""; TCLplainfinal = ""; TNLconsonant = "ss"; } else if ((NLconsonant == 9) && (CLfinal == 6)) { TCLfinal = "N"; TCLplainfinal = "N"; TNLconsonant = "ss"; } else if ((NLconsonant == 9) && (CLfinal == 15)) { TCLfinal = "L"; TCLplainfinal = "L"; TNLconsonant = "ss"; }

                        if ((NLconsonant == 2) && (CLfinal == 27)) { TCLfinal = "N"; TCLplainfinal = "N"; }

                        if ((NLconsonant == 18) && (CLfinal == 1)) { TCLfinal = ""; TCLplainfinal = ""; TNLconsonant = "k"; } else if ((NLconsonant == 18) && (CLfinal == 7)) { TCLfinal = ""; TCLplainfinal = ""; TNLconsonant = "t"; } else if ((NLconsonant == 18) && (CLfinal == 17)) { TCLfinal = ""; TCLplainfinal = ""; TNLconsonant = "p"; } else if ((NLconsonant == 18) && (CLfinal == 22)) { TCLfinal = ""; TCLplainfinal = ""; TNLconsonant = "ch"; }
                    }


                    // 3. syllable ending rule exceptions
                    if (nextExist && nextHangeul) {
                        /*
                        // When there is a ㄼ + consonant => ㄼ : p
                        if ((CLfinal == 11) && (TCLconsonant != "")) { TCLfinal = "P"; }
                        */
                        // ㄺ(lg) + ㄱ(g) => ㄺ(lg) : ㄹ(l)
                        if ((CLfinal == 9) && (NLconsonant == 0)) { TCLfinal = "L"; TCLplainfinal = "L"; }
                    }


                    // 4. Become hard/voiced
                    if (prevExist && prevHangeul && TPLfinal != "") {
                        // ㄱ(g)ㄷ(d)ㅂ(b) + ㄱ(g)ㄷ(d)ㅂ(b)ㅅ(s)ㅈ(j) = ㄲ(gg)ㄸ(dd)ㅃ(bb)ㅆ(ss)ㅉ(jj)
                        if (((TPLfinal == "K") && (CLconsonant == 0)) || ((TPLfinal == "T") && (CLconsonant == 0)) || ((TPLfinal == "P") && (CLconsonant == 0))) { TCLconsonant = "gg"; } else if (((TPLfinal == "K") && (CLconsonant == 3)) || ((TPLfinal == "T") && (CLconsonant == 3)) || ((TPLfinal == "P") && (CLconsonant == 3))) { TCLconsonant = "dd"; } else if (((TPLfinal == "K") && (CLconsonant == 7)) || ((TPLfinal == "T") && (CLconsonant == 7)) || ((TPLfinal == "P") && (CLconsonant == 7))) { TCLconsonant = "bb"; } else if (((TPLfinal == "K") && (CLconsonant == 9)) || ((TPLfinal == "T") && (CLconsonant == 9)) || ((TPLfinal == "P") && (CLconsonant == 9))) { TCLconsonant = "ss"; } else if (((TPLfinal == "K") && (CLconsonant == 12)) || ((TPLfinal == "T") && (CLconsonant == 12)) || ((TPLfinal == "P") && (CLconsonant == 12))) { TCLconsonant = "jj"; }

                        /* 
                        // Word stem support ㄴ(n)ㅁ(m) + ㄱ(g)ㄷ(d)ㅅ(s)ㅈ(j) = ㄲ(gg)ㄸ(dd)ㅆ(ss)ㅉ(jj)
                        if(((TPLfinal=="n")&&(CLconsonant==0))|| ((TPLfinal == "M") && (CLconsonant == 0))) { TCLconsonant = "gg"; }
                        else if (((TPLfinal == "N") && (CLconsonant == 3)) || ((TPLfinal == "M") && (CLconsonant == 3))) { TCLconsonant = "dd"; }
                        else if (((TPLfinal == "N") && (CLconsonant == 9)) || ((TPLfinal == "M") && (CLconsonant == 9))) { TCLconsonant = "ss"; }
                        else if (((TPLfinal == "N") && (CLconsonant == 12)) || ((TPLfinal == "M") && (CLconsonant == 12))) { TCLconsonant = "jj"; }
                        */

                        // Articles ending in ㄹ(l) / Hanja ㄹ(l) + ㄷ(d)ㅅ(s)ㅈ(j) = ㄸ(dd)ㅆ(ss)ㅉ(jj)
                        if ((PLfinal == 8) && (CLconsonant == 3)) { TCLconsonant = "dd"; } else if ((PLfinal == 8) && (CLconsonant == 9)) { TCLconsonant = "ss"; } else if ((PLfinal == 8) && (CLconsonant == 12)) { TCLconsonant = "jj"; }

                        // stem support ㄼ(lb)ㄾ(lt) + ㄱ(g)ㄷ(d)ㅅ(s)ㅈ(j) = ㄲ(gg)ㄸ(dd)ㅆ(ss)ㅉ(jj)
                        if (((PLfinal == 11) && (CLconsonant == 0)) || ((PLfinal == 13) && (CLconsonant == 0))) { TCLconsonant = "gg"; } else if (((PLfinal == 11) && (CLconsonant == 3)) || ((PLfinal == 13) && (CLconsonant == 3))) { TCLconsonant = "dd"; } else if (((PLfinal == 11) && (CLconsonant == 9)) || ((PLfinal == 13) && (CLconsonant == 9))) { TCLconsonant = "ss"; } else if (((PLfinal == 11) && (CLconsonant == 12)) || ((PLfinal == 13) && (CLconsonant == 12))) { TCLconsonant = "jj"; }
                    }


                    // 5. palatalization
                    if (prevExist && prevHangeul && (TPLfinal != "")) {
                        if ((PLfinal == 7) && (CLconsonant == 11) && (CLvowel == 20)) { TCLconsonant = "j"; } else if ((PLfinal == 25) && (CLconsonant == 11) && (CLvowel == 20)) { TCLconsonant = "ch"; } else if ((PLfinal == 13) && (CLconsonant == 11) && (CLvowel == 20)) { TCLconsonant = "ch"; } else if ((PLfinal == 7) && (CLconsonant == 18) && (CLvowel == 20)) { TCLconsonant = "ch"; }
                    }
                    if (nextExist && nextHangeul && (TCLfinal != "")) {
                        if ((CLfinal == 7) && (NLconsonant == 11) && (NLvowel == 20)) { TCLfinal = ""; TCLplainfinal = ""; } else if ((CLfinal == 25) && (NLconsonant == 11) && (NLvowel == 20)) { TCLfinal = ""; TCLplainfinal = ""; } else if ((CLfinal == 13) && (NLconsonant == 11) && (NLvowel == 20)) { TCLfinal = ""; TCLplainfinal = ""; } else if ((CLfinal == 7) && (NLconsonant == 18) && (NLvowel == 20)) { TCLfinal = ""; TCLplainfinal = ""; }

                    }


                    // 6. nasalization
                    if (prevExist && prevHangeul && (TPLfinal != "")) {
                        // Hanja batchim (endings) ㅁ(m)ㅇ(ng) + ㄹ(l) = ㄴ(n)
                        if (((TPLfinal == "M") && (CLconsonant == 5)) || ((TPLfinal == "NG") && (CLconsonant == 5))) { TPLplainfinal = TPLfinal; TCLconsonant = "n"; }

                        // Hanja batchim ㄱ(g)ㄷ(d)ㅂ(b) + ㄹ(l) = ㅇ(ng)ㄴ(n)ㅁ(m) + ㄴ(n)(1)
                        if (((TPLfinal == "K") && (CLconsonant == 5)) || ((TPLfinal == "T") && (CLconsonant == 5)) || ((TPLfinal == "P") && (CLconsonant == 5))) { TCLconsonant = "n"; }
                    }
                    if (nextExist && nextHangeul && (TCLfinal != "")) {
                        //Batchim ㄱ(g)ㄷ(d)ㅂ(b) + ㄴ(n)ㅁ(m) = ㅇ(ng)ㄴ(n)ㅁ(m)
                        if (((TCLfinal == "K") && (TNLconsonant == "n")) || ((TCLfinal == "K") && (TNLconsonant == "m"))) { TCLfinal = "NG"; TCLplainfinal = "NG"; } else if (((TCLfinal == "T") && (TNLconsonant == "n")) || ((TCLfinal == "T") && (TNLconsonant == "m"))) { TCLfinal = "N"; TCLplainfinal = "N"; } else if (((TCLfinal == "P") && (TNLconsonant == "n")) || ((TCLfinal == "P") && (TNLconsonant == "m"))) { TCLfinal = "M"; TCLplainfinal = "M"; }

                        if ((TCLfinal == "K") && (TNLconsonant == "s") || (TCLfinal == "P") && (TNLconsonant == "s") || (TCLfinal == "L") && (TNLconsonant == "s")) { TNLconsonant = "ss"; }

                        if ((TCLfinal == "K") && (TNLconsonant == "r") || (TCLfinal == "P") && (TNLconsonant == "r") || (TCLfinal == "T") && (TNLconsonant == "r")) { TNLconsonant = "n"; }

                        if ((TCLfinal == "L") && (TNLconsonant == "n") || (TCLfinal == "L") && (TNLconsonant == "r")) { TNLconsonant = "l"; }

                        if ((TCLfinal == "M") && (TNLconsonant == "r")) { TNLconsonant = "n"; }

                        if ((TCLfinal == "T") && (TNLconsonant == "s")) { TCLfinal = ""; TNLconsonant = "ss"; }

                        if ((TCLfinal == "NG") && (TNLconsonant == "r")) { TNLconsonant = "n"; }


                        // Hanja batchim base ㄱ(g)ㄷ(d)ㅂ(b) + ㄹ(l) = ㅇ(ng)ㄴ(n)ㅁ(m) + ㄴ(n)(2)
                        if ((TCLfinal == "K") && (NLconsonant == 5)) { TCLfinal = "NG"; TCLplainfinal = "NG"; } else if ((TCLfinal == "T") && (NLconsonant == 5)) { TCLfinal = "N"; TCLplainfinal = "N"; } else if ((TCLfinal == "P") && (NLconsonant == 5)) { TCLfinal = "M"; TCLplainfinal = "M"; }
                    }


                    // 7. voicing
                    if (prevExist && prevHangeul && (TPLfinal != "")) {
                        if (((PLfinal == 8) && (TCLconsonant == "n")) || ((PLfinal == 13) && (TCLconsonant == "n")) || ((PLfinal == 15) && (TCLconsonant == "n"))) { TPLplainfinal = "L"; TCLconsonant = "l"; }
                    }
                    if (nextExist && nextHangeul && (TCLfinal != "")) {
                        if (((PLfinal == 8) && (TNLconsonant == "r") || ((PLfinal == 13) && (TCLconsonant == "r")) || ((PLfinal == 15) && (TCLconsonant == "r")))) { TCLconsonant = "l"; }
                    }


                    // 8. Batchim + ㄹ(r) = ㄹㄹ(l)
                    if (prevExist && prevHangeul && (TPLfinal != "")) { lCL = 1; }



                    // When there is a change in the consonant
                    if (prevExist && prevHangeul) {


                        // Nasalization
                        // (1) ㄱ(g)(ㄲ(gg)ㅋ(k)ㄳ(gs)ㄺ(lg))
                        //     ㄷ(d)(ㅅ(s),ㅆ(ss),ㅈ(j),ㅊ(ch),ㅌ(t),ㅎ(h))
                        //     ㅂ(b)(ㅍ(p),ㄼ(lb),ㄿ(lp),ㅄ(bs))


                    }
                    // When the final has changes


                }

                bool isLastBatchim = false;

                // to make FC's length to 1 if FC comes final (=no next note)
                if (!nextHangeul && TCLfinal != "" && TCLvowel != "") {
                    isLastBatchim = true;
                }

                // To use semivowels in VC (ex: [- ga][a gy][gya], ** so not [- ga][a g][gya] **)
                if (TCLsemivowel == 1) {
                    TCLconsonantCVVC = TCLconsonant + 'y';
                } else if (TCLsemivowel == 2) {
                    TCLconsonantCVVC = TCLconsonant + 'w';
                } else {
                    TCLconsonantCVVC = TCLconsonant;
                }

                if (TNLsemivowel == 1) {
                    TNLconsonantCVVC = TNLconsonant + 'y';
                } else if (TNLsemivowel == 2) {
                    TNLconsonantCVVC = TNLconsonant + 'w';
                } else {
                    TNLconsonantCVVC = TNLconsonant;
                }

                string CV = (TCLconsonant + TCLvowel);
                string VC = "";
                bool comesSemivowelWithoutVC = false;

                if (TCLsemivowel != 0 && TCLconsonant == "") {
                    comesSemivowelWithoutVC = true;
                }


                if (prevExist && TCLconsonant == "" && TPLfinal == "") {
                    string[] tests = new string[] { $"{TPLplainvowel} {CV}", CV, currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        CV = oto.Alias;
                    }
                }

                if (nextExist && (TCLfinal == "") && nextHangeul) { VC = TCLplainvowel + " " + TNLconsonantCVVC; }
                //for Vowel VCV
                if (prevExist && prevHangeul && TPLfinal == "" && TCLconsonantCVVC == "" && TCLconsonant == "" && !comesSemivowelWithoutVC) {
                    CV = TPLplainvowel + " " + TCLvowel;

                }

                string FC = "";
                if (TCLfinal != "") { FC = TCLplainvowel + " " + TCLfinal; }

                if (lCL == 1) { CV = CV.Replace("r", "l"); }

                // Batchim connector note
                string CC = "";

                if (nextExist && nextHangeul && TCLfinal != "" && TNLconsonantCVVC != "" && (TNLsemivowel == 1 || TNLsemivowel == 2)) {
                    CC = $"{TCLplainfinal} {TNLconsonantCVVC}";
                } else if (nextExist && nextHangeul && TCLfinal != "" && TNLconsonant != "" && TNLsemivowel == 0) {
                    CC = $"{TCLplainfinal} {TNLconsonant}";
                }

                // If there is no previous note
                if (!prevExist && TCLconsonant == "r") {
                    string[] tests = new string[] { $"- {CV}", $"l{TCLvowel}", CV, currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        CV = oto.Alias;
                    }
                } else if (!prevExist && TCLconsonant != "r") {
                    string[] tests = new string[] { $"- {CV}", CV, currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        CV = oto.Alias;
                    }
                }


                // Connector note with ㅇ(NG)
                if (prevExist && TCLconsonant == "" && TPLfinal == "NG") {
                    string[] tests = new string[] { $"NG {CV}", CV, currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        CV = oto.Alias;
                    }
                }

                if (prevNeighbour != null && prevExist && !prevHangeul && TCLconsonant == "") {
                    var prevUnicode = ToUnicodeElements(prevNeighbour?.lyric);
                    var vowel = "";

                    var prevLyric = string.Join("", prevUnicode);

                    // Current note is VV
                    if (vowelLookup.TryGetValue(prevUnicode.LastOrDefault() ?? string.Empty, out var vow)) {
                        vowel = vow;

                        var mixVV = $"{vow} {CV}";

                        if (prevLyric.EndsWith("eo")) {
                            mixVV = $"eo {CV}";
                        } else if (prevLyric.EndsWith("eu")) {
                            mixVV = $"eu {CV}";
                        } else if (prevLyric.EndsWith("NG")) {
                            mixVV = $"NG {CV}";
                        } else if (prevLyric.EndsWith("er")) {
                            mixVV = $"er {CV}";
                        }

                        // try vowlyric then currentlyric
                        string[] tests = new string[] { mixVV, CV };
                        if (checkOtoUntilHit(tests, note, out var oto)) {
                            CV = oto.Alias;
                        }
                    }

                }

                if (nextNeighbour != null) { // 다음에 노트가 있으면
                    var nextUnicode = ToUnicodeElements(nextNeighbour?.lyric);
                    var nextLyric = string.Join("", nextUnicode);

                    // Insert VC before next neighbor
                    // Get vowel from current note
                    var vowel = TCLplainvowel;

                    // Get consonant from next note
                    var consonant = "";
                    if (consonantLookup.TryGetValue(nextUnicode.FirstOrDefault() ?? string.Empty, out var con)) {
                        consonant = getConsonant(nextNeighbour?.lyric); //Mixed romaja
                        if ((!isAlphaCon(consonant) || con == "f" || con == "v" || con == "z" || con == "th" || con == "rr")) {
                            consonant = con;
                        } else if (nextLyric.StartsWith(con + "y") || (nextLyric.Contains("i") && !nextLyric.Contains("wi") && !nextLyric.Contains("eui"))) {
                            consonant = con + "y";
                        } else if (nextLyric.StartsWith(con + "w") || (nextLyric.Contains("o") && !nextLyric.Contains("eo")) || (nextLyric.Contains("u") && !nextLyric.Contains("eu"))) {
                            consonant = con + "w";
                        } else if (nextLyric.StartsWith("ssy") || (nextLyric.Contains("ssi"))) {
                            consonant = "ssy";
                        } else if (nextLyric.StartsWith("ssw") || (nextLyric.Contains("sso")) || (nextLyric.Contains("ssu"))) {
                            consonant = "ssw";
                        }
                    } else if (nextLyric.StartsWith("y")) {
                        consonant = "y";
                    } else if (nextLyric.StartsWith("w")) {
                        consonant = "w";
                    } else if (nextExist && nextHangeul && TNLconsonant != "") {
                        consonant = TNLconsonant;
                    } else if (nextLyric.StartsWith("ch")) { consonant = "ch"; }

                    if (!nextHangeul) {
                        VC = TCLplainvowel + " " + consonant;
                        CC = TCLplainfinal + " " + consonant;

                    }
                }

                // if there is a batchim with CC
                if (FC != "") {
                    if (CC != "" && singer.TryGetMappedOto(CC, note.tone + attr0.toneShift, attr0.voiceColor, out _)) {
                        if (nextHangeul && (TNLconsonant != "" || TNLconsonantCVVC != "") && CC != "") {
                            int totalDuration = notes.Sum(n => n.duration);
                            int fcLength = totalDuration / 2;
                            if ((TCLfinal == "K") || (TCLfinal == "P") || (TCLfinal == "T")) { fcLength = totalDuration / 2; }

                            if (nextExist) { if ((nextNeighbour?.lyric)[0] == 'ㄹ') { CC = $"{TCLplainfinal} {TNLconsonant = "l"}"; } }

                            int ccLength = 60;
                            if (TNLconsonant == "r") { ccLength = 30; } else if (TNLconsonant == "s") { ccLength = totalDuration / 3; } else if ((TNLconsonant == "k") || (TNLconsonant == "t") || (TNLconsonant == "p") || (TNLconsonant == "ch")) { ccLength = totalDuration / 3; } else if ((TNLconsonant == "gg") || (TNLconsonant == "dd") || (TNLconsonant == "bb") || (TNLconsonant == "ss") || (TNLconsonant == "jj")) { ccLength = totalDuration / 3; }
                            if (singer.TryGetMappedOto(CV, note.tone + attr0.toneShift, attr0.voiceColor, out var oto1) && singer.TryGetMappedOto(FC, note.tone + attr0.toneShift, attr0.voiceColor, out var oto2) && singer.TryGetMappedOto(CC, note.tone + attr0.toneShift, attr0.voiceColor, out var oto3)) {
                                CV = oto1.Alias;
                                FC = oto2.Alias;
                                CC = oto3.Alias;
                                return new Result {
                                    phonemes = new Phoneme[] {
                                new Phoneme() {
                                    phoneme = CV,
                                },
                                new Phoneme() {
                                    phoneme = FC,
                                    position = totalDuration - fcLength,
                                },
                                new Phoneme() {
                                    phoneme = CC,
                                    position = totalDuration - ccLength,
                                }
                            },
                                };
                            }
                        } else if (!nextHangeul && singer.TryGetMappedOto(CC, note.tone + attr0.toneShift, attr0.voiceColor, out _)) {
                            int totalDuration = notes.Sum(n => n.duration);
                            int fcLength = totalDuration / 3;
                            if ((TCLfinal == "K") || (TCLfinal == "P") || (TCLfinal == "T")) { fcLength = totalDuration / 2; }
                            int ccLength = 60;
                            var nextUnicode = ToUnicodeElements(nextNeighbour?.lyric);
                            var nextLyric = string.Join("", nextUnicode);
                            if (singer.TryGetMappedOto(nextLyric, nextNeighbour.Value.tone + attr0.toneShift, attr0.voiceColor, out var oto0)) {
                                if (oto0.Overlap < 0) {
                                    ccLength = MsToTick(oto0.Preutter - oto0.Overlap);
                                    //ccLength = totalDuration / 2;
                                } else {
                                    ccLength = MsToTick(oto0.Preutter);
                                }
                            }

                            if (singer.TryGetMappedOto(CV, note.tone + attr0.toneShift, attr0.voiceColor, out var oto1) && singer.TryGetMappedOto(FC, note.tone + attr0.toneShift, attr0.voiceColor, out var oto2) && singer.TryGetMappedOto(CC, note.tone + attr0.toneShift, attr0.voiceColor, out var oto3)) {
                                CV = oto1.Alias;
                                FC = oto2.Alias;
                                CC = oto3.Alias;
                                return new Result {
                                    phonemes = new Phoneme[] {
                                    new Phoneme() {
                                        phoneme = CV,
                                    },
                                    new Phoneme() {
                                        phoneme = FC,
                                        position = totalDuration - fcLength,
                                    },
                                    new Phoneme() {
                                        phoneme = CC,
                                        position = totalDuration - ccLength,
                                    }
                                },
                                };
                            }
                        }
                        // if there is a batchim but no CC
                    } else {
                        int totalDuration = notes.Sum(n => n.duration);
                        int fcLength = totalDuration / 3;
                        if ((TCLfinal == "K") || (TCLfinal == "P") || (TCLfinal == "T")) { fcLength = totalDuration / 2; }

                        if (singer.TryGetMappedOto(CV, note.tone + attr0.toneShift, attr0.voiceColor, out var oto1) && singer.TryGetMappedOto(FC, note.tone + attr0.toneShift, attr0.voiceColor, out var oto2)) {
                            CV = oto1.Alias;
                            FC = oto2.Alias;
                            return new Result {
                                phonemes = new Phoneme[] {
                                new Phoneme() {
                                    phoneme = CV,
                                },
                                new Phoneme() {
                                    phoneme = FC,
                                    position = totalDuration - fcLength,
                                }
                            },
                            };
                        }
                    }

                }

                // if there is no batchim
                if (TCLfinal == "") {
                    // if there is a next note
                    if (nextExist) { if ((nextNeighbour?.lyric)[0] == 'ㄹ') { VC = $"{TCLplainvowel} {TNLconsonant = "l"}"; } }
                    if ((VC != "") && (TNLconsonant != "" || TNLconsonantCVVC != "") && singer.TryGetMappedOto(VC, nextNeighbour.Value.tone + attr0.toneShift, attr0.voiceColor, out var otoVC)) {
                        int totalDuration = notes.Sum(n => n.duration);
                        int vcLength = 60;

                        if (TNLconsonant == "r") { vcLength = 30; } else if (TNLconsonant == "s") { vcLength = totalDuration / 3; } else if ((TNLconsonant == "k") || (TNLconsonant == "t") || (TNLconsonant == "p") || (TNLconsonant == "ch")) { vcLength = totalDuration / 2; } else if ((TNLconsonant == "gg") || (TNLconsonant == "dd") || (TNLconsonant == "bb") || (TNLconsonant == "ss") || (TNLconsonant == "jj")) { vcLength = totalDuration / 2; }

                        if (singer.TryGetMappedOto(CV, note.tone + attr0.toneShift, attr0.voiceColor, out var oto1) && singer.TryGetMappedOto(VC, note.tone + attr0.toneShift, attr0.voiceColor, out var oto2)) {
                            CV = oto1.Alias;
                            VC = oto2.Alias;
                            return new Result {
                                phonemes = new Phoneme[] {
                                    new Phoneme() {
                                        phoneme = CV,
                                    },
                                    new Phoneme() {
                                        phoneme = VC,
                                        position = totalDuration - vcLength,
                                    }
                                },
                            };
                        }
                    } else if (VC != "" && (TNLconsonant == "" || TNLconsonantCVVC == "")) {
                        int totalDuration = notes.Sum(n => n.duration);
                        int vcLength = 60;
                        var nextUnicode = ToUnicodeElements(nextNeighbour?.lyric);
                        var nextLyric = string.Join("", nextUnicode);
                        if (singer.TryGetMappedOto(nextLyric, nextNeighbour.Value.tone + attr0.toneShift, attr0.voiceColor, out var oto0)) {
                            if (oto0.Overlap < 0) {
                                vcLength = MsToTick(oto0.Preutter - oto0.Overlap);
                            } else {
                                vcLength = MsToTick(oto0.Preutter);
                            }
                        }

                        if (singer.TryGetMappedOto(CV, note.tone + attr0.toneShift, attr0.voiceColor, out var oto1) && singer.TryGetMappedOto(VC, note.tone + attr0.toneShift, attr0.voiceColor, out var oto2)) {
                            CV = oto1.Alias;
                            VC = oto2.Alias;
                            return new Result {
                                phonemes = new Phoneme[] {
                                    new Phoneme() {
                                        phoneme = CV,
                                    },
                                    new Phoneme() {
                                        phoneme = VC,
                                        position = totalDuration - vcLength,
                                    }
                                },
                            };
                        }
                    }
                    // Others (last note without ending or VC)
                    if (singer.TryGetMappedOto(CV, note.tone + attr0.toneShift, attr0.voiceColor, out var oto)) {
                        CV = oto.Alias;
                        return new Result {
                            phonemes = new Phoneme[] {
                            new Phoneme() {
                                phoneme = CV,
                                }
                            },
                        };
                    }
                }
            }

            if (prevHangeul) {
                string endBreath = "";
                var vowEnd = vowelEndings;

                if (prevExist && TPLfinal == "" && vowEnd.Contains(currentLyric)) {
                    endBreath = $"{TPLplainvowel} {vowEnd}";
                    prevIsBreath = true; // to prevent this→→ case→→, for example... "[사, -, 사 (=notes)]" should be "[- sa,  a -, - sa(=phonemes)]", but it becomes [sa, a -, 사(=phonemes)] in phonemizer, so '사' note becomes *no sound.
                }

                if (singer.TryGetMappedOto(endBreath, note.tone + attr0.toneShift, attr0.voiceColor, out var oto)) {
                    endBreath = oto.Alias;
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme() {
                                phoneme = endBreath,
                            }
                        },
                    };
                }
            }



            // ======================================================================================


            if (prevNeighbour == null) {
                // Use "- V" or "- CV" if present in voicebank
                var initial = $"- {currentLyric}";
                string[] tests = new string[] { initial, currentLyric };
                // try [- XX] before trying plain lyric
                if (checkOtoUntilHit(tests, note, out var oto)) {
                    currentLyric = oto.Alias;
                }
            } else if (plainVowels.Contains(currentLyric) || plainDiphthongs.Contains(currentLyric) || plainConsonants.Contains(currentLyric) || vowelEndings.Contains(currentLyric)) {
                var prevUnicode = ToUnicodeElements(prevNeighbour?.lyric);
                var vowel = "";

                var prevLyric = string.Join("", prevUnicode);
                // Current note is VV
                if (vowelLookup.TryGetValue(prevUnicode.LastOrDefault() ?? string.Empty, out var vow)) {
                    vowel = vow;

                    var vowLyric = $"{vow} {currentLyric}";

                    if (prevLyric.EndsWith("eo")) {
                        vowLyric = $"eo {currentLyric}";
                    } else if (prevLyric.EndsWith("eu")) {
                        vowLyric = $"eu {currentLyric}";
                    } else if (prevLyric.EndsWith("NG")) {
                        vowLyric = $"NG {currentLyric}";
                    } else if (prevLyric.EndsWith("er")) {
                        vowLyric = $"er {currentLyric}";
                    }

                    // try vowlyric then currentlyric
                    string[] tests = new string[] { vowLyric, currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        currentLyric = oto.Alias;
                    }
                } else if (prevExist && prevHangeul && TPLfinal == "") {
                    var vowLyric = $"{TPLplainvowel} {currentLyric}";

                    string[] tests = new string[] { vowLyric, currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        currentLyric = oto.Alias;
                    }
                } else if (prevExist && prevHangeul && TPLfinal != "") {
                    var ccLyric = $"{TPLplainfinal} {currentLyric}";

                    string[] tests = new string[] { ccLyric, currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        currentLyric = oto.Alias;
                    }
                } else {
                    string[] tests = new string[] { currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        currentLyric = oto.Alias;
                    }
                }
            } else {
                string[] tests = new string[] { currentLyric };
                if (checkOtoUntilHit(tests, note, out var oto)) {
                    currentLyric = oto.Alias;
                }
            }

            if (nextNeighbour != null) { // If there is a next note
                var nextUnicode = ToUnicodeElements(nextNeighbour?.lyric);
                var nextLyric = string.Join("", nextUnicode);

                // Check if next note is a vowel and does not require VC
                if (plainVowels.Contains(nextUnicode.FirstOrDefault() ?? string.Empty)) {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme() {
                                phoneme = currentLyric,
                            }
                        },
                    };
                }

                // Insert VC before next neighbor
                // Get vowel from current note
                var vowel = "";

                if (vowelLookup.TryGetValue(currentUnicode.LastOrDefault() ?? string.Empty, out var vow)) {
                    vowel = vow;

                    if (currentLyric.Contains(TCLplainvowel)) {
                        vow = TCLplainvowel;
                    }

                    if (currentLyric.Contains("e") && !currentLyric.Contains("eui")) {
                        vowel = "e" + vowel;
                        vowel = vowel.Replace("ee", "e");
                    }
                }

                // Get consonant from next note
                var consonant = "";
                if (consonantLookup.TryGetValue(nextUnicode.FirstOrDefault() ?? string.Empty, out var con)) {
                    consonant = getConsonant(nextNeighbour?.lyric); //Romaja only
                    if ((!isAlphaCon(consonant) || con == "f" || con == "v" || con == "z" || con == "th" || con == "rr")) {
                        consonant = con;
                    } else if (nextLyric.StartsWith(con + "y") || (nextLyric.Contains("i") && !nextLyric.Contains("eui"))) {
                        consonant = con + "y";
                    } else if (nextLyric.StartsWith(con + "w") || (nextLyric.Contains("o") && !nextLyric.Contains("eo")) || (nextLyric.Contains("u") && !nextLyric.Contains("eu"))) {
                        consonant = con + "w";
                    } else if (nextLyric.StartsWith("ssy") || (nextLyric.Contains("ssi"))) {
                        consonant = "ssy";
                    } else if (nextLyric.StartsWith("ssw") || (nextLyric.Contains("sso")) || (nextLyric.Contains("ssu"))) {
                        consonant = "ssw";
                    }
                } else if (nextLyric.StartsWith("y")) {
                    consonant = "y";
                } else if (nextLyric.StartsWith("w")) {
                    consonant = "w";
                } else if (nextExist && nextHangeul && TNLconsonant != "" && TNLsemivowel == 0) {
                    consonant = TNLconsonant;
                } else if (nextExist && nextHangeul && TNLconsonant == "" && TNLsemivowel == 1) {
                    consonant = "y";
                } else if (nextExist && nextHangeul && TNLconsonant == "" && TNLsemivowel == 2) {
                    consonant = "w";
                } else if (nextExist && nextHangeul && TNLconsonant != "" && TNLsemivowel == 1) {
                    consonant = TNLconsonant + "y";
                } else if (nextExist && nextHangeul && TNLconsonant != "" && TNLsemivowel == 2) {
                    consonant = TNLconsonant + "w";
                } else if (nextExist && nextHangeul && nextLyric.StartsWith('ㄹ')) {
                    consonant = "l";
                } else if (nextExist && nextHangeul && TNLconsonant == "") {
                    consonant = "";
                } else if (nextLyric.StartsWith("ch")) { consonant = "ch"; }


                if (consonant == "") {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme() {
                                phoneme = currentLyric,
                            }
                        },
                    };
                }

                var vcPhoneme = $"{vowel} {consonant}";
                if (currentLyric.Contains("NG")) {
                    vcPhoneme = $"NG {consonant}";
                }
                if (currentLyric.Contains("P")) {
                    vcPhoneme = $"P {consonant}";
                }
                if (currentLyric.Contains("K")) {
                    vcPhoneme = $"K {consonant}";
                }
                var vcPhonemes = new string[] { vcPhoneme, "" };
                if (checkOtoUntilHit(vcPhonemes, note, out var oto1)) {
                    vcPhoneme = oto1.Alias;
                } else {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme() {
                                phoneme = currentLyric,
                            }
                        },
                    };
                }

                int totalDuration = notes.Sum(n => n.duration);
                int vcLength = 60;
                var nextAttr = nextNeighbour.Value.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
                if (singer.TryGetMappedOto(nextLyric, nextNeighbour.Value.tone + nextAttr.toneShift, nextAttr.voiceColor, out var oto)) {
                    // If overlap is a negative value, vcLength is longer than Preutter
                    if (oto.Overlap < 0) {
                        vcLength = MsToTick(oto.Preutter - oto.Overlap);
                    } else {
                        vcLength = MsToTick(oto.Preutter);
                    }
                } else if (TNLconsonant == "r") { vcLength = 30; } else if (TNLconsonant == "s") { vcLength = totalDuration / 3; } else if ((TNLconsonant == "k") || (TNLconsonant == "t") || (TNLconsonant == "p") || (TNLconsonant == "ch")) { vcLength = totalDuration / 2; } else if ((TNLconsonant == "gg") || (TNLconsonant == "dd") || (TNLconsonant == "bb") || (TNLconsonant == "ss") || (TNLconsonant == "jj")) { vcLength = totalDuration / 2; }

                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme() {
                            phoneme = currentLyric,
                        },
                        new Phoneme() {
                            phoneme = vcPhoneme,
                            position = totalDuration - vcLength,
                        }
                    },
                };
            }

            // No next neighbor
            return new Result {
                phonemes = new Phoneme[] {
                    new Phoneme {
                        phoneme = currentLyric,
                    }
                },
            };
        }
    }
}

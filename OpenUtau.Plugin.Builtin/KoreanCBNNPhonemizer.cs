using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;
using Serilog;
using static OpenUtau.Api.Phonemizer;

namespace OpenUtau.Plugin.Builtin {
     /// This phonemizer is based on 'KOR CVC Phonemizer'(by NANA). ///
    [Phonemizer("Korean CBNN Phonemizer", "KO CBNN", "EX3", language:"KO")]

    public class KoreanCBNNPhonemizer : Phonemizer {

        // ↓ Plainvowels of [ㅏ ㅐ ㅑ ㅒ ㅓ ㅔ ㅕ ㅖ ㅗ ㅘ ㅙ ㅚ ㅛ ㅜ ㅝ ㅞ ㅟ ㅠ ㅡ ㅢ ㅣ]. //
        static readonly string[] naPlainVowels = new string[] { "a", "e", "a", "e", "eo", "e", "eo", "e", "o", "a", "e", "e", "o", "u", "eo", "e", "i", "u", "eu", "i", "i" };
        static readonly string[] naConsonants = new string[] {
            "ㄱ:g","ㄲ:gg","ㄴ:n","ㄷ:d","ㄸ:dd","ㄹ:r","ㅁ:m","ㅂ:b","ㅃ:bb","ㅅ:s","ㅆ:ss","ㅇ:","ㅈ:j","ㅉ:jj","ㅊ:ch","ㅋ:k","ㅌ:t","ㅍ:p","ㅎ:h"
        };

        // ↓ ㅢ is e (* There's no "eui" in Kor CBNN *).//
        static readonly string[] naVowels = new string[] {
            "ㅏ:a","ㅐ:e","ㅑ:ya","ㅒ:ye","ㅓ:eo","ㅔ:e","ㅕ:yeo","ㅖ:ye","ㅗ:o","ㅘ:wa","ㅙ:we","ㅚ:we","ㅛ:yo","ㅜ:u","ㅝ:weo","ㅞ:we","ㅟ:wi","ㅠ:yu","ㅡ:eu","ㅢ:e","ㅣ:i"
        };

        // ↓ ["Grapheme : Phoneme"] of batchims.
        static readonly string[] naFinals = new string[] {
            ":","ㄱ:k","ㄲ:k","ㄳ:k","ㄴ:n","ㄵ:n","ㄶ:n","ㄷ:t","ㄹ:l","ㄺ:l","ㄻ:m","ㄼ:l","ㄽ:l","ㄾ:l","ㄿ:p","ㅀ:l","ㅁ:m","ㅂ:p","ㅄ:p","ㅅ:t","ㅆ:t","ㅇ:ng","ㅈ:t","ㅊ:t","ㅋ:k","ㅌ:t","ㅍ:p:1","ㅎ:t:2"
        };
        private const int hangeulStartIndex = 0xAC00; // unicode of '가'
        private const int hangeulEndIndex = 0xD7A3; // unicode of '힣'

        // ======================================================================================


        // ↓ Plain vowels of Korean.
        static readonly string[] plainVowels = new string[] { "eu", "eo", "a", "i", "u", "e", "o" };

        // ↓ Vowels of romanized CVs.
        static readonly string[] vowels = new string[] {
            "eu=geu,neu,deu,reu,meu,beu,seu,eu,jeu,cheu,keu,teu,peu,heu,ggeu,ddeu,bbeu,sseu,jjeu",
            "eo=geo,neo,deo,reo,meo,beo,seo,eo,jeo,cheo,keo,teo,peo,heo,ggeo,ddeo,bbeo,sseo,jjeo,gyeo,nyeo,dyeo,ryeo,myeo,byeo,syeo,yeo,jyeo,chyeo,kyeo,tyeo,pyeo,hyeo,ggyeo,ddyeo,bbyeo,ssyeo,jjyeo,gweo,nweo,dweo,rweo,mweo,bweo,sweo,weo,jweo,chweo,kweo,tweo,pweo,hweo,ggweo,ddweo,bbweo,ssweo,jjweo",
            "a=ga,na,da,ra,ma,ba,sa,a,ja,cha,ka,ta,pa,ha,gga,dda,bba,ssa,jja,gya,nya,dya,rya,mya,bya,sya,ya,jya,chya,kya,tya,pya,hya,ggya,ddya,bbya,ssya,jjya,gwa,nwa,dwa,rwa,mwa,bwa,swa,wa,jwa,chwa,kwa,twa,pwa,hwa,ggwa,ddwa,bbwa,sswa,jjwa",
            "e=ge,ne,de,re,me,be,se,e,je,che,ke,te,pe,he,gge,dde,bbe,sse,jje,gye,nye,dye,rye,mye,bye,sye,ye,jye,chye,kye,tye,pye,hye,ggye,ddye,bbye,ssye,jjye,gwe,nwe,dwe,rwe,mwe,bwe,swe,we,jwe,chwe,kwe,twe,pwe,hwe,ggwe,ddwe,bbwe,sswe,jjwe",
            "i=gi,ni,di,ri,mi,bi,si,i,ji,chi,ki,ti,pi,hi,ggi,ddi,bbi,ssi,jji,gwi,nwi,dwi,rwi,mwi,bwi,swi,wi,jwi,chwi,kwi,twi,pwi,hwi,ggwi,ddwi,bbwi,sswi,jjwi",
            "o=go,no,do,ro,mo,bo,so,o,jo,cho,ko,to,po,ho,ggo,ddo,bbo,sso,jjo,gyo,nyo,dyo,ryo,myo,byo,syo,yo,jyo,chyo,kyo,tyo,pyo,hyo,ggyo,ddyo,bbyo,ssyo,jjyo",
            "u=gu,nu,du,ru,mu,bu,su,u,ju,chu,ku,tu,pu,hu,ggu,ddu,bbu,ssu,jju,gyu,nyu,dyu,ryu,myu,byu,syu,yu,jyu,chyu,kyu,tyu,pyu,hyu,ggyu,ddyu,bbyu,ssyu,jjyu",
            "ng=ang,ing,ung,eng,ong,eung,eong",
            "n=an,in,un,en,on,eun,eon",
            "m=am,im,um,em,om,eum,eom",
            "l=al,il,ul,el,ol,eul,eol",
            "p=ap,ip,up,ep,op,eup,eop",
            "t=at,it,ut,et,ot,eut,eot",
            "k=ak,ik,uk,ek,ok,euk,eok"
        };

        // ↓ consonants of romanized CVs.
        static readonly string[] consonants = new string[] {
            "ggy=ggya,ggyu,ggye,ggyo,ggyeo",
            "ggw=ggwa,ggwi,ggwe,ggweo",
            "gg=gg,gga,ggi,ggu,gge,ggo,ggeu,ggeo",
            "ddy=ddya,ddyu,ddye,ddyo,ddyeo",
            "ddw=ddwa,ddwi,ddwe,ddweo",
            "dd=dd,dda,ddi,ddu,dde,ddo,ddeu,ddeo",
            "bby=bbya,bbyu,bbye,bbyo,bbyeo",
            "bbw=bbwa,bbwi,bbwe,bbweo",
            "bb=bb,bba,bbi,bbu,bbe,bbo,bbeu,bbeo",
            "ssy=ssya,ssyu,ssye,ssyo,ssyeo",
            "ssw=sswa,sswi,sswe,ssweo",
            "ss=ss,ssa,ssi,ssu,sse,sso,sseu,sseo",
            "gy=gya,gyu,gye,gyo,gyeo",
            "gw=gwa,gwi,gwe,gweo",
            "g=g,ga,gi,gu,ge,go,geu,geo",
            "ny=nya,nyu,nye,nyo,nyeo",
            "nw=nwa,nwi,nwe,nweo",
            "n=n,na,ni,nu,ne,no,neu,neo",
            "dy=dya,dyu,dye,dyo,dyeo",
            "dw=dwa,dwi,dwe,dweo",
            "d=d,da,di,du,de,do,deu,deo",
            "ry=rya,ryu,rye,ryo,ryeo",
            "rw=rwa,rwi,rwe,rweo",
            "r=r,ra,ri,ru,re,ro,reu,reo",            
            "my=mya,myu,mye,myo,myeo",
            "mw=mwa,mwi,mwe,mweo",
            "m=m,ma,mi,mu,me,mo,meu,meo",
            "by=bya,byu,bye,byo,byeo",
            "bw=bwa,bwi,bwe,bweo",
            "b=b,ba,bi,bu,be,bo,beu,beo",
            "sy=sya,syu,sye,syo,syeo",
            "sw=swa,swi,swe,sweo",
            "s=s,sa,si,su,se,so,seu,seo",
            "jy=jya,jyu,jye,jyo,jyeo",
            "jw=jwa,jwi,jwe,jweo",
            "j=j,ja,ji,ju,je,jo,jeu,jeo",            
            "chy=chya,chyu,chye,chyo,chyeo,chwa",
            "chw=chwi,chwe,chweo",
            "ch=ch,cha,chi,chu,che,cho,cheu,cheo",
            "ky=kya,kyu,kye,kyo,kyeo",
            "kw=kwa,kwi,kwe,kweo",
            "k=k,ka,ki,ku,ke,ko,keu,keo",
            "ty=tya,tyu,tye,tyo,tyeo",
            "tw=twa,twi,twe,tweo",
            "t=t,ta,ti,tu,te,to,teu,teo",
            "py=pya,pyu,pye,pyo,pyeo",
            "pw=pwa,pwi,pwe,pweo",
            "p=p,pa,pi,pu,pe,po,peu,peo",
            "hy=hya,hyu,hye,hyo,hyeo",
            "hw=hwa,hwi,hwe,hweo",
            "h=h,ha,hi,hu,he,ho,heu,heo"
            };

        static readonly Dictionary<string, string> vowelLookup;
        static readonly Dictionary<string, string> consonantLookup;

        string getConsonant(string str) {
            str = str.Replace('a', ' ');
            str = str.Replace('i', ' ');
            str = str.Replace('u', ' ');
            str = str.Replace('e', ' ');
            str = str.Replace('o', ' ');
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
            else if (str == "h") { return true; }else { return false; }
        }

        static KoreanCBNNPhonemizer() {
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
            private bool checkOtoUntilHit(string[] input, Note note, out UOto oto){
                oto = default;

                var attr0 = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
                var attr1 = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 1) ?? default;

                foreach (string test in input){
                    if (singer.TryGetMappedOto(test, note.tone + attr0.toneShift, attr0.voiceColor, out oto)){
                        return true;
                    }
                }

                return false;
            }

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            var note = notes[0];
            var currentUnicode = ToUnicodeElements(note.lyric); // ← unicode of current lyric
            string currentLyric = note.lyric; // ← string of current lyric
            var attr0 = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
            var attr1 = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 1) ?? default;
            
            //-----------------------------------------------------------------------//
            ////// ***   ↓↓↓ Seperates Lyrics in:                                     //
            /////           - first consonant letter(초성, "consonant" in below),    //
            /////           -  middle vowel letter(중성, "vowel" in below),          //
            /////           -  last consonant letter(종성, "final" in below) ↓↓↓  *** //.


            ////  ↓↓ 1 ** Variables for 'Current Notes' ** --
            // ↓ index of "consonant", "vowel", "final".
            int CLconsonant = 0;
            int CLvowel = 0;
            int CLfinal = 0; 

            // ↓ Use for Temp
            string[] TCLtemp;

            // ↓ use these for applying phonological rules
            string TCLconsonant = "";
            string TCLvowel = "";
            string TCLfinal = "";
            string TCLplainvowel = ""; //← Simplifies vowels

            int TCLsemivowel = 0; // semi vowel is 'y', 'w'. [0 means "there's no semi vowel], [1 means "there is 'y'"], [2 means "there is 'w'"]]
            
            // ↓ use these for generating phonemes in phonemizers 
            string TCLconsonantCBNN = "";
            string TCLvowelCBNN = "";

            ////  ↓↓ 2 ** Variables for 'Next Notes' ** --
            // ↓ index of "consonant", "vowel", "final".
            int NLconsonant = 0;
            int NLvowel = 0;
            int NLfinal = 0;

            // ↓ Use for Temp
            string[] TNLtemp;

            // ↓ use these for applying phonological rules
            string TNLconsonant = "";
            string TNLvowel = "";
            string TNLfinal = "";
            string TNLplainvowel = "";

            // ↓ use these for generating phonemes in phonemizers 
            string TNLconsonantCBNN = "";
            //string TNLvowelCBNN = "";

            int TNLsemivowel = 0; // semi vowel is 'y', 'w'. [0 means "there's no semi vowel], [1 means "there is 'y'"], [2 means "there is 'w'"]]

            ////  ↓↓ 3 ** Variables for 'Previous Notes' ** --
            // ↓ index of "consonant", "vowel", "final".
            int PLconsonant = 0;
            int PLvowel = 0;
            int PLfinal = 0;
            
            // ↓ Use for Temp
            string[] TPLtemp;

            // ↓ use these for applying phonological rules
            string TPLconsonant = "";
            string TPLvowel = "";
            string TPLfinal = "";
            string TPLplainvowel = "";
            string TPLplainfinal = "";

            // ↓ use these for generating phonemes in phonemizers 
            //string TPLconsonantCBNN = "";
            //string TPLvowelCBNN = "";

            //int TPLsemivowel = 0; // semi vowel is 'y', 'w'. [0 means "there's no semi vowel], [1 means "there is 'y'"], [2 means "there is 'w'"]]


            ////  ↓↓ 4 ** Variables for checking notes ** --
            bool currentHangeul = false;
            bool prevHangeul = false;
            bool nextHangeul = false;

            bool prevExist = false;
            bool nextExist = false;

            char firstCL, firstPL, firstNL;
            int uCL, uPL, uNL;
            bool prevIsBreath = false;


            // check first lyric
            firstCL = currentLyric[0];
            
            uCL = (int)firstCL;
            if ((uCL >= hangeulStartIndex) && (uCL <= hangeulEndIndex)) {
                currentHangeul = true;
                CLconsonant = (uCL - hangeulStartIndex) / (21 * 28);
                CLvowel = (uCL - hangeulStartIndex) % (21 * 28) / 28;
                CLfinal = (uCL - hangeulStartIndex) % 28;
 

                TCLtemp = naVowels[CLvowel].Split(":");
                TCLvowel = TCLtemp[1];
                TCLplainvowel = naPlainVowels[CLvowel];
                
                if (TCLvowel.StartsWith('y')) {TCLsemivowel = 1;} 
                else if (TCLvowel.StartsWith('w')) {TCLsemivowel = 2;}
                
                TCLtemp = naConsonants[CLconsonant].Split(":");
                TCLconsonant = TCLtemp[1];

                TCLtemp = naFinals[CLfinal].Split(":");
                TCLfinal = TCLtemp[1];


                // TCLconsonant : 현노트 초성    TCLvowel : 현노트 중성    TCLfinal : 현노트 종성

            }

            // 이전 노트 존재 여부 확인 + 이전 노트 첫번째 글자 확인
            if (prevNeighbour != null) {
                firstPL = (prevNeighbour?.lyric)[0]; // 가사 받아오기
                prevExist = true; // 이전 노트 존재한다 반짝
                
                uPL = (int)firstPL; // 가사를 int로 변환

                if ((uPL >= hangeulStartIndex) && (uPL <= hangeulEndIndex)) {
                    prevHangeul = true;

                    PLconsonant = (uPL - hangeulStartIndex) / (21 * 28);
                    PLvowel = (uPL - hangeulStartIndex) % (21 * 28) / 28;
                    PLfinal = (uPL - hangeulStartIndex) % 28;


                    TPLtemp = naConsonants[PLconsonant].Split(":");
                    TPLconsonant = TPLtemp[1];

                    TPLtemp = naVowels[PLvowel].Split(":");
                    TPLvowel = TPLtemp[1];
                    TPLplainvowel = naPlainVowels[PLvowel];

                    //if (TPLvowel.StartsWith('y')) {TPLsemivowel = 1;} 
                    //else if (TPLvowel.StartsWith('w')) {TPLsemivowel = 2;}
                
                    TPLtemp = naFinals[PLfinal].Split(":");
                    TPLfinal = TPLtemp[1];
                    TPLplainfinal = TPLfinal;
                }
            }

            // 다음 노트 존재 여부 확인 + 다음 노트 첫번째 글자 확인
            if (nextNeighbour != null) {
                firstNL = (nextNeighbour?.lyric)[0];
                nextExist = true;
                uNL = (int)firstNL;

                if ((uNL >= hangeulStartIndex) && (uNL <= hangeulEndIndex)) {
                    nextHangeul = true;

                    NLconsonant = (uNL - hangeulStartIndex) / (21 * 28);
                    NLvowel = (uNL - hangeulStartIndex) % (21 * 28) / 28;
                    NLfinal = (uNL - hangeulStartIndex) % 28;


                    TNLtemp = naConsonants[NLconsonant].Split(":");
                    TNLconsonant = TNLtemp[1];

                    TNLtemp = naVowels[NLvowel].Split(":");
                    TNLvowel = TNLtemp[1];
                    TNLplainvowel = naPlainVowels[NLvowel];

                    if (TNLvowel.StartsWith('y')) {TNLsemivowel = 1;} 
                    else if (TNLvowel.StartsWith('w')) {TNLsemivowel = 2;}
                

                    TNLtemp = naFinals[NLfinal].Split(":");
                    TNLfinal = TNLtemp[1];
                }
            }

            if (currentHangeul) {
                // 음운규칙 적용
                if (currentHangeul) {

                    // 1. 연음법칙 
                    string tempTCLconsonant = "";
                    string tempTCLfinal = "";
                    bool yeoneum = false;
                    bool yeoneum2 = false;

                    if (prevExist && prevHangeul && (CLconsonant == 11) && (TPLfinal != "")) {
                        int temp = PLfinal;
                        if (temp == 1) { TCLtemp = naConsonants[0].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 2) { TCLtemp = naConsonants[1].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 3) { TCLtemp = naConsonants[10].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 4) { TCLtemp = naConsonants[2].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 5) { TCLtemp = naConsonants[12].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 6) { TCLtemp = naConsonants[18].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 7) { TCLtemp = naConsonants[3].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 8) { TCLtemp = naConsonants[5].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 9) { TCLtemp = naConsonants[0].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 10) { TCLtemp = naConsonants[6].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 11) { TCLtemp = naConsonants[7].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 12) { TCLtemp = naConsonants[9].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 13) { TCLtemp = naConsonants[16].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 14) { TCLtemp = naConsonants[17].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 15) { TCLtemp = naConsonants[18].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 16) { TCLtemp = naConsonants[6].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 17) { TCLtemp = naConsonants[7].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 18) { TCLtemp = naConsonants[9].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 19) { TCLtemp = naConsonants[9].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 20) { TCLtemp = naConsonants[10].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 21) { tempTCLconsonant = ""; yeoneum = true; }
                        else if (temp == 22) { TCLtemp = naConsonants[12].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 23) { TCLtemp = naConsonants[14].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 24) { TCLtemp = naConsonants[15].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 25) { TCLtemp = naConsonants[16].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 26) { TCLtemp = naConsonants[17].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                        else if (temp == 27) { TCLtemp = naConsonants[18].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                    }

                    if (nextExist && nextHangeul && (TCLfinal != "") && (TNLconsonant == "")) {
                        int temp = CLfinal;

                        if (temp == 1) { TCLtemp = naConsonants[0].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; }
                        else if (temp == 2) { TCLtemp = naConsonants[1].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; }
                        else if (temp == 3) { TCLfinal = "k"; yeoneum2 = true; }
                        else if (temp == 4) { TCLtemp = naConsonants[2].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; }
                        else if (temp == 5) { TCLfinal = "n"; yeoneum2 = true; }
                        else if (temp == 6) { TCLfinal = "n"; yeoneum2 = true; }
                        else if (temp == 7) { TCLtemp = naConsonants[3].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; }
                        else if (temp == 8) { TCLtemp = naConsonants[5].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; }
                        else if (temp == 9) { TCLfinal = "l"; yeoneum2 = true; }
                        else if (temp == 10) { TCLfinal = "l"; yeoneum2 = true; }
                        else if (temp == 11) { TCLfinal = "l"; yeoneum2 = true; }
                        else if (temp == 12) { TCLfinal = "l"; yeoneum2 = true; }
                        else if (temp == 13) { TCLfinal = "l"; yeoneum2 = true; }
                        else if (temp == 14) { TCLfinal = "l"; yeoneum2 = true; }
                        else if (temp == 15) { TCLfinal = "l"; yeoneum2 = true; }
                        else if (temp == 16) { TCLtemp = naConsonants[6].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; }
                        else if (temp == 17) { TCLtemp = naConsonants[7].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; }
                        else if (temp == 18) { TCLfinal = "p"; yeoneum2 = true; }
                        else if (temp == 19) { TCLtemp = naConsonants[9].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; }
                        else if (temp == 20) { TCLtemp = naConsonants[10].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; }
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             //else if (temp == 21) { TCLtemp = naConsonants[11].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; }
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             else if (temp == 22) { TCLtemp = naConsonants[12].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; } else if (temp == 23) { TCLtemp = naConsonants[14].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; } else if (temp == 24) { TCLtemp = naConsonants[15].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; } else if (temp == 25) { TCLtemp = naConsonants[16].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; } else if (temp == 26) { TCLtemp = naConsonants[17].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; } else if (temp == 27) { TCLtemp = naConsonants[18].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; }

                    }
                    if (yeoneum) { TCLconsonant = tempTCLconsonant; }
                    if (yeoneum2) { TNLconsonant = tempTCLfinal; }


                    // 2. 격음화/유기음화/거센소리되기
                    if (prevExist && prevHangeul && (TPLfinal != "")) {
                        if (((PLfinal == 27) && (CLconsonant == 0)) || ((PLfinal == 6) && (CLconsonant == 0)) || ((PLfinal == 15) && (CLconsonant == 0))) { TCLconsonant = "k"; } else if (((PLfinal == 27) && (CLconsonant == 3)) || ((PLfinal == 6) && (CLconsonant == 3)) || ((PLfinal == 15) && (CLconsonant == 3))) { TCLconsonant = "t"; } else if (((PLfinal == 27) && (CLconsonant == 12)) || ((PLfinal == 6) && (CLconsonant == 12)) || ((PLfinal == 15) && (CLconsonant == 12))) { TCLconsonant = "ch"; } else if (((PLfinal == 27) && (CLconsonant == 9)) || ((PLfinal == 6) && (CLconsonant == 9)) || ((PLfinal == 15) && (CLconsonant == 9))) { TCLconsonant = "ss"; }

                        if ((PLfinal == 1) && (CLconsonant == 18)) { TCLconsonant = "k"; } else if ((PLfinal == 7) && (CLconsonant == 18)) { TCLconsonant = "t"; } else if ((PLfinal == 17) && (CLconsonant == 18)) { TCLconsonant = "p"; } else if ((PLfinal == 22) && (CLconsonant == 18)) { TCLconsonant = "ch"; }
                    }
                    if (nextExist && nextHangeul && (TCLfinal != "")) {
                        if ((NLconsonant == 0) && (CLfinal == 27)) { TCLfinal = ""; TNLconsonant = "k"; } else if ((NLconsonant == 0) && (CLfinal == 6)) { TCLfinal = "n"; TNLconsonant = "k"; } else if ((NLconsonant == 0) && (CLfinal == 15)) { TCLfinal = "l"; TNLconsonant = "k"; } else if ((NLconsonant == 3) && (CLfinal == 27)) { TCLfinal = ""; TNLconsonant = "t"; } else if ((NLconsonant == 3) && (CLfinal == 6)) { TCLfinal = "n"; TNLconsonant = "t"; } else if ((NLconsonant == 3) && (CLfinal == 15)) { TCLfinal = "l"; TNLconsonant = "t"; } else if ((NLconsonant == 12) && (CLfinal == 27)) { TCLfinal = ""; TNLconsonant = "ch"; } else if ((NLconsonant == 12) && (CLfinal == 6)) { TCLfinal = "n"; TNLconsonant = "ch"; } else if ((NLconsonant == 12) && (CLfinal == 15)) { TCLfinal = "l"; TNLconsonant = "ch"; } else if ((NLconsonant == 9) && (CLfinal == 27)) { TCLfinal = ""; TNLconsonant = "ss"; } else if ((NLconsonant == 9) && (CLfinal == 6)) { TCLfinal = "n"; TNLconsonant = "ss"; } else if ((NLconsonant == 9) && (CLfinal == 15)) { TCLfinal = "l"; TNLconsonant = "ss"; }

                        if ((NLconsonant == 2) && (CLfinal == 27)) { TCLfinal = "n"; }

                        if ((NLconsonant == 18) && (CLfinal == 1)) { TCLfinal = ""; TNLconsonant = "k"; } else if ((NLconsonant == 18) && (CLfinal == 7)) { TCLfinal = ""; TNLconsonant = "t"; } else if ((NLconsonant == 18) && (CLfinal == 17)) { TCLfinal = ""; TNLconsonant = "p"; } else if ((NLconsonant == 18) && (CLfinal == 22)) { TCLfinal = ""; TNLconsonant = "ch"; }
                    }


                    // 3. 음절의 끝소리 규칙 예외
                    if (nextExist && nextHangeul) {
                        /*
                        // ㄼ + 자음이 있을 때 => ㄼ : p
                        if ((CLfinal == 11) && (TCLconsonant != "")) { TCLfinal = "p"; }
                        */
                        // ㄺ + ㄱ => ㄺ : ㄹ
                        if ((CLfinal == 9) && (NLconsonant == 0)) { TCLfinal = "l"; }
                    }


                    // 4. 경음화/된소리되기
                    if (prevExist && prevHangeul && TPLfinal != "") {
                        // ㄱㄷㅂ + ㄱㄷㅂㅅㅈ = ㄲㄸㅃㅆㅉ
                        if (((TPLfinal == "k") && (CLconsonant == 0)) || ((TPLfinal == "t") && (CLconsonant == 0)) || ((TPLfinal == "p") && (CLconsonant == 0))) { TCLconsonant = "gg"; } else if (((TPLfinal == "k") && (CLconsonant == 3)) || ((TPLfinal == "t") && (CLconsonant == 3)) || ((TPLfinal == "p") && (CLconsonant == 3))) { TCLconsonant = "dd"; } else if (((TPLfinal == "k") && (CLconsonant == 7)) || ((TPLfinal == "t") && (CLconsonant == 7)) || ((TPLfinal == "p") && (CLconsonant == 7))) { TCLconsonant = "bb"; } else if (((TPLfinal == "k") && (CLconsonant == 9)) || ((TPLfinal == "t") && (CLconsonant == 9)) || ((TPLfinal == "p") && (CLconsonant == 9))) { TCLconsonant = "ss"; } else if (((TPLfinal == "k") && (CLconsonant == 12)) || ((TPLfinal == "t") && (CLconsonant == 12)) || ((TPLfinal == "p") && (CLconsonant == 12))) { TCLconsonant = "jj"; }

                    
                        // 용언 어간 받침 ㄴㅁ + ㄱㄷㅅㅈ = ㄲㄸㅆㅉ
                        if(((TPLfinal=="n")&&(CLconsonant==0))|| ((TPLfinal == "m") && (CLconsonant == 0))) { TCLconsonant = "gg"; }
                        else if (((TPLfinal == "n") && (CLconsonant == 3)) || ((TPLfinal == "m") && (CLconsonant == 3))) { TCLconsonant = "dd"; }
                        else if (((TPLfinal == "n") && (CLconsonant == 9)) || ((TPLfinal == "m") && (CLconsonant == 9))) { TCLconsonant = "ss"; }
                        else if (((TPLfinal == "n") && (CLconsonant == 12)) || ((TPLfinal == "m") && (CLconsonant == 12))) { TCLconsonant = "jj"; }

                        // 관형사형 어미ㄹ / 한자어 ㄹ + ㄷㅅㅈ = ㄸㅆㅉ
                        if ((PLfinal == 8) && (CLconsonant == 3)) { TCLconsonant = "dd"; } else if ((PLfinal == 8) && (CLconsonant == 9)) { TCLconsonant = "ss"; } else if ((PLfinal == 8) && (CLconsonant == 12)) { TCLconsonant = "jj"; }

                        // 어간 받침 ㄼㄾ + ㄱㄷㅅㅈ = ㄲㄸㅆㅉ
                        if (((PLfinal == 11) && (CLconsonant == 0)) || ((PLfinal == 13) && (CLconsonant == 0))) { TCLconsonant = "gg"; } else if (((PLfinal == 11) && (CLconsonant == 3)) || ((PLfinal == 13) && (CLconsonant == 3))) { TCLconsonant = "dd"; } else if (((PLfinal == 11) && (CLconsonant == 9)) || ((PLfinal == 13) && (CLconsonant == 9))) { TCLconsonant = "ss"; } else if (((PLfinal == 11) && (CLconsonant == 12)) || ((PLfinal == 13) && (CLconsonant == 12))) { TCLconsonant = "jj"; }
                    }


                    // 5. 구개음화 
                    if (prevExist && prevHangeul && (TPLfinal != "")) {
                        if ((PLfinal == 7) && (CLconsonant == 11) && (CLvowel == 20)) { TCLconsonant = "j"; } else if ((PLfinal == 25) && (CLconsonant == 11) && (CLvowel == 20)) { TCLconsonant = "ch"; } else if ((PLfinal == 13) && (CLconsonant == 11) && (CLvowel == 20)) { TCLconsonant = "ch"; } else if ((PLfinal == 7) && (CLconsonant == 18) && (CLvowel == 20)) { TCLconsonant = "ch"; }
                    }
                    if (nextExist && nextHangeul && (TCLfinal != "")) {
                        if ((CLfinal == 7) && (NLconsonant == 11) && (NLvowel == 20)) { TCLfinal = ""; } else if ((CLfinal == 25) && (NLconsonant == 11) && (NLvowel == 20)) { TCLfinal = ""; } else if ((CLfinal == 13) && (NLconsonant == 11) && (NLvowel == 20)) { TCLfinal = ""; } else if ((CLfinal == 7) && (NLconsonant == 18) && (NLvowel == 20)) { TCLfinal = ""; }

                    }


                    // 6. 비음화
                    /**
                    if (prevExist && prevHangeul && (TPLfinal != "")) {
                        // 한자어 받침 ㅁㅇ + ㄹ = ㄴ
                        if (((TPLfinal == "m") && (CLconsonant == 5)) || ((TPLfinal == "ng") && (CLconsonant == 5))) { TCLconsonant = "n"; }

                        // 한자어 받침 ㄱㄷㅂ + ㄹ = ㅇㄴㅁ + ㄴ(1)
                        if (((TPLfinal == "k") && (CLconsonant == 5)) || ((TPLfinal == "t") && (CLconsonant == 5)) || ((TPLfinal == "p") && (CLconsonant == 5))) { TCLconsonant = "n"; }
                    }
                    **/
                    if (nextExist && nextHangeul && (TCLfinal != "")) {
                        //받침 ㄱㄷㅂ + ㄴㅁ = ㅇㄴㅁ
                        if (((TCLfinal == "k") && (TNLconsonant == "n")) || ((TCLfinal == "k") && (TNLconsonant == "m"))) { TCLfinal = "ng"; } else if (((TCLfinal == "t") && (TNLconsonant == "n")) || ((TCLfinal == "t") && (TNLconsonant == "m"))) { TCLfinal = "n"; } else if (((TCLfinal == "p") && (TNLconsonant == "n")) || ((TCLfinal == "p") && (TNLconsonant == "m"))) { TCLfinal = "m"; }

                        // 한자어 받침 ㄱㄷㅂ + ㄹ = ㅇㄴㅁ + ㄴ(2)
                        if ((TCLfinal == "k") && (NLconsonant == 5)) { TCLfinal = "ng"; } else if ((TCLfinal == "t") && (NLconsonant == 5)) { TCLfinal = "n"; } else if ((TCLfinal == "p") && (NLconsonant == 5)) { TCLfinal = "m"; }
                    }


                    // 7. 유음화
                    /**
                    if (prevExist && prevHangeul && (TPLfinal != "")) {
                        if (((PLfinal == 8) && (TCLconsonant == "n")) || ((PLfinal == 13) && (TCLconsonant == "n")) || ((PLfinal == 15) && (TCLconsonant == "n"))) { TCLconsonant = "r"; }
                    }
                    if (nextExist && nextHangeul && (TCLfinal != "")) {
                        if ((TCLfinal == "n") && (TNLconsonant == "r")) { TCLfinal = "l"; }
                    }
                    **/



                    // 8. 받침 + ㄹ = ㄹㄹ



                    // consonant에 변경 사항이 있을 때
                    //if (prevExist && prevHangeul) {


                        // 비음화
                        // (1) ㄱ(ㄲㅋㄳㄺ)
                        //     ㄷ(ㅅ,ㅆ,ㅈ,ㅊ,ㅌ,ㅎ)
                        //     ㅂ(ㅍ,ㄼ,ㄿ,ㅄ)


                    //}
                    // final에 변경 사항이 있을 때


                }

                bool isLastBatchim = false;

                // vowels do not have suffixed phonemes in CBNN, so use suffixed '- h'~ phonemes instead. 
                if (!prevExist && TCLconsonant == "" && TCLfinal != "" && TCLvowel != "") {
                    TCLconsonant = "h";
                }
                
                // to make FC's length to 1 if FC comes final (=no next note)
                if (!nextHangeul && TCLfinal != "" &&TCLvowel != "") {
                    isLastBatchim = true;
                }

                // To use semivowels in VC (ex: [- ga][a gy][gya], ** so not [- ga][a g][gya] **)
                if (TCLsemivowel == 1 && TPLplainvowel != "i" && TPLplainvowel != "eu") {TCLconsonantCBNN = TCLconsonant + 'y';}
                else if (TCLsemivowel == 2 && TPLplainvowel != "u" && TPLplainvowel != "o" && TPLplainvowel != "eu") {TCLconsonantCBNN = TCLconsonant + 'w';}
                else {TCLconsonantCBNN = TCLconsonant;}

                if (TNLsemivowel == 1 && TCLplainvowel != "i" && TCLplainvowel != "eu") {TNLconsonantCBNN = TNLconsonant + 'y';}
                else if (TNLsemivowel == 2 && TCLplainvowel != "u" && TCLplainvowel != "o" && TCLplainvowel != "eu") {TNLconsonantCBNN = TNLconsonant + 'w';}
                else {TNLconsonantCBNN = TNLconsonant;}

                
                
                //To set suffix of CV, according to next-coming batchim.
                if (TCLfinal == "") {
                    TCLvowelCBNN = TCLvowel;}
                else if (TCLfinal == "m" && TCLconsonantCBNN != "" || TCLfinal == "m" && TCLconsonantCBNN == "" && TCLsemivowel != 0) {
                    TCLvowelCBNN = TCLvowel + '1';}
                else if (TCLfinal == "n" && TCLconsonantCBNN != ""  || TCLfinal == "n" && TCLconsonantCBNN == "" && TCLsemivowel != 0) {
                    TCLvowelCBNN = TCLvowel + '2';}
                else if (TCLfinal == "ng" && TCLconsonantCBNN != "" || TCLfinal == "ng" && TCLconsonantCBNN == "" && TCLsemivowel != 0) {
                    TCLvowelCBNN = TCLvowel + '3';} 
                else if (TCLfinal == "l" && TCLconsonantCBNN != "" || TCLfinal == "l" && TCLconsonantCBNN == "" && TCLsemivowel != 0) {
                    TCLvowelCBNN = TCLvowel + '4';}
                else if (TCLfinal == "k" && TCLconsonantCBNN != "" || TCLfinal == "k" && TCLconsonantCBNN == "" && TCLsemivowel != 0) {
                    TCLvowelCBNN = TCLvowel;}
                else if (TCLfinal == "t" && TCLconsonantCBNN != "" || TCLfinal == "t" && TCLconsonantCBNN == "" && TCLsemivowel != 0) {
                    TCLvowelCBNN = TCLvowel + '3';}
                else if (TCLfinal == "p" && TCLconsonantCBNN != "" || TCLfinal == "p" && TCLconsonantCBNN == "" && TCLsemivowel != 0) {
                    TCLvowelCBNN = TCLvowel + '1';}
                else {TCLvowelCBNN = TCLvowel;}


                string CV = (TCLconsonant + TCLvowelCBNN);
                string VC = "";
                bool comesSemivowelWithoutVC = false;
                

                if (TCLsemivowel != 0 && TCLconsonant == ""){
                    comesSemivowelWithoutVC = true;
                }
                if (nextExist && (TCLfinal == "")) { VC = TCLplainvowel + " " + TNLconsonantCBNN; }

                //for Vowel VCV
                if (prevExist && TPLfinal == "" && TCLconsonantCBNN == "" && !comesSemivowelWithoutVC) {CV = TPLplainvowel + " " + TCLvowel;}

                
                string FC = "";
                if (TCLfinal != "") { FC = TCLplainvowel + TCLfinal; }


                // for [- XX] phonemes
                if (!prevExist || prevIsBreath || TPLfinal != "" && TCLconsonant != "r" && TCLconsonant != "n" && TCLconsonant != "" ) { CV = $"- {CV}"; }

                
                // 만약 받침이 있다면
                if (FC != "") {
                    int totalDuration = notes.Sum(n => n.duration);
                    int fcLength = totalDuration / 3;

                    if (isLastBatchim) {
                        fcLength = 1;
                    }
                    else if ((TCLfinal == "k") || (TCLfinal == "p") || (TCLfinal == "t")) { 
                        fcLength = totalDuration / 2;}
                    else if ((TCLfinal == "l") || (TCLfinal == "ng") || (TCLfinal == "m")) { 
                        fcLength = totalDuration / 5;}
                    else if ((TCLfinal == "n")) {
                        fcLength = totalDuration / 3;
                    }

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


                // 만약 받침이 없다면
                if (TCLfinal == "") {
                    // 뒤에 노트가 있다면
                    if ((TNLconsonantCBNN != "")) {
                        int totalDuration = notes.Sum(n => n.duration);
                        int vcLength = 60;
                        if ((TNLconsonant == "r") || (TNLconsonant == "g") || (TNLconsonant == "d") || (TNLconsonant == "n")) { vcLength = 33; }
                        else if (TNLconsonant == "h") {
                            vcLength = 15;
                        }
                        else if ((TNLconsonant == "ch") || (TNLconsonant == "gg")) { vcLength = totalDuration / 2; }
                        else if ((TNLconsonant == "k") || (TNLconsonant == "t") || (TNLconsonant == "p")  || (TNLconsonant == "dd") || (TNLconsonant == "bb") || (TNLconsonant == "ss") || (TNLconsonant == "jj")) { vcLength = totalDuration / 3; }
                        vcLength = Math.Min(totalDuration / 2, vcLength);

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
                }


                // 그 외(받침 없는 마지막 노트)
                if (singer.TryGetMappedOto(CV, note.tone + attr0.toneShift, attr0.voiceColor, out var oto)){
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

            if (prevHangeul) {
                string endBreath = "-";

                if (prevExist && TPLfinal == "" && endBreath.Contains(currentLyric)) {
                    endBreath = $"{TPLplainvowel} -";
                    prevIsBreath = true; // to prevent this→→ case→→, for example... "[사, -, 사 (=notes)]" should be "[- sa,  a -, - sa(=phonemes)]", but it becomes [sa, a -, 사(=phonemes)] in phonemizer, so '사' note becomes *no sound.
                }
                else if (prevExist && TPLfinal != "" && endBreath.Contains(currentLyric)) {
                    endBreath = $"{TPLplainfinal} -";
                    prevIsBreath = true; // to prevent this→→ case→→, for example... "[사, -, 사 (=notes)]" should be "[- sa,  a -, - sa(=phonemes)]", but it becomes [sa, a -, 사(=phonemes)] in phonemizer, so '사' note becomes *no sound.
                }

                if (singer.TryGetMappedOto(endBreath, note.tone + attr0.toneShift, attr0.voiceColor, out var oto)){
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
/**
            if (prevNeighbour == null) {
                // Use "- V" or "- CV" if present in voicebank
                var initial = $"- {currentLyric}";
                string[] tests = new string[] {initial, currentLyric};
                // try [- XX] before trying plain lyric
                if (checkOtoUntilHit(tests, note, out var oto)){
                    currentLyric = oto.Alias;
                }
            } else if ("-".Contains(currentLyric)) {
                var prevUnicode = ToUnicodeElements(prevNeighbour?.lyric);
                prevIsBreath = true;
                // end breath note
                if (vowelLookup.TryGetValue(prevUnicode.LastOrDefault() ?? string.Empty, out var vow)) {
                    var vowel = "";
                    var prevLyric = string.Join("", prevUnicode);;   
                    vowel = vow;
                    
                    var endBreath = $"{vow} -";
                    if (prevLyric.EndsWith("eo")) {
                        endBreath = $"eo -";
                    } else if (prevLyric.EndsWith("eu")) {
                        endBreath = $"eu -";
                    }
                                        
                    // try end breath
                    string[] tests = new string[] {endBreath, currentLyric};
                    if (checkOtoUntilHit(tests, note, out var oto)){ 
                        currentLyric = oto.Alias;
                    }
                }
            } else {
                string[] tests = new string[] {currentLyric};
                if (checkOtoUntilHit(tests, note, out var oto)){
                    currentLyric = oto.Alias;
                }
            }
**/
            if (nextNeighbour != null) { // 다음에 노트가 있으면
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

                    if (currentLyric.Contains("e")) {
                        vowel = "e" + vowel;
                        vowel = vowel.Replace("ee", "e");
                    }
                }

                // Get consonant from next note
                var consonant = "";
                if (consonantLookup.TryGetValue(nextUnicode.FirstOrDefault() ?? string.Empty, out var con)) {
                    consonant = getConsonant(nextNeighbour?.lyric); //로마자만 가능
                    if (!(isAlphaCon(consonant))) { consonant = con; }
                }

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
                var vcPhonemes = new string[] {vcPhoneme, ""};
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
                    vcLength = MsToTick(oto.Preutter);
                }
                vcLength = Math.Min(totalDuration / 2, vcLength);



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

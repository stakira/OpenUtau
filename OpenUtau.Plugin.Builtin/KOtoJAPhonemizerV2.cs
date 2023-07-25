using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;
using Serilog;
using WanaKanaNet;
using static OpenUtau.Api.Phonemizer;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Korean to Japanese Phonemizer (Version 2)", "KO to JA", "Lotte V", language: "KO")]

    public class KOtoJAPhonemizerV2 : Phonemizer {

        static readonly string[] naPlainVowels = new string[] { "a", "e", "a", "e", "o", "e", "o", "e", "o", "a", "e", "e", "o", "u", "o", "e", "i", "u", "u", "i", "i" };

        static readonly string[] naConsonants = new string[] {
            "ㄱ:g","ㄲ:k","ㄴ:n","ㄷ:d","ㄸ:t","ㄹ:r","ㅁ:m","ㅂ:b","ㅃ:p","ㅅ:s","ㅆ:s","ㅇ:","ㅈ:z","ㅉ:ts","ㅊ:ts","ㅋ:k","ㅌ:t","ㅍ:p","ㅎ:h"
        };
        static readonly string[] naVowels = new string[] {
            "ㅏ:a","ㅐ:e","ㅑ:ya","ㅒ:ye","ㅓ:o","ㅔ:e","ㅕ:yo","ㅖ:ye","ㅗ:o","ㅘ:wa","ㅙ:we","ㅚ:we","ㅛ:yo","ㅜ:u","ㅝ:wo","ㅞ:we","ㅟ:wi","ㅠ:yu","ㅡ:u","ㅢ:ui","ㅣ:i"
        };
        static readonly string[] naFinals = new string[] {
            ":","ㄱ:k","ㄲ:k","ㄳ:k","ㄴ:n","ㄵ:n","ㄶ:n","ㄷ:t","ㄹ:r","ㄺ:k","ㄻ:m","ㄼ:r","ㄽ:r","ㄾ:r","ㄿ:p","ㅀ:r","ㅁ:m","ㅂ:p","ㅄ:p","ㅅ:t","ㅆ:t","ㅇ:ng","ㅈ:t","ㅊ:t","ㅋ:k","ㅌ:t","ㅍ:p","ㅎ:t"
        };
        private const int hangeulStartIndex = 0xAC00;
        private const int hangeulEndIndex = 0xD7A3;

        // ======================================================================================


        static readonly string[] plainVowels = new string[] { "a", "i", "u", "e", "o", "wo" };

        static readonly string[] plainDiphthongs = new string[] { "ya", "yo", "yu", "ye", "wa", "ulo", "uli", "ule", "ui" };
        static readonly string[] vowels = new string[] {
            "a=a,ka,sa,ta,na,ha,ma,ya,ra,wa,ga,za,da,ba,pa,kya,sya,tya,nya,hya,rya,mya,bya,pya,telya,delya,kwa,sula,tsa,nula,fa,mula,rula,gwa,zula,bula,pula",
            "e=e,ke,se,te,ne,he,me,ye,re,we,ge,ze,de,be,pe,kye,sye,tye,nye,hye,rye,mye,bye,pye,tele,dele,ule,kwe,sule,tse,nule,fe,mule,rule,gwe,zule,bule,pule",
            "i=i,ki,si,ti,ni,hi,mi,ri,wi,gi,zi,di,bi,pi,teli,deli,uli,kwi,suli,tsi,nuli,fi,muli,ruli,gwi,zuli,buli,puli",
            "o=o,ko,so,to,no,ho,mo,yo,ro,wo,go,zo,do,bo,po,kyo,syo,tyo,nyo,hyo,ryo,myo,byo,pyo,telyo,delyo,ulo,kwo,sulo,tso,nulo,fo,mulo,rulo,gwo,zulo,bulo,pulo",
            "u=u,ku,su,tu,nu,hu,mu,yu,ru,gu,zu,du,bu,pu,kyu,syu,tyu,nyu,hyu,ryu,myu,byu,pyu,tolu,dolu,telyu,delyu",
            "n=n",
            "m=m",
            "r=r",
            "p=p",
            "t=t",
            "k=k"
        };

        static readonly string[] consonants = new string[] {
            "k=k,ka,ku,ke,ko,kwa,kwi,kwe,kwo",
            "ky=ky,ki,kye,kya,kyu,kyo",
            "s=s,sa,su,se,so,sula,suli,sule,sulo",
            "sh=sh,si,sye,sya,syu,syo",
            "t=t,ta,te,to,tolu",
            "ch=ch,chi,che,cha,chu,cho",
            "ts=ts,tsu,tsa,tsi,tse,tso",
            "ty=ty,teli,tele,telya,telyu,telyo",
            "n=n,na,nu,ne,no,nula,nuli,nule,nulo",
            "ny=ny,ni,nye,nya,nyu,nyo",
            "h=h,ha,he,ho",
            "hy=hy,hi,hye,hya,hyu,hyo",
            "f=f,fu,fa,fi,fe,fo",
            "m=m,ma,mu,me,mo,mula,muli,mule,mulo",
            "my=my,mi,mye,mya,myu,myo",
            "y=y,ye,ya,yu,yo",
            "r=r,ra,ru,re,ro,rula,ruli,rule,rulo",
            "ry=ry,ri,rye,rya,ryu,ryo",
            "g=g,ga,gu,ge,go,gwa,gwi,gwe,gwo",
            "gy=gy,gi,gye,gya,gyu,gyo",
            "z=z,za,zu,ze,zo,zula,zuli,zule,zulo",
            "j=j,zi,zye,zya,zyu,zyo",
            "d=d,da,de,do,dolu",
            "dy=dy,deli,dele,delya,delyu,delyo",
            "b=b,ba,bu,be,bo,bula,buli,bule,bulo",
            "by=by,bi,bye,bya,byu,byo",
            "p=p,pa,pu,pe,po,pula,puli,pule,pulo",
            "py=py,pi,pye,pya,pyu,pyo",
            "v=v,vu,va,vi,ve,vo"
            };

        // in case voicebank is missing certain symbols
        static readonly string[] substitution = new string[] {
            "ty,ch,ts=t", "j,dy=d", "gy=g", "ky=k", "py=p", "ny=n", "ry=r", "hy,f=h", "by,v=b", "dz=z", "l=r", "ly=l"
        };

        static readonly Dictionary<string, string> vowelLookup;
        static readonly Dictionary<string, string> consonantLookup;
        static readonly Dictionary<string, string> substituteLookup;

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

        bool isAlphaCon(string str) {
            if (str == "f") { return true; }
            else if (str == "v") { return true; }
            else if (str == "z") { return true; }
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
            else { return false; }
        }

        static KOtoJAPhonemizerV2() {
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
            substituteLookup = substitution.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[0].Split(',').Select(orig => (orig, parts[1]));
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

        private string ToHiragana(string romaji) {
            var hiragana = WanaKana.ToHiragana(romaji);
            hiragana = hiragana.Replace("ゔ", "ヴ");
            hiragana = hiragana.Replace("hわ", "ふぁ");
            hiragana = hiragana.Replace("hうぃ", "ふぃ");
            hiragana = hiragana.Replace("hうぇ", "ふぇ");
            hiragana = hiragana.Replace("hうぉ", "ふぉ");
            //hiragana = hiragana.Replace("bわ", "ぶぁ");
            //hiragana = hiragana.Replace("bうぃ", "ぶぃ");
            //hiragana = hiragana.Replace("bうぇ", "ぶぇ");
            //hiragana = hiragana.Replace("bうぉ", "ぶぉ");
            //hiragana = hiragana.Replace("pわ", "ぷぁ");
            //hiragana = hiragana.Replace("pうぃ", "ぷぃ");
            //hiragana = hiragana.Replace("pうぇ", "ぷぇ");
            //hiragana = hiragana.Replace("pうぉ", "ぷぉ");
            return hiragana;
        }

        private Dictionary<string, string> AltCv => altCv;
        private static readonly Dictionary<string, string> altCv = new Dictionary<string, string> {
            {"kui", "ki" },
            {"sui", "suli" },
            {"tui", "teli" },
            {"nui", "ni" },
            {"hui", "hi" },
            {"mui", "mi" },
            {"rui", "ri" },
            {"gui", "gi" },
            {"zui", "zuli" },
            {"dui", "deli" },
            {"bui", "bi" },
            {"pui", "pi" },
            {"fui", "fi" },
            {"vui", "vi" },
            {"thui", "thi" },
            {"rrui", "rri" },
            {"lui", "li" },
            {"kwa", "kula" },
            {"kwi", "kuli" },
            {"kwe", "kule" },
            {"kwo", "kulo" },
            {"gwa", "gula" },
            {"gwi", "guli" },
            {"gwe", "gule" },
            {"gwo", "gulo" },
            {"swa", "sula" },
            {"swi", "suli" },
            {"swe", "sule" },
            {"swo", "sulo" },
            {"zwa", "zula" },
            {"zwi", "zuli" },
            {"zwe", "zule" },
            {"zwo", "zulo" },
            {"tswa", "tsula" },
            {"tswi", "tsuli" },
            {"tswe", "tsule" },
            {"tswo", "tsulo" },
            {"tsi", "chi" },
            {"tsye", "che" },
            {"tsya", "cha" },
            {"tsyu", "chu" },
            {"tsyo", "cho" },
            {"ti", "teli" },
            {"tya", "telya" },
            {"tyu", "telyu" },
            {"tye", "tele" },
            {"tyo", "telyo" },
            {"tu", "tolu" },
            {"di", "deli" },
            {"dya", "delya" },
            {"dyu", "delyu" },
            {"dye", "dele" },
            {"dyo", "delyo" },
            {"du", "dolu" },
            {"nwa", "nula" },
            {"nwi", "nuli" },
            {"nwe", "nule" },
            {"nwo", "nulo" },
            {"bwa", "bula" },
            {"bwi", "buli" },
            {"bwe", "bule" },
            {"bwo", "bulo" },
            {"pwa", "pula" },
            {"pwi", "puli" },
            {"pwe", "pule" },
            {"pwo", "pulo" },
            {"mwa", "mula" },
            {"mwi", "muli" },
            {"mwe", "mule" },
            {"mwo", "mulo" },
            {"rwa", "rula" },
            {"rwi", "ruli" },
            {"rwe", "rule" },
            {"rwo", "rulo" },
            {"wi", "uli" },
            {"we", "ule" },
            {"wo", "ulo" },
        };

        private Dictionary<string, string> ConditionalAlt => conditionalAlt;
        private static readonly Dictionary<string, string> conditionalAlt = new Dictionary<string, string> {
            {"ui", "uli" },
            {"uli", "wi" },
            {"ule", "we" },
            {"ulo", "wo"},
            {"kye", "ke" },
            {"kula", "ka" },
            {"kuli", "ki" },
            {"kule", "ke"  },
            {"kulo", "ko"  },
            {"gye", "ge" },
            {"gula", "ga" },
            {"guli", "gi" },
            {"gule", "ge" },
            {"gulo", "go" },
            {"sye", "se" },
            {"sula", "sa" },
            {"suli", "shi" },
            {"sule", "se" },
            {"sulo", "so" },
            {"zye", "ze" },
            {"zula", "za" },
            {"zuli", "zi" },
            {"zule", "ze" },
            {"zulo", "zo" },
            {"teli", "ti" },
            {"telya", "tya" },
            {"telyu", "tyu" },
            {"tele", "te" },
            {"telyo", "tyo" },
            {"tolu", "tsu" },
            {"tsye", "che" },
            {"tsula", "cha" },
            {"tsuli", "chi" },
            {"tsule", "tse" },
            {"tsulo", "cho" },
            {"deli", "ji" },
            {"delya", "ja" },
            {"delyu", "ju" },
            {"dele", "de" },
            {"delyo", "jo" },
            {"dolu", "zu" },
            {"nye", "ne" },
            {"nula", "na" },
            {"nuli", "ni" },
            {"nule", "ne" },
            {"nulo", "no" },
            {"hye", "he" },
            {"fa", "ha" },
            {"fi", "hi" },
            {"fe", "he" },
            {"fo", "ho" },
            {"bye", "be" },
            {"bula", "ba" },
            {"buli", "bi" },
            {"bule", "be" },
            {"bulo", "bo" },
            {"pye", "pe" },
            {"pula", "pa" },
            {"puli", "pi"},
            {"pule", "pe"},
            {"pulo", "po" },
            {"mye", "me" },
            {"mula", "ma" },
            {"muli", "mi" },
            {"mule", "me" },
            {"mulo", "mo" },
            {"ye", "e" },
            {"rye", "re" },
            {"rula", "ra" },
            {"ruli", "ri" },
            {"rule", "re" },
            {"rulo", "ro" },
        };

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            var note = notes[0];
            var currentUnicode = ToUnicodeElements(note.lyric); // 현재 가사의 유니코드
            string currentLyric = note.lyric; // 현재 가사
            var attr0 = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
            var attr1 = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 1) ?? default;

            // 가사의 초성, 중성, 종성 분리
            // P(re)Lconsonant, PLvowel, PLfinal / C(urrent)Lconsonant, CLvowel, CLfinal / N(ext)Lconsonant, NLvowel, NLfinal

            int CLconsonant = 0;
            int CLvowel = 0;
            int CLfinal = 0; // 현재 노트의 consonant, vowel, final 인덱스
            string[] TCLtemp;
            string TCLconsonant = "";
            string TCLvowel = "";
            string TCLfinal = "";
            string TCLplainvowel = ""; // 현재 노트의 consonant, vowel, fina, 모음의 단순화

            int NLconsonant = 0;
            int NLvowel = 0;
            int NLfinal = 0;
            string[] TNLtemp;
            string TNLconsonant = "";
            string TNLvowel = "";
            string TNLfinal = "";
            string TNLplainvowel = "";

            int PLconsonant = 0;
            int PLvowel = 0;
            int PLfinal = 0;
            string[] TPLtemp;
            string TPLconsonant = "";
            string TPLvowel = "";
            string TPLfinal = "";
            string TPLplainvowel = "";

            bool currentHangeul = false;
            bool prevHangeul = false;
            bool nextHangeul = false;

            bool prevExist = false;
            bool nextExist = false;

            char firstCL, firstPL, firstNL;
            int lCL, lPL, lNL;
            int uCL, uPL, uNL;
            lCL = 0; lPL = 0; lNL = 0;


            // 현재 노트 첫번째 글자 확인
            firstCL = currentLyric[0];
            if (firstCL == 'ㄹ') { lCL = 1; firstCL = currentLyric[1]; }
            uCL = (int) firstCL;
            if ((uCL >= hangeulStartIndex) && (uCL <= hangeulEndIndex)) {
                currentHangeul = true;
                CLconsonant = (uCL - hangeulStartIndex) / (21 * 28);
                CLvowel = (uCL - hangeulStartIndex) % (21 * 28) / 28;
                CLfinal = (uCL - hangeulStartIndex) % 28;


                TCLtemp = naConsonants[CLconsonant].Split(":");
                TCLconsonant = TCLtemp[1];

                TCLtemp = naVowels[CLvowel].Split(":");
                TCLvowel = TCLtemp[1];
                TCLplainvowel = naPlainVowels[CLvowel];

                TCLtemp = naFinals[CLfinal].Split(":");
                TCLfinal = TCLtemp[1];

                // TCLconsonant : 현노트 초성    TCLvowel : 현노트 중성    TCLfinal : 현노트 종성

            }

            // 이전 노트 존재 여부 확인 + 이전 노트 첫번째 글자 확인
            if (prevNeighbour != null) {
                firstPL = (prevNeighbour?.lyric)[0]; // 가사 받아오기
                prevExist = true; // 이전 노트 존재한다 반짝
                if (firstPL == 'ㄹ') { lPL = 1; firstPL = (prevNeighbour?.lyric)[1]; } // ㄹㄹ 발음이다 반짝
                uPL = (int) firstPL; // 가사를 int로 변환

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

                    TPLtemp = naFinals[PLfinal].Split(":");
                    TPLfinal = TPLtemp[1];
                }
            }

            // 다음 노트 존재 여부 확인 + 다음 노트 첫번째 글자 확인
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


                    TNLtemp = naConsonants[NLconsonant].Split(":");
                    TNLconsonant = TNLtemp[1];

                    TNLtemp = naVowels[NLvowel].Split(":");
                    TNLvowel = TNLtemp[1];
                    TNLplainvowel = naPlainVowels[NLvowel];

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
                        if (temp == 1) { TCLtemp = naConsonants[0].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 2) { TCLtemp = naConsonants[1].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 3) { TCLtemp = naConsonants[10].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 4) { TCLtemp = naConsonants[2].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 5) { TCLtemp = naConsonants[12].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 6) { TCLtemp = naConsonants[18].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 7) { TCLtemp = naConsonants[3].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 8) { TCLtemp = naConsonants[5].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 9) { TCLtemp = naConsonants[0].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 10) { TCLtemp = naConsonants[6].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 11) { TCLtemp = naConsonants[7].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 12) { TCLtemp = naConsonants[9].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 13) { TCLtemp = naConsonants[16].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 14) { TCLtemp = naConsonants[17].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 15) { TCLtemp = naConsonants[18].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 16) { TCLtemp = naConsonants[6].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 17) { TCLtemp = naConsonants[7].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 18) { TCLtemp = naConsonants[9].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 19) { TCLtemp = naConsonants[9].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 20) { TCLtemp = naConsonants[10].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 21) { tempTCLconsonant = ""; yeoneum = true; } else if (temp == 22) { TCLtemp = naConsonants[12].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 23) { TCLtemp = naConsonants[14].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 24) { TCLtemp = naConsonants[15].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 25) { TCLtemp = naConsonants[16].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 26) { TCLtemp = naConsonants[17].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; } else if (temp == 27) { TCLtemp = naConsonants[18].Split(":"); tempTCLconsonant = TCLtemp[1]; yeoneum = true; }
                    }

                    if (nextExist && nextHangeul && (TCLfinal != "") && (TNLconsonant == "")) {
                        int temp = CLfinal;

                        if (temp == 1) { TCLtemp = naConsonants[0].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; } else if (temp == 2) { TCLtemp = naConsonants[1].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; } else if (temp == 3) { TCLfinal = "k"; yeoneum2 = true; } else if (temp == 4) { TCLtemp = naConsonants[2].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; } else if (temp == 5) { TCLfinal = "n"; yeoneum2 = true; } else if (temp == 6) { TCLfinal = "n"; yeoneum2 = true; } else if (temp == 7) { TCLtemp = naConsonants[3].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; } else if (temp == 8) { TCLtemp = naConsonants[5].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; } else if (temp == 9) { TCLfinal = "r"; yeoneum2 = true; } else if (temp == 10) { TCLfinal = "r"; yeoneum2 = true; } else if (temp == 11) { TCLfinal = "r"; yeoneum2 = true; } else if (temp == 12) { TCLfinal = "r"; yeoneum2 = true; } else if (temp == 13) { TCLfinal = "r"; yeoneum2 = true; } else if (temp == 14) { TCLfinal = "r"; yeoneum2 = true; } else if (temp == 15) { TCLfinal = "r"; yeoneum2 = true; } else if (temp == 16) { TCLtemp = naConsonants[6].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; } else if (temp == 17) { TCLtemp = naConsonants[7].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; } else if (temp == 18) { TCLfinal = "p"; yeoneum2 = true; } else if (temp == 19) { TCLtemp = naConsonants[9].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; } else if (temp == 20) { TCLtemp = naConsonants[10].Split(":"); tempTCLfinal = TCLtemp[1]; TCLfinal = ""; yeoneum2 = true; }
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
                        if ((NLconsonant == 0) && (CLfinal == 27)) { TCLfinal = ""; TNLconsonant = "k"; } else if ((NLconsonant == 0) && (CLfinal == 6)) { TCLfinal = "n"; TNLconsonant = "k"; } else if ((NLconsonant == 0) && (CLfinal == 15)) { TCLfinal = "r"; TNLconsonant = "k"; } else if ((NLconsonant == 3) && (CLfinal == 27)) { TCLfinal = ""; TNLconsonant = "t"; } else if ((NLconsonant == 3) && (CLfinal == 6)) { TCLfinal = "n"; TNLconsonant = "t"; } else if ((NLconsonant == 3) && (CLfinal == 15)) { TCLfinal = "r"; TNLconsonant = "t"; } else if ((NLconsonant == 12) && (CLfinal == 27)) { TCLfinal = ""; TNLconsonant = "ch"; } else if ((NLconsonant == 12) && (CLfinal == 6)) { TCLfinal = "n"; TNLconsonant = "ch"; } else if ((NLconsonant == 12) && (CLfinal == 15)) { TCLfinal = "r"; TNLconsonant = "ch"; } else if ((NLconsonant == 9) && (CLfinal == 27)) { TCLfinal = ""; TNLconsonant = "ss"; } else if ((NLconsonant == 9) && (CLfinal == 6)) { TCLfinal = "n"; TNLconsonant = "ss"; } else if ((NLconsonant == 9) && (CLfinal == 15)) { TCLfinal = "r"; TNLconsonant = "ss"; }

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
                        if ((CLfinal == 9) && (NLconsonant == 0)) { TCLfinal = "r"; }
                    }


                    // 4. 경음화/된소리되기
                    if (prevExist && prevHangeul && TPLfinal != "") {
                        // ㄱㄷㅂ + ㄱㄷㅂㅅㅈ = ㄲㄸㅃㅆㅉ
                        if (((TPLfinal == "k") && (CLconsonant == 0)) || ((TPLfinal == "t") && (CLconsonant == 0)) || ((TPLfinal == "p") && (CLconsonant == 0))) { TCLconsonant = "gg"; } else if (((TPLfinal == "k") && (CLconsonant == 3)) || ((TPLfinal == "t") && (CLconsonant == 3)) || ((TPLfinal == "p") && (CLconsonant == 3))) { TCLconsonant = "dd"; } else if (((TPLfinal == "k") && (CLconsonant == 7)) || ((TPLfinal == "t") && (CLconsonant == 7)) || ((TPLfinal == "p") && (CLconsonant == 7))) { TCLconsonant = "bb"; } else if (((TPLfinal == "k") && (CLconsonant == 9)) || ((TPLfinal == "t") && (CLconsonant == 9)) || ((TPLfinal == "p") && (CLconsonant == 9))) { TCLconsonant = "ss"; } else if (((TPLfinal == "k") && (CLconsonant == 12)) || ((TPLfinal == "t") && (CLconsonant == 12)) || ((TPLfinal == "p") && (CLconsonant == 12))) { TCLconsonant = "jj"; }

                        /* 
                        // 용언 어간 받침 ㄴㅁ + ㄱㄷㅅㅈ = ㄲㄸㅆㅉ
                        if(((TPLfinal=="n")&&(CLconsonant==0))|| ((TPLfinal == "m") && (CLconsonant == 0))) { TCLconsonant = "gg"; }
                        else if (((TPLfinal == "n") && (CLconsonant == 3)) || ((TPLfinal == "m") && (CLconsonant == 3))) { TCLconsonant = "dd"; }
                        else if (((TPLfinal == "n") && (CLconsonant == 9)) || ((TPLfinal == "m") && (CLconsonant == 9))) { TCLconsonant = "ss"; }
                        else if (((TPLfinal == "n") && (CLconsonant == 12)) || ((TPLfinal == "m") && (CLconsonant == 12))) { TCLconsonant = "jj"; }
                        */

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
                    if (prevExist && prevHangeul && (TPLfinal != "")) {
                        // 한자어 받침 ㅁㅇ + ㄹ = ㄴ
                        if (((TPLfinal == "m") && (CLconsonant == 5)) || ((TPLfinal == "n") && (CLconsonant == 5))) { TCLconsonant = "n"; }

                        // 한자어 받침 ㄱㄷㅂ + ㄹ = ㅇㄴㅁ + ㄴ(1)
                        if (((TPLfinal == "k") && (CLconsonant == 5)) || ((TPLfinal == "t") && (CLconsonant == 5)) || ((TPLfinal == "p") && (CLconsonant == 5))) { TCLconsonant = "n"; }
                    }
                    if (nextExist && nextHangeul && (TCLfinal != "")) {
                        //받침 ㄱㄷㅂ + ㄴㅁ = ㅇㄴㅁ
                        if (((TCLfinal == "k") && (TNLconsonant == "n")) || ((TCLfinal == "k") && (TNLconsonant == "m"))) { TCLfinal = "n"; } else if (((TCLfinal == "t") && (TNLconsonant == "n")) || ((TCLfinal == "t") && (TNLconsonant == "m"))) { TCLfinal = "n"; } else if (((TCLfinal == "p") && (TNLconsonant == "n")) || ((TCLfinal == "p") && (TNLconsonant == "m"))) { TCLfinal = "m"; }

                        // 한자어 받침 ㄱㄷㅂ + ㄹ = ㅇㄴㅁ + ㄴ(2)
                        if ((TCLfinal == "k") && (NLconsonant == 5)) { TCLfinal = "n"; } else if ((TCLfinal == "t") && (NLconsonant == 5)) { TCLfinal = "n"; } else if ((TCLfinal == "p") && (NLconsonant == 5)) { TCLfinal = "m"; }
                    }


                    // 7. 유음화
                    if (prevExist && prevHangeul && (TPLfinal != "")) {
                        if (((PLfinal == 8) && (TCLconsonant == "n")) || ((PLfinal == 13) && (TCLconsonant == "n")) || ((PLfinal == 15) && (TCLconsonant == "n"))) { TCLconsonant = "r"; }
                    }
                    if (nextExist && nextHangeul && (TCLfinal != "")) {
                        if ((TCLfinal == "n") && (TNLconsonant == "r")) { TCLfinal = "r"; }
                    }



                    // 8. 받침 + ㄹ = ㄹㄹ
                    if (prevExist && prevHangeul && (TPLfinal != "")) { lCL = 1; }



                    // consonant에 변경 사항이 있을 때
                    if (prevExist && prevHangeul) {


                        // 비음화
                        // (1) ㄱ(ㄲㅋㄳㄺ)
                        //     ㄷ(ㅅ,ㅆ,ㅈ,ㅊ,ㅌ,ㅎ)
                        //     ㅂ(ㅍ,ㄼ,ㄿ,ㅄ)


                    }
                    // final에 변경 사항이 있을 때


                }

                string CV = ToHiragana(TCLconsonant + TCLvowel);
                string VC = "";

                if (nextExist && (TCLfinal == "") && nextHangeul && (TNLvowel.Contains("y") || TNLvowel.Contains("i"))) {
                    string[] tests = new string[] { $"{TCLplainvowel} {TNLconsonant}y", $"{TCLplainvowel} {TNLconsonant}", VC, currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        VC = oto.Alias;
                    }
                } else {
                    VC = TCLplainvowel + " " + TNLconsonant;
                }

                string FC = "";
                if (TCLfinal != "") {
                    string[] tests = new string[] { $"{TCLplainvowel} {TCLfinal}", $"{TCLplainvowel} {ToHiragana(TCLfinal)}", FC, currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        FC = oto.Alias;
                    }
                }

                    //if (lCL == 1) { CV = CV.Replace("r", "r"); }

                    // 만약 앞에 노트가 없다면
                    //if (!prevExist && TCLconsonant == "r") {
                    //    string[] tests = new string[] { $"- {CV}", $"l{TCLvowel}", CV, currentLyric };
                    //    if (checkOtoUntilHit(tests, note, out var oto)) {
                    //        CV = oto.Alias;
                    //    }
                    //} else
                    if (!prevExist) {
                    string[] tests = new string[] { $"- {CV}", CV, currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        CV = oto.Alias;
                    }
                }

                if (prevExist && VC == "" && TPLfinal == "") {
                    string[] tests = new string[] { $"{TPLplainvowel} {CV}", $"* {CV}", CV, currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        CV = oto.Alias;
                    }
                }

                //if (prevExist && TCLconsonant == "" && TPLfinal == "ng") {
                //    string[] tests = new string[] { $"ng{CV}", CV, currentLyric };
                //    if (checkOtoUntilHit(tests, note, out var oto)) {
                //        CV = oto.Alias;
                //    }
                //}

                if (nextNeighbour != null) { // 다음에 노트가 있으면
                    var nextUnicode = ToUnicodeElements(nextNeighbour?.lyric);
                    var nextLyric = string.Join("", nextUnicode);

                    // Insert VC before next neighbor
                    // Get vowel from current note
                    var vowel = TCLplainvowel;

                    // Get consonant from next note
                    var consonant = "";
                    if (consonantLookup.TryGetValue(nextUnicode.FirstOrDefault() ?? string.Empty, out var con)) {
                        consonant = getConsonant(nextNeighbour?.lyric); //로마자만 가능
                        if ((!isAlphaCon(consonant))) { consonant = con; }
                    } else if (nextExist && nextHangeul) {
                        consonant = TNLconsonant;
                    } else if (nextLyric.StartsWith("ch")) { consonant = "ch"; }

                    //var vcPhoneme = $"{TCLplainvowel} {consonant}";

                    if (!nextHangeul) {
                        VC = TCLplainvowel + " " + consonant;
                    }

                    var vcPhonemes = new string[] { VC, "" };
                    // find potential substitute symbol
                    if (substituteLookup.TryGetValue(consonant ?? string.Empty, out con)) {
                        vcPhonemes[1] = $"{TCLplainvowel} {con}";
                    }
                }

                if (prevNeighbour != null && prevExist && !prevHangeul && TCLconsonant == "" && VC == "") {
                    var prevUnicode = ToUnicodeElements(prevNeighbour?.lyric);
                    var vowel = "";

                    var prevLyric = string.Join("", prevUnicode);

                    // Current note is VV
                    if (vowelLookup.TryGetValue(prevUnicode.LastOrDefault() ?? string.Empty, out var vow)) {
                        vowel = vow;

                        var mixVV = $"{vow} {ToHiragana(CV)}";

                        //if (prevLyric.EndsWith("eo")) {
                        //    mixVV = $"eo {CV}";
                        //} else if (prevLyric.EndsWith("eu")) {
                        //    mixVV = $"eu {CV}";
                        //} else if (prevLyric.EndsWith("er")) {
                        //    mixVV = $"er {CV}";
                        //}

                        // try vowlyric then currentlyric
                        string[] tests = new string[] { mixVV, ToHiragana(CV), currentLyric };
                        if (checkOtoUntilHit(tests, note, out var oto)) {
                            CV = oto.Alias;
                        }
                    }

                }

                // 만약 받침이 있다면
                if (FC != "") {
                    int totalDuration = notes.Sum(n => n.duration);
                    int fcLength = totalDuration / 3;
                    if ((TCLfinal == "k") || (TCLfinal == "p") || (TCLfinal == "t")) { fcLength = totalDuration / 2; }

                    if (singer.TryGetMappedOto(ToHiragana(CV), note.tone + attr0.toneShift, attr0.voiceColor, out var oto1) && singer.TryGetMappedOto(FC, note.tone + attr0.toneShift, attr0.voiceColor, out var oto2)) {
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
                    if (nextExist) { if ((nextNeighbour?.lyric)[0] == 'ㄹ') { VC = ""; } }
                    if ((VC != "") && (TNLconsonant != "")) {
                        int totalDuration = notes.Sum(n => n.duration);
                        int vcLength = 60;
                        if ((TNLconsonant == "r") || (TNLconsonant == "h")) { vcLength = 30; } else if (TNLconsonant == "s") { vcLength = totalDuration / 3; } else if ((TNLconsonant == "k") || (TNLconsonant == "t") || (TNLconsonant == "p") || (TNLconsonant == "ch")) { vcLength = totalDuration / 2; } else if ((TNLconsonant == "gg") || (TNLconsonant == "dd") || (TNLconsonant == "bb") || (TNLconsonant == "ss") || (TNLconsonant == "jj")) { vcLength = totalDuration / 2; }
                        vcLength = Math.Min(totalDuration / 2, vcLength);

                        if (singer.TryGetMappedOto(ToHiragana(CV), note.tone + attr0.toneShift, attr0.voiceColor, out var oto1) && singer.TryGetMappedOto(VC, note.tone + attr0.toneShift, attr0.voiceColor, out var oto2)) {
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
                    } else if (VC != "" && TNLconsonant == "") {
                        int totalDuration = notes.Sum(n => n.duration);
                        int vcLength = 60;
                        var nextAttr = nextNeighbour.Value.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
                        var nextUnicode = ToUnicodeElements(nextNeighbour?.lyric);
                        var nextLyric = string.Join("", nextUnicode);
                        if (singer.TryGetMappedOto(nextLyric, nextNeighbour.Value.tone + nextAttr.toneShift, nextAttr.voiceColor, out var oto0)) {
                            vcLength = MsToTick(oto0.Preutter);

                        }
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
                    // 그 외(받침 없는 마지막 노트)
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
                string endVow = "R";
                string endVow2 = "-";
                string endBreath = "息";
                string endBreath2 = "吸";


                if (prevExist && TPLfinal == "" && endVow.Contains(currentLyric)) {
                    endVow = $"{TPLplainvowel} R";
                } else if (prevExist && TPLfinal == "" && endVow2.Contains(currentLyric)) {
                    endVow2 = $"{TPLplainvowel} -";
                } else if (prevExist && TPLfinal == "" && endBreath.Contains(currentLyric)) {
                    endBreath = $"{TPLplainvowel} 息";
                } else if (prevExist && TPLfinal == "" && endBreath2.Contains(currentLyric)) {
                    endBreath2 = $"{TPLplainvowel} 吸";
                }

                if (singer.TryGetMappedOto(endVow, note.tone + attr0.toneShift, attr0.voiceColor, out var oto)) {
                    endVow = oto.Alias;
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme() {
                                phoneme = endVow,
                            }
                        },
                    };
                } else if (singer.TryGetMappedOto(endVow2, note.tone + attr0.toneShift, attr0.voiceColor, out var oto1)) {
                    endVow2 = oto1.Alias;
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme() {
                                phoneme = endVow,
                            }
                        },
                    };
                } else if (singer.TryGetMappedOto(endBreath, note.tone + attr0.toneShift, attr0.voiceColor, out var oto2)) {
                    endBreath = oto2.Alias;
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme() {
                                phoneme = endBreath,
                            }
                        },
                    };
                } else if (singer.TryGetMappedOto(endBreath2, note.tone + attr0.toneShift, attr0.voiceColor, out var oto3)) {
                    endBreath = oto3.Alias;
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

            if (nextNeighbour != null) { // 다음에 노트가 있으면
                var nextUnicode = ToUnicodeElements(nextNeighbour?.lyric);
                var nextLyric = string.Join("", nextUnicode);

                // Check if next note is a vowel and does not require VC
                if (vowels.Contains(nextUnicode.FirstOrDefault() ?? string.Empty)) {
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
                }

                // Get consonant from next note
                var consonant = "";
                if (consonantLookup.TryGetValue(nextUnicode.FirstOrDefault() ?? string.Empty, out var con)) {
                    consonant = getConsonant(nextNeighbour?.lyric); //로마자만 가능
                    if ((!isAlphaCon(consonant))) { consonant = con; }
                } else if (nextExist && nextHangeul) {
                    consonant = TNLconsonant;
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
                var vcPhonemes = new string[] { vcPhoneme, "" };
                // find potential substitute symbol
                if (substituteLookup.TryGetValue(consonant ?? string.Empty, out con)) {
                    vcPhonemes[1] = $"{vowel} {con}";
                }
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
                } else if ((TNLconsonant == "r") || (TNLconsonant == "h")) { vcLength = 30; } else if (TNLconsonant == "s") { vcLength = totalDuration / 3; } else if ((TNLconsonant == "k") || (TNLconsonant == "t") || (TNLconsonant == "p") || (TNLconsonant == "ch")) { vcLength = totalDuration / 2; } else if ((TNLconsonant == "gg") || (TNLconsonant == "dd") || (TNLconsonant == "bb") || (TNLconsonant == "ss") || (TNLconsonant == "jj")) { vcLength = totalDuration / 2; }
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

            var nextUnicode2 = ToUnicodeElements(nextNeighbour?.lyric);
            var nextLyric2 = string.Join("", nextUnicode2);
            if (prevNeighbour == null) {
                // Use "- V" or "- CV" if present in voicebank
                var initial = $"- {ToHiragana(currentLyric)}";
                string[] tests = new string[] { initial, ToHiragana(currentLyric), currentLyric };
                // try [- XX] before trying plain lyric
                if (checkOtoUntilHit(tests, note, out var oto)) {
                    currentLyric = oto.Alias;
                }
            } else if (plainVowels.Contains(ToHiragana(currentLyric))) {
                var prevUnicode = ToUnicodeElements(prevNeighbour?.lyric);
                var vowel = "";

                var prevLyric = string.Join("", prevUnicode);
                // Current note is VV
                if (vowelLookup.TryGetValue(prevUnicode.LastOrDefault() ?? string.Empty, out var vow)) {
                    vowel = vow;

                    var vowLyric = $"{vow} {ToHiragana(currentLyric)}";

                    //if (prevLyric.EndsWith("eo")) {
                    //    vowLyric = $"eo {currentLyric}";
                    //} else if (prevLyric.EndsWith("eu")) {
                    //    vowLyric = $"eu {currentLyric}";
                    //} else if (prevLyric.EndsWith("er")) {
                    //    vowLyric = $"er {currentLyric}";
                    //}

                    // try vowlyric then currentlyric
                    string[] tests = new string[] { vowLyric, ToHiragana(currentLyric), currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        currentLyric = oto.Alias;
                    }
                } else if (prevExist && prevHangeul && TPLfinal == "") {
                    var vowLyric = $"{TPLplainvowel} {ToHiragana(currentLyric)}";

                    string[] tests = new string[] { vowLyric, $"* {ToHiragana(currentLyric)}", ToHiragana(currentLyric), currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        currentLyric = oto.Alias;
                    }
                //} else if (prevExist && prevHangeul && TPLfinal == "ng") {
                //    string[] tests = new string[] { $"ng{currentLyric}", currentLyric };
                //    if (checkOtoUntilHit(tests, note, out var oto)) {
                //        currentLyric = oto.Alias;
                //    }
                } else {
                    string[] tests = new string[] { $"* {ToHiragana(currentLyric)}", ToHiragana(currentLyric), currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        currentLyric = oto.Alias;
                    }
                }
            } else if ("R".Contains(currentLyric) || "-".Contains(currentLyric) || "息".Contains(currentLyric) || "吸".Contains(currentLyric)) {
                var prevUnicode = ToUnicodeElements(prevNeighbour?.lyric);
                // end breath note
                if (vowelLookup.TryGetValue(prevUnicode.LastOrDefault() ?? string.Empty, out var vow)) {
                    var vowel = "";
                    var prevLyric = string.Join("", prevUnicode);
                    vowel = vow;

                    var endVow = $"{vow} {currentLyric}";
                    //if (prevLyric.EndsWith("eo")) {
                    //    endBreath = $"eo R";
                    //} else if (prevLyric.EndsWith("eu")) {
                    //    endBreath = $"eu R";
                    //} else if (prevLyric.EndsWith("er")) {
                    //    endBreath = $"er R";
                    //}

                    // try end breath
                    string[] tests = new string[] { endVow, currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        currentLyric = oto.Alias;
                    }
                }
            } else {
                string[] tests = new string[] { $"* {ToHiragana(currentLyric)}", ToHiragana(currentLyric), currentLyric };
                if (checkOtoUntilHit(tests, note, out var oto)) {
                    currentLyric = oto.Alias;
                }
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

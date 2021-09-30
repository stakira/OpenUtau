using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("KoreanCVCPhonemizer", "KO CVC", "NANA")]

    public class KoreanCVCPhonemizer : Phonemizer {
   
        static readonly string[] naPlainVowels = new string[] { "a", "e", "a", "e", "eo", "e", "eo", "e", "o", "a", "e", "e", "o", "u", "eo", "e", "i", "u", "eu", "i", "i" };

        static readonly string[] naConsonants = new string[] {
            "ㄱ:g","ㄲ:gg","ㄴ:n","ㄷ:d","ㄸ:dd","ㄹ:r","ㅁ:m","ㅂ:b","ㅃ:bb","ㅅ:s","ㅆ:ss","ㅇ:","ㅈ:j","ㅉ:jj","ㅊ:ch","ㅋ:k","ㅌ:t","ㅍ:p","ㅎ:h"
        };
        static readonly string[] naVowels = new string[] {
            "ㅏ:a","ㅐ:e","ㅑ:ya","ㅒ:ye","ㅓ:eo","ㅔ:e","ㅕ:yeo","ㅖ:ye","ㅗ:o","ㅘ:wa","ㅙ:we","ㅚ:we","ㅛ:yo","ㅜ:u","ㅝ:weo","ㅞ:we","ㅟ:wi","ㅠ:yu","ㅡ:eu","ㅢ:eui","ㅣ:i"
        };
        static readonly string[] naFinals = new string[] {
            ":","ㄱ:k","ㄲ:k","ㄳ:k","ㄴ:n","ㄵ:n","ㄶ:n","ㄷ:t","ㄹ:l","ㄺ:l","ㄻ:l","ㄼ:l","ㄽ:l","ㄾ:l","ㄿ:l","ㅀ:l","ㅁ:m","ㅂ:p","ㅄ:p","ㅅ:t","ㅆ:t","ㅇ:ng","ㅈ:t","ㅊ:t","ㅋ:t","ㅌ:t","ㅍ:p","ㅎ:t"
        };
        private const int hangeulStartIndex = 0xAC00;
        private const int hangeulEndIndex = 0xD7A3;

        // ======================================================================================

        static readonly string[] plainVowels = new string[] { "eu", "eo", "a", "i", "u", "e", "o" };

        static readonly string[] vowels = new string[] {
            "eu=geu,neu,deu,reu,leu,meu,beu,seu,eu,jeu,cheu,keu,teu,peu,heu,ggeu,ddeu,bbeu,sseu,jjeu",
            "eo=geo,neo,deo,reo,leo,meo,beo,seo,eo,jeo,cheo,keo,teo,peo,heo,ggeo,ddeo,bbeo,sseo,jjeo,gyeo,nyeo,dyeo,ryeo,lyeo,myeo,byeo,syeo,yeo,jyeo,chyeo,kyeo,tyeo,pyeo,hyeo,ggyeo,ddyeo,bbyeo,ssyeo,jjyeo,gweo,nweo,dweo,rweo,lweo,mweo,bweo,sweo,weo,jweo,chweo,kweo,tweo,pweo,hweo,ggweo,ddweo,bbweo,ssweo,jjweo",
            "a=ga,na,da,ra,la,ma,ba,sa,a,ja,cha,ka,ta,pa,ha,gga,dda,bba,ssa,jja,gya,nya,dya,rya,lya,mya,bya,sya,ya,jya,chya,kya,tya,pya,hya,ggya,ddya,bbya,ssya,jjya,gwa,nwa,dwa,rwa,lwa,mwa,bwa,swa,wa,jwa,chwa,kwa,twa,pwa,hwa,ggwa,ddwa,bbwa,sswa,jjwa",
            "e=ge,ne,de,re,le,me,be,se,e,je,che,ke,te,pe,he,gge,dde,bbe,sse,jje,gye,nye,dye,rye,lye,mye,bye,sye,ye,jye,chye,kye,tye,pye,hye,ggye,ddye,bbye,ssye,jjye,gwe,nwe,dwe,rwe,lwe,mwe,bwe,swe,we,jwe,chwe,kwe,twe,pwe,hwe,ggwe,ddwe,bbwe,sswe,jjwe",
            "i=gi,ni,di,ri,li,mi,bi,si,i,ji,chi,ki,ti,pi,hi,ggi,ddi,bbi,ssi,jji,gwi,nwi,dwi,rwi,lwi,mwi,bwi,swi,wi,jwi,chwi,kwi,twi,pwi,hwi,ggwi,ddwi,bbwi,sswi,jjwi",
            "o=go,no,do,ro,lo,mo,bo,so,o,jo,cho,ko,to,po,ho,ggo,ddo,bbo,sso,jjo,gyo,nyo,dyo,ryo,lyo,myo,byo,syo,yo,jyo,chyo,kyo,tyo,pyo,hyo,ggyo,ddyo,bbyo,ssyo,jjyo",
            "u=gu,nu,du,ru,lu,mu,bu,su,u,ju,chu,ku,tu,pu,hu,ggu,ddu,bbu,ssu,jju,gyu,nyu,dyu,ryu,lyu,myu,byu,syu,yu,jyu,chyu,kyu,tyu,pyu,hyu,ggyu,ddyu,bbyu,ssyu,jjyu",
            "ng=ang,ing,ung,eng,ong,eung,eong",
            "n=an,in,un,en,on,eun,eon",
            "m=am,im,um,em,om,eum,eom",
            "l=al,il,ul,el,ol,eul,eol",
            "p=ap,ip,up,ep,op,eup,eop",
            "t=at,it,ut,et,ot,eut,eot",
            "k=ak,ik,uk,ek,ok,euk,eok"
        };

        static readonly string[] consonants = new string[] {
            "gg=gg,gga,ggi,ggu,gge,ggo,ggeu,ggeo,ggya,ggyu,ggye,ggyo,ggyeo,ggwa,ggwi,ggwe,ggweo",
            "dd=dd,dda,ddi,ddu,dde,ddo,ddeu,ddeo,ddya,ddyu,ddye,ddyo,ddyeo,ddwa,ddwi,ddwe,ddweo",
            "bb=bb,bba,bbi,bbu,bbe,bbo,bbeu,bbeo,bbya,bbyu,bbye,bbyo,bbyeo,bbwa,bbwi,bbwe,bbweo",
            "ss=ss,ssa,ssi,ssu,sse,sso,sseu,sseo,ssya,ssyu,ssye,ssyo,ssyeo,sswa,sswi,sswe,ssweo",
            "g=g,ga,gi,gu,ge,go,geu,geo,gya,gyu,gye,gyo,gyeo,gwa,gwi,gwe,gweo",
            "n=n,na,ni,nu,ne,no,neu,neo,nya,nyu,nye,nyo,nyeo,nwa,nwi,nwe,nweo",
            "d=d,da,di,du,de,do,deu,deo,dya,dyu,dye,dyo,dyeo,dwa,dwi,dwe,dweo",
            "r=r,ra,ri,ru,re,ro,reu,reo,rya,ryu,rye,ryo,ryeo,rwa,rwi,rwe,rweo",
            "m=m,ma,mi,mu,me,mo,meu,meo,mya,myu,mye,myo,myeo,mwa,mwi,mwe,mweo",
            "b=b,ba,bi,bu,be,bo,beu,beo,bya,byu,bye,byo,byeo,bwa,bwi,bwe,bweo",
            "s=s,sa,si,su,se,so,seu,seo,sya,syu,sye,syo,syeo,swa,swi,swe,sweo",
            "j=j,ja,ji,ju,je,jo,jeu,jeo,jya,jyu,jye,jyo,jyeo,jwa,jwi,jwe,jweo",
            "ch=ch,cha,chi,chu,che,cho,cheu,cheo,chya,chyu,chye,chyo,chyeo,chwa,chwi,chwe,chweo",
            "k=k,ka,ki,ku,ke,ko,keu,keo,kya,kyu,kye,kyo,kyeo,kwa,kwi,kwe,kweo",
            "t=t,ta,ti,tu,te,to,teu,teo,tya,tyu,tye,tyo,tyeo,twa,twi,twe,tweo",
            "p=p,pa,pi,pu,pe,po,peu,peo,pya,pyu,pye,pyo,pyeo,pwa,pwi,pwe,pweo",
            "h=h,ha,hi,hu,he,ho,heu,heo,hya,hyu,hye,hyo,hyeo,hwa,hwi,hwe,hweo"
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

        bool isAlphaCon(string str) {
            if (str == "gg") { return true; }
            else if (str == "gg") { return true; }
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

        static KoreanCVCPhonemizer() {
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
        public override Phoneme[] Process(Note[] notes, Note? prevNeighbour, Note? nextNeighbour) {
            var note = notes[0];
            var currentUnicode = ToUnicodeElements(note.lyric); // 현재 가사의 유니코드
            string currentLyric = note.lyric; // 현재 가사

            // 가사의 초성, 중성, 종성 분리
            // P(re)Lconsonant, PLvowel, PLfinal / C(urrent)Lconsonant, CLvowel, CLfinal / N(ext)Lconsonant, NLvowel, NLfinal
            int CLconsonant, CLvowel, CLfinal; // 현재 노트의 consonant, vowel, final 인덱스
            string[] TCLconsonant, TCLvowel, TCLfinal; // 현재 노트의 consonant, vowel, final
            string TCLplainvowel; // 현재 노트의 모음의 단순화
            int NLconsonant; // 다음 노트의 consonant 인덱스
            string[] TNLconsonant; // 다음 노트의 consonant

            /*
            string PLconsonant = "", PLvowel = "", PLfinal = "";
            string CLconsonant = "", CLvowel = "", CLfinal = "";
            string NLconsonant = "", NLvowel = "", NLfinal = "";
            */


            
            char first = currentLyric[0];
            int L = 0;

            

            if (first == 'ㄹ') {
                L = 1;
                first = currentLyric[1];
            }

            

            int uCL = (int)first;

            // 한글이면 유니코드로 초중종성 나눔
            if ((uCL >= hangeulStartIndex) && (uCL <= hangeulEndIndex)) {
                // 유니코드로 초중종성 나눔
                
                

                CLconsonant = (uCL - hangeulStartIndex) / (21 * 28);
                CLvowel = (uCL - hangeulStartIndex) % (21 * 28) / 28;
                CLfinal = (uCL - hangeulStartIndex) % 28;




                TCLconsonant = naConsonants[CLconsonant].Split(":");
                TCLvowel = naVowels[CLvowel].Split(":");
                TCLplainvowel = naPlainVowels[CLvowel];
                TCLfinal = naFinals[CLfinal].Split(":");

                // ex. 강 => mean1 : ga / mean : ang
                string mean1 = "";
                mean1 = TCLconsonant[1] + TCLvowel[1];
                string mean2 = "";
                mean2 = TCLplainvowel + TCLfinal[1];

                //만약 앞에 노트가 없으면
                if (prevNeighbour == null) {
                    mean1 = $"- {mean1}";
                }


                // 만약 받침이 있으면
                if (TCLfinal[1] != "") {
                    int totalDuration = notes.Sum(n => n.duration);
                    int fcLength = totalDuration / 3;

                    if (L == 1) { mean1 = mean1.Replace("r", "l"); }

                    return new Phoneme[] {
                        new Phoneme() {
                            phoneme = mean1,
                        },
                        new Phoneme() {
                            phoneme = mean2,
                            position = totalDuration - fcLength,
                        }
                    };
                }

                // 만약 받침이 없으면
                if (TCLfinal[1] == "") { 
                    // 뒤에 노트가 있다면
                    if (nextNeighbour != null) {
                        // 뒷노트 유니코드로 나누기
                        char first2 = (nextNeighbour?.lyric)[0];
                        int nNL = (int)first2;
                        var nextUnicode = ToUnicodeElements(nextNeighbour?.lyric);
                        var nextLyric = string.Join("", nextUnicode);

                        // 한글이면 유니코드로 초중종성 나눔
                        if ((nNL > hangeulStartIndex) && (nNL < hangeulEndIndex)) {
                            int nlUnicode = nNL - hangeulStartIndex;
                            NLconsonant = nlUnicode / (21 * 28);
                            TNLconsonant = naConsonants[NLconsonant].Split(":");

                            var vowel = TCLplainvowel; // 현재 노트의 모음
                            var consonant = TNLconsonant[1]; // 다음 노트의 자음

                            // vc 만들기
                            var vcPhoneme = $"{vowel} {consonant}";
                            if (!singer.TryGetMappedOto(vcPhoneme, note.tone, out var _)) {
                                return new Phoneme[] {
                                new Phoneme() {
                                    phoneme = mean1,
                                }
                             };
                            }

                            int totalDuration = notes.Sum(n => n.duration);
                            int vcLength = 60;
                            if (singer.TryGetMappedOto(nextLyric, note.tone, out var oto)) {
                                vcLength = MsToTick(oto.Preutter);
                            }
                            vcLength = Math.Min(totalDuration / 2, vcLength);

                            if (L == 1) { mean1 = mean1.Replace("r", "l"); }

                            return new Phoneme[] {
                            new Phoneme() {
                                phoneme = mean1,
                            },
                            new Phoneme() {
                                phoneme = vcPhoneme,
                                position = totalDuration - vcLength,
                            }
                        };
                        }
                    }
                   
                }
                // 만약 받침도 없고 뒤에 노트도 없다면

                if (L == 1) { mean1 = mean1.Replace("r", "l"); }
                return new Phoneme[] {
                    new Phoneme {
                        phoneme = mean1,
                    }
                }; 
            }

            // ======================================================================================

            else {
                if (prevNeighbour == null) {
                    // Use "- V" or "- CV" if present in voicebank
                    var initial = $"- {currentLyric}";
                    if (singer.TryGetMappedOto(initial, note.tone, out var _)) {
                        currentLyric = initial;
                    }
                } else if (plainVowels.Contains(currentLyric)) {
                    var prevUnicode = ToUnicodeElements(prevNeighbour?.lyric);

                    // CVC는 VV 구현하지 않음
                    //if (vowelLookup.TryGetValue(prevUnicode.LastOrDefault() ?? string.Empty, out var vow)) {
                    //    currentLyric = $"{vow} {currentLyric}";
                    //}
                }

                if (nextNeighbour != null) { // 다음에 노트가 있으면
                    var nextUnicode = ToUnicodeElements(nextNeighbour?.lyric);
                    var nextLyric = string.Join("", nextUnicode);

                    // Check if next note is a vowel and does not require VC
                    if (plainVowels.Contains(nextUnicode.FirstOrDefault() ?? string.Empty)) {
                        return new Phoneme[] {
                        new Phoneme() {
                            phoneme = currentLyric,
                        }
                    };
                    }

                    // Insert VC before next neighbor
                    // Get vowel from current note
                    var vowel = "";
                    if (vowelLookup.TryGetValue(currentUnicode.LastOrDefault() ?? string.Empty, out var vow)) {

                        vowel = vow;
                    }

                    // Get consonant from next note
                    var consonant = "";
                    if (consonantLookup.TryGetValue(nextUnicode.FirstOrDefault() ?? string.Empty, out var con)) {
                        consonant = getConsonant(nextNeighbour?.lyric); //로마자만 가능
                        if (!(isAlphaCon(consonant))) { consonant = con; }
                    }

                    if (consonant == "") {
                        return new Phoneme[] {
                        new Phoneme() {
                            phoneme = currentLyric,
                        }
                    };
                    }

                    var vcPhoneme = $"{vowel} {consonant}";
                    if (!singer.TryGetMappedOto(vcPhoneme, note.tone, out var _)) {
                        return new Phoneme[] {
                        new Phoneme() {
                            phoneme = currentLyric,
                        }
                    };
                    }

                    int totalDuration = notes.Sum(n => n.duration);
                    int vcLength = 60;
                    if (singer.TryGetMappedOto(nextLyric, note.tone, out var oto)) {
                        vcLength = MsToTick(oto.Preutter);
                    }

                    vcLength = Math.Min(totalDuration / 2, vcLength);



                    return new Phoneme[] {
                    new Phoneme() {
                        phoneme = currentLyric,
                    },
                    new Phoneme() {
                        phoneme = vcPhoneme,
                        position = totalDuration - vcLength,
                    }
                };
                }


                // No next neighbor
                return new Phoneme[] {
                new Phoneme {
                    phoneme = currentLyric,
                    }
                };
            }            
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using Serilog;


namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Korean CVC Phonemizer", "KO CVC", "NANA")]
    public class KoreanCVCPhonemizer : Phonemizer {

        bool isAlphaCon(string str) {
            if (str == "gg") {return true;}
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
            else if (str == "h") { return true; }
            else { return false;}
        }

        static readonly string[] plainVowels = new string[] {"eu","eo","a","i","u","e","o"};

        static readonly string[] vowels = new string[] {
            "eu=geu,neu,deu,reu,leu,meu,beu,seu,eu,jeu,cheu,keu,teu,peu,heu,ggeu,ddeu,bbeu,sseu,jjeu,그,느,드,르,ㄹ르,므,브,스,으,즈,츠,크,트,프,흐,끄,뜨,쁘,쓰,쯔",
            "eo=geo,neo,deo,reo,leo,meo,beo,seo,eo,jeo,cheo,keo,teo,peo,heo,ggeo,ddeo,bbeo,sseo,jjeo,gyeo,nyeo,dyeo,ryeo,lyeo,myeo,byeo,syeo,yeo,jyeo,chyeo,kyeo,tyeo,pyeo,hyeo,ggyeo,ddyeo,bbyeo,ssyeo,jjyeo,gweo,nweo,dweo,rweo,lweo,mweo,bweo,sweo,weo,jweo,chweo,kweo,tweo,pweo,hweo,ggweo,ddweo,bbweo,ssweo,jjweo,거,너,더,러,ㄹ러,머,버,서,어,저,처,커,터,퍼,허,꺼,떠,뻐,써,쩌,겨,녀,뎌,려,ㄹ려,며,벼,셔,여,져,쳐,켜,텨,펴,혀,껴,뗘,뼈,쎠,쪄,궈,눠,둬,뤄,ㄹ뤄,뭐,붜,숴,워,줘,춰,쿼,퉈,풔,훠,꿔,뚸,뿨,쒀,쭤",
            "a=ga,na,da,ra,la,ma,ba,sa,a,ja,cha,ka,ta,pa,ha,gga,dda,bba,ssa,jja,gya,nya,dya,rya,lya,mya,bya,sya,ya,jya,chya,kya,tya,pya,hya,ggya,ddya,bbya,ssya,jjya,gwa,nwa,dwa,rwa,lwa,mwa,bwa,swa,wa,jwa,chwa,kwa,twa,pwa,hwa,ggwa,ddwa,bbwa,sswa,jjwa,가,나,다,라,ㄹ라,마,바,사,아,자,차,카,타,파,하,까,따,빠,싸,짜,갸,냐,댜,랴,ㄹ랴,먀,뱌,샤,야,쟈,챠,캬,탸,퍄,햐,꺄,땨,뺘,쌰,쨔,과,놔,돠,롸,ㄹ롸,뫄,봐,솨,와,좌,촤,콰,톼,퐈,화",
            "e=ge,ne,de,re,le,me,be,se,e,je,che,ke,te,pe,he,gge,dde,bbe,sse,jje,gye,nye,dye,rye,lye,mye,bye,sye,ye,jye,chye,kye,tye,pye,hye,ggye,ddye,bbye,ssye,jjye,gwe,nwe,dwe,rwe,lwe,mwe,bwe,swe,we,jwe,chwe,kwe,twe,pwe,hwe,ggwe,ddwe,bbwe,sswe,jjwe,게,네,데,레,ㄹ레,메,베,세,에,제,체,케,테,페,헤,께,떼,뻬,쎄,쩨,개,내,대,래,ㄹ래,매,배,새,애,재,채,캐,태,패,해,계,녜,뎨,례,ㄹ례,몌,볘,셰,예,졔,쳬,켸,톄,폐,혜,꼐,뗴,뼤,쎼,쪠,걔,냬,댸,럐,ㄹ럐,먜,뱨,섀,얘,쟤,챼,컈,턔,퍠,햬,꺠,떄,뺴,썌,쨰,궤,눼,뒈,뤠,ㄹ뤠,뭬,붸,쉐,웨,줴,췌,퀘,퉤,풰,훼,괘,놰,돼,뢔,ㄹ뢔,뫠,봬,쇄,왜,좨,쵀,쾌,퇘,퐤,홰,괴,뇌,되,뢰,ㄹ뢰,뫼,뵈,쇠,외,죄,최,쾨,퇴,푀,회",
            "i=gi,ni,di,ri,li,mi,bi,si,i,ji,chi,ki,ti,pi,hi,ggi,ddi,bbi,ssi,jji,gwi,nwi,dwi,rwi,lwi,mwi,bwi,swi,wi,jwi,chwi,kwi,twi,pwi,hwi,ggwi,ddwi,bbwi,sswi,jjwi,기,니,디,리,ㄹ리,미,비,시,이,지,치,키,티,피,히,끼,띠,삐,씨,찌,귀,뉘,뒤,뤼,ㄹ뤼,뮈,뷔,쉬,위,쥐,취,퀴,튀,퓌,휘,뀌,뛰,쀠,쒸,쮜",
            "o=go,no,do,ro,lo,mo,bo,so,o,jo,cho,ko,to,po,ho,ggo,ddo,bbo,sso,jjo,gyo,nyo,dyo,ryo,lyo,myo,byo,syo,yo,jyo,chyo,kyo,tyo,pyo,hyo,ggyo,ddyo,bbyo,ssyo,jjyo,고,노,도,로,ㄹ로,모,보,소,오,조,초,코,토,포,호,꼬,또,뽀,쏘,쪼,교,뇨,됴,료,ㄹ료,묘,뵤,쇼,요,죠,쵸,쿄,툐,표,효,꾜,뚀,뾰,쑈,쬬",
            "u=gu,nu,du,ru,lu,mu,bu,su,u,ju,chu,ku,tu,pu,hu,ggu,ddu,bbu,ssu,jju,gyu,nyu,dyu,ryu,lyu,myu,byu,syu,yu,jyu,chyu,kyu,tyu,pyu,hyu,ggyu,ddyu,bbyu,ssyu,jjyu,구,누,두,루,ㄹ루,무,부,수,우,주,추,쿠,투,푸,후,꾸,뚜,뿌,쑤,쭈,규,뉴,듀,류,ㄹ류,뮤,뷰,슈,유,쥬,츄,큐,튜,퓨,휴,뀨,뜌,쀼,쓔,쮸",
            "ng=ang,ing,ung,eng,ong,eung,eong,앙,잉,웅,엥,앵,옹,응,엉",
            "n=an,in,un,en,on,eun,eon,안,인,운,엔,앤,온,은,언",
            "m=am,im,um,em,om,eum,eom,암,임,움,엠,앰,옴,음,엄",
            "l=al,il,ul,el,ol,eul,eol,알,일,울,엘,앨,올,을,얼",
            "p=ap,ip,up,ep,op,eup,eop,압,입,웁,엡,앱,옵,읍,업",
            "t=at,it,ut,et,ot,eut,eot,앗,잇,웃,엣,앳,옷,읏,엇",
            "k=ak,ik,uk,ek,ok,euk,eok,악,익,욱,엑,액,옥,윽,억"
        };

        static readonly string[] consonants = new string[] {
            "gg=gg,gga,ggi,ggu,gge,ggo,ggeu,ggeo,ggya,ggyu,ggye,ggyo,ggyeo,ggwa,ggwi,ggwe,ggweo,ㄲ,까,끼,꾸,께,깨,꼬,끄,꺼,꺄,뀨,꼐,꺠,꾜,껴,꽈,뀌,꿰,꽤,꾀,꿔",
            "dd=dd,dda,ddi,ddu,dde,ddo,ddeu,ddeo,ddya,ddyu,ddye,ddyo,ddyeo,ddwa,ddwi,ddwe,ddweo,ㄸ,따,띠,뚜,떼,때,또,뜨,떠,땨,뜌,뗴,떄,뚀,뗘,똬,뛰,뛔,뙈,뙤,뚸",
            "bb=bb,bba,bbi,bbu,bbe,bbo,bbeu,bbeo,bbya,bbyu,bbye,bbyo,bbyeo,bbwa,bbwi,bbwe,bbweo,ㅃ,빠,삐,뿌,뻬,빼,뽀,쁘,뻐,뺘,쀼,뼤,뺴,뾰,뼈,뽜,쀠,쀄,뽸,뾔,뿨",
            "ss=ss,ssa,ssi,ssu,sse,sso,sseu,sseo,ssya,ssyu,ssye,ssyo,ssyeo,sswa,sswi,sswe,ssweo,ㅆ,싸,씨,쑤,쎄,쌔,쏘,쓰,써,쌰,쓔,쎼,썌,쑈,쎠,쏴,쒸,쒜,쐐,쐬,쒀",
            "g=g,ga,gi,gu,ge,go,geu,geo,gya,gyu,gye,gyo,gyeo,gwa,gwi,gwe,gweo,가,기,구,게,개,고,그,거,갸,규,계,걔,교,겨,과,귀,궤,괘,괴,궈",
            "n=n,na,ni,nu,ne,no,neu,neo,nya,nyu,nye,nyo,nyeo,nwa,nwi,nwe,nweo,나,니,누,네,내,노,느,너,냐,뉴,녜,냬,뇨,녀,놔,뉘,눼,놰,뇌,눠",
            "d=d,da,di,du,de,do,deu,deo,dya,dyu,dye,dyo,dyeo,dwa,dwi,dwe,dweo,다,디,두,데,대,도,드,더,댜,듀,뎨,댸,됴,뎌,돠,뒤,뒈,돼,되,둬",
            "r=r,ra,ri,ru,re,ro,reu,reo,rya,ryu,rye,ryo,ryeo,rwa,rwi,rwe,rweo,라,리,루,레,래,로,르,러,랴,류,례,럐,료,려,롸,뤼,뤠,뢔,뤄",
            "m=m,ma,mi,mu,me,mo,meu,meo,mya,myu,mye,myo,myeo,mwa,mwi,mwe,mweo,마,미,무,메,매,모,므,머,먀,뮤,몌,먜,묘,며,뫄,뮈,뭬,뫠,뫼,뭐",
            "b=b,ba,bi,bu,be,bo,beu,beo,bya,byu,bye,byo,byeo,bwa,bwi,bwe,bweo,바,비,부,베,배,보,브,버,뱌,뷰,볘,뱨,뵤,벼,봐,뷔,붸,봬,뵈,붜",
            "s=s,sa,si,su,se,so,seu,seo,sya,syu,sye,syo,syeo,swa,swi,swe,sweo,사,시,수,세,새,소,스,서,샤,슈,셰,섀,쇼,셔,솨,쉬,쉐,쇄,쇠,숴",
            "j=j,ja,ji,ju,je,jo,jeu,jeo,jya,jyu,jye,jyo,jyeo,jwa,jwi,jwe,jweo,자,지,주,제,재,조,즈,저,쟈,쥬,졔,쟤,죠,져,좌,쥐,줴,좨,죄,줘",
            "ch=ch,cha,chi,chu,che,cho,cheu,cheo,chya,chyu,chye,chyo,chyeo,chwa,chwi,chwe,chweo,차,치,추,체,채,초,츠,처,챠,츄,쳬,챼,쵸,쳐,촤,취,췌,쵀,최,춰",
            "k=k,ka,ki,ku,ke,ko,keu,keo,kya,kyu,kye,kyo,kyeo,kwa,kwi,kwe,kweo,카,키,쿠,케,캐,코,크,커,캬,큐,켸,컈,쿄,켜,콰,퀴,퀘,쾌,쾨,쿼",
            "t=t,ta,ti,tu,te,to,teu,teo,tya,tyu,tye,tyo,tyeo,twa,twi,twe,tweo,타,티,투,테,태,토,트,터,탸,튜,톄,턔,툐,텨,톼,튀,퉤,퇘,퇴,퉈",
            "p=p,pa,pi,pu,pe,po,peu,peo,pya,pyu,pye,pyo,pyeo,pwa,pwi,pwe,pweo,파,피,푸,페,패,포,프,퍼,퍄,퓨,폐,퍠,표,펴,퐈,퓌,풰,퐤,푀,풔",
            "h=h,ha,hi,hu,he,ho,heu,heo,hya,hyu,hye,hyo,hyeo,hwa,hwi,hwe,hweo,하,히,후,헤,해,호,흐,허,햐,휴,혜,햬,효,혀,화,휘,훼,홰,회,훠"
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

        // Store singer in field, will try reading presamp.ini later
        private USinger singer;
        public override void SetSinger(USinger singer) => this.singer = singer;

        public override Phoneme[] Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour) {
            var note = notes[0];
            var currentUnicode = ToUnicodeElements(note.lyric);
            var currentLyric = note.lyric;






            if (prevNeighbour == null) {
                // Use "- V" or "- CV" if present in voicebank
                var initial = $"- {currentLyric}";
                if (singer.TryGetMappedOto(initial, note.tone, out var _)) {
                    currentLyric = initial;
                }
            } else if (plainVowels.Contains(currentLyric)){
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

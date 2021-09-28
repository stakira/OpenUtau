using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin
{
    [Phonemizer("Korean CVVC Phonemizer", "KR CVVC", "Coludy")]
    public class KoreanCVVCPhonemizer : Phonemizer
    {
        static readonly string[] plainVowels = new string[] { "아", "야", "와", "이", "위", "의", "우", "유", "에", "예", "애", "얘", "외", "왜", "웨", "오", "요", "으", "어", "여", "ㄴ", "ㅁ", "ㅇ", "ㄹ" };

        static readonly string[] vowels = new string[] {
            "a=a,ya,wa,가,나,다,라,ㄹ라,마,바,사,아,자,차,카,타,파,하,까,따,빠,싸,짜,갸,냐,댜,랴,ㄹ랴,먀,뱌,샤,야,쟈,챠,캬,탸,퍄,햐,꺄,땨,뺘,쌰,쨔,과,놔,돠,롸,ㄹ롸,뫄,봐,솨,좌,촤,콰,톼,퐈,화,꽈,똬,뽜,쏴,쫘,fa,va,za,tha,rra,fya,vya,zya,thya,fwa,vwa,zwa,thwa",
            "e=e,ye,we,게,네,데,레,ㄹ레,메,베,세,에,제,체,케,테,페,헤,께,떼,뻬,쎄,쩨,계,녜,뎨,례,ㄹ례,몌,볘,셰,예,졔,쳬,켸,톄,폐,혜,꼐,뗴,뼤,쎼,쪠,개,내,대,래,ㄹ래,매,배,새,애,재,채,캐,태,패,해,깨,때,빼,쌔,째,걔,냬,댸,럐,ㄹ럐,먜,뱨,섀,얘,쟤,챼,컈,턔,퍠,햬,꺠,떄,뺴,썌,쨰,괘,놰,돼,뢔,뫠,봬,쇄,왜,좨,쵀,쾌,퇘,퐤,홰,궤,눼,뒈,뤠,ㄹ뤠,뭬,붸,쉐,웨,줴,췌,퀘,퉤,풰,훼,꿰,뛔,쀄,쒜,쮀,괴,뇌,되,뢰,ㄹ뢰,뫼,뵈,쇠,외,죄,최,쾨,퇴,푀,회,꾀,뙤,뾔,쐬,쬐,fe,ve,ze,the,rre,fye,vye,zye,thye,fwe,vwe,zwe,thwe",
            "i=i,wi,eui,기,니,디,리,ㄹ리,미,비,시,이,지,치,키,티,피,히,끼,띠,삐,씨,찌,귀,뉘,뒤,뤼,ㄹ뤼,뮈,뷔,쉬,위,쥐,취,퀴,튀,퓌,휘,뀌,뛰,쀠,쒸,쮜,긔,늬,듸,릐,ㄹ릐,믜,븨,싀,의,즤,츼,킈,틔,픠,희,끠,띄,쁴,씌,쯰,fi,vi,zi,thi,rri,fwi,vwi,zwi,thwi,feui,veui,zeui,theui",
            "o=o,yo,고,노,도,로,ㄹ로,모,보,소,오,조,초,코,토,포,호,꼬,또,뽀,쏘,쪼,교,뇨,됴,료,ㄹ료,묘,뵤,쇼,요,죠,쵸,쿄,툐,표,효,꾜,뚀,뾰,쑈,쬬,fo,vo,zo,tho,rro,fyo,vyo,zyo,thyo",
            "u=u,yu,구,누,두,루,ㄹ루,무,부,수,우,주,추,쿠,투,푸,후,꾸,뚜,뿌,쑤,쭈,규,뉴,듀,류,ㄹ류,뮤,뷰,슈,유,쥬,츄,큐,튜,퓨,휴,뀨,뜌,쀼,쓔,쮸,fu,vu,zu,thu,rru,fyu,vyu,zyu,thyu",
            "eu=eu,그,느,드,르,ㄹ르,므,브,스,으,즈,츠,크,트,프,흐,끄,뜨,쁘,쓰,쯔,feu,veu,zeu,theu,rreu",
            "eo=eo,yeo,weo,거,너,더,러,ㄹ러,머,버,서,어,저,처,커,터,퍼,허,꺼,떠,뻐,써,쩌,궈,눠,둬,뤄,ㄹ뤄,뭐,붜,숴,워,줘,춰,쿼,퉈,풔,훠,꿔,뚸,뿨,쒀,쭤,feo,veo,zeo,theo,rreo,fyeo,vyeo,zyeo,thyeo,fweo,vweo,zweo,thweo",
            "N=N,ㄴ",
            "M=M,ㅁ",
            "NG=NG,ㅇ",
            "L=L,ㄹ",
            "K=K,ㄱ,ㅋ,ㄲ",
            "P=P,ㅂ,ㅍ",
            "T=T,ㄷ,ㅌ,ㅅ"
        };

        static readonly string[] consonants = new string[] {
            "g=g,가,갸,과,기,귀,긔,구,규,게,계,개,걔,괴,괘,궤,고,교,그,거,겨",
            "gg=gg,까,꺄,꽈,끼,뀌,끠,꾸,뀨,께,꼐,깨,꺠,꾀,꽤,꿰,꼬,꾜,끄,꺼,껴",
            "n=n,나,냐,놔,니,뉘,늬,누,뉴,네,녜,내,냬,뇌,놰,눼,노,뇨,느,너,녀",
            "d=d,다,댜,돠,디,뒤,듸,두,듀,데,뎨,대,댸,되,돼,뒈,도,됴,드,더,뎌",
            "dd=dd,따,땨,똬,띠,뛰,띄,뚜,뜌,떼,뗴,때,떄,뙤,뙈,뛔,또,뚀,뜨,떠,뗘",
            "r=r,라,랴,롸,리,뤼,릐,루,류,레,례,래,럐,뢰,뢔,뤠,로,료,르,러,려",
            "l=l,ㄹ라,ㄹ랴,ㄹ롸,ㄹ리,ㄹ뤼,ㄹ릐,ㄹ루,ㄹ류,ㄹ레,ㄹ례,ㄹ래,ㄹ럐,ㄹ뢰,ㄹ뢔,ㄹ뤠,ㄹ로,ㄹ료,ㄹ르,ㄹ러,ㄹ려",
            "m=m,마,먀,뫄,미,뮈,믜,무,뮤,메,몌,매,먜,뫼,뫠,뭬,모,묘,므,머,며",
            "b=b,바,뱌,봐,비,뷔,븨,부,뷰,베,볘,배,뱨,뵈,봬,붸,보,뵤,브,버,벼",
            "bb=bb,빠,뺘,뽜,삐,쀠,쁴,뿌,쀼,뻬,뼤,빼,뺴,뾔,뽸,쀄,뽀,뾰,쁘,뻐,뼈",
            "s=s,사,솨,쉬,싀,수,세,새,쇠,쇄,쉐,소,스,서",
            "sy=sy,샤,시,슈,셰,섀,쇼,셔",
            "ss=ss,싸,쏴,쒸,씌,쑤,쎄,쌔,쐬,쐐,쒜,쏘,쓰,써",
            "ssy=ssy,쌰,씨,쓔,쎼,썌,쑈,쎠",
            "j=j,자,쟈,좌,지,쥐,즤,주,쥬,제,졔,재,쟤,죄,좨,줴,조,죠,즈,저,져",
            "jj=jj,짜,쨔,쫘,찌,쮜,쯰,쭈,쮸,쩨,쪠,째,쨰,쬐,쫴,쮀,쪼,쬬,쯔,쩌,쪄",
            "ch=ch,차,챠,촤,치,취,츼,추,츄,체,쳬,채,챼,최,쵀,췌,초,쵸,츠,처,쳐",
            "k=k,카,캬,콰,키,퀴,킈,쿠,큐,케,켸,캐,컈,쾨,쾌,퀘,코,쿄,크,커,켜",
            "t=t,타,탸,톼,티,튀,틔,투,튜,테,톄,태,턔,퇴,퇘,퉤,토,툐,트,터,텨",
            "p=p,파,퍄,퐈,피,퓌,픠,푸,퓨,페,폐,패,퍠,푀,퐤,풰,포,표,프,퍼,펴",
            "h=h,하,햐,화,히,휘,희,후,휴,헤,혜,해,햬,회,홰,훼,호,효,흐,허,혀",
            "f=f",
            "v=v",
            "z=z",
            "rr=rr",
            "th=th"
        };

        static readonly Dictionary<string, string> vowelLookup;
        static readonly Dictionary<string, string> consonantLookup;

        static KoreanCVVCPhonemizer()
        {
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

        public override Phoneme[] Process(Note[] notes, Note? prevNeighbour, Note? nextNeighbour)
        {
            var note = notes[0];
            var currentUnicode = ToUnicodeElements(note.lyric);
            var currentLyric = note.lyric;

            if (prevNeighbour == null)
            {
                // Use "- V" or "- CV" if present in voicebank
                var initial = $"- {currentLyric}";
                if (singer.TryGetMappedOto(initial, note.tone, out var _))
                {
                    currentLyric = initial;
                }
            }
            else if (plainVowels.Contains(currentLyric))
            {
                var prevUnicode = ToUnicodeElements(prevNeighbour?.lyric);

                // Current note is VV
                if (vowelLookup.TryGetValue(prevUnicode.LastOrDefault() ?? string.Empty, out var vow))
                {
                    currentLyric = $"{vow} {currentLyric}";
                }
            }

            if (nextNeighbour != null)
            {
                var nextUnicode = ToUnicodeElements(nextNeighbour?.lyric);
                var nextLyric = string.Join("", nextUnicode);

                // Check if next note is a vowel and does not require VC
                if (plainVowels.Contains(nextUnicode.FirstOrDefault() ?? string.Empty))
                {
                    return new Phoneme[] {
                        new Phoneme() {
                            phoneme = currentLyric,
                        }
                    };
                }

                // Insert VC before next neighbor
                // Get vowel from current note
                var vowel = "";
                if (vowelLookup.TryGetValue(currentUnicode.LastOrDefault() ?? string.Empty, out var vow))
                {
                    vowel = vow;
                }

                // Get consonant from next note
                var consonant = "";
                if (consonantLookup.TryGetValue(nextUnicode.FirstOrDefault() ?? string.Empty, out var con))
                {
                    consonant = con;
                }

                if (consonant == "")
                {
                    return new Phoneme[] {
                        new Phoneme() {
                            phoneme = currentLyric,
                        }
                    };
                }

                var vcPhoneme = $"{vowel} {consonant}";
                if (!singer.TryGetMappedOto(vcPhoneme, note.tone, out var _))
                {
                    return new Phoneme[] {
                        new Phoneme() {
                            phoneme = currentLyric,
                        }
                    };
                }

                int totalDuration = notes.Sum(n => n.duration);
                int vcLength = 120;
                if (singer.TryGetMappedOto(nextLyric, note.tone, out var oto))
                {
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

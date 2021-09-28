using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Korean CVC Phonemizer", "KR CVC", "NANA")]
    public class KoreanCVCPhonemizer : Phonemizer {
        static readonly string[] plainVowels = new string[] {"a","i","u","e","o","eu","eo"};

        static readonly string[] vowels = new string[] {
            "a=ga,na,da,ra,la,ma,ba,sa,a,ja,cha,ka,ta,pa,ha,gga,dda,bba,ssa,jja,gya,nya,dya,rya,lya,mya,bya,sya,ya,jya,chya,kya,tya,pya,hya,ggya,ddya,bbya,ssya,jjya,gwa,nwa,dwa,rwa,lwa,mwa,bwa,swa,wa,jwa,chwa,kwa,twa,pwa,hwa,ggwa,ddwa,bbwa,sswa,jjwa",
            "e=ge,ne,de,re,le,me,be,se,e,je,che,ke,te,pe,he,gge,dde,bbe,sse,jje,gye,nye,dye,rye,lye,mye,bye,sye,ye,jye,chye,kye,tye,pye,hye,ggye,ddye,bbye,ssye,jjye,gwe,nwe,dwe,rwe,lwe,mwe,bwe,swe,we,jwe,chwe,kwe,twe,pwe,hwe,ggwe,ddwe,bbwe,sswe,jjwe",
            "i=gi,ni,di,ri,li,mi,bi,si,i,ji,chi,ki,ti,pi,hi,ggi,ddi,bbi,ssi,jji,gwi,nwi,dwi,rwi,lwi,mwi,bwi,swi,wi,jwi,chwi,kwi,twi,pwi,hwi,ggwi,ddwi,bbwi,sswi,jjwi,geui,neui,deui,reui,leui,meui,beui,seui,eui,jeui,cheui,keui,teui,peui,heui,ggeui,ddeui,bbeui,sseui,jjeui",
            "o=go,no,do,ro,lo,mo,bo,so,o,jo,cho,ko,to,po,ho,ggo,ddo,bbo,sso,jjo,gyo,nyo,dyo,ryo,lyo,myo,byo,syo,yo,jyo,chyo,kyo,tyo,pyo,hyo,ggyo,ddyo,bbyo,ssyo,jjyo",
            "u=gu,nu,du,ru,lu,mu,bu,su,u,ju,chu,ku,tu,pu,hu,ggu,ddu,bbu,ssu,jju,gyu,nyu,dyu,ryu,lyu,myu,byu,syu,yu,jyu,chyu,kyu,tyu,pyu,hyu,ggyu,ddyu,bbyu,ssyu,jjyu",
            "eu=geu,neu,deu,reu,leu,meu,beu,seu,eu,jeu,cheu,keu,teu,peu,heu,ggeu,ddeu,bbeu,sseu,jjeu",
            "eo=geo,neo,deo,reo,leo,meo,beo,seo,eo,jeo,cheo,keo,teo,peo,heo,ggeo,ddeo,bbeo,sseo,jjeo,gyeo,nyeo,dyeo,ryeo,lyeo,myeo,byeo,syeo,yeo,jyeo,chyeo,kyeo,tyeo,pyeo,hyeo,ggyeo,ddyeo,bbyeo,ssyeo,jjyeo,gweo,nweo,dweo,rweo,lweo,mweo,bweo,sweo,weo,jweo,chweo,kweo,tweo,pweo,hweo,ggweo,ddweo,bbweo,ssweo,jjweo",
            "n=an,in,un,en,on,eun,eon",
            "m=am,im,um,em,om,eum,eom",
            "l=al,il,ul,el,ol,eul,eol",
            "ng=ang,ing,ung,eng,ong,eung,eong",
            "p=ap,ip,up,ep,op,eup,eop",
            "t=at,it,ut,et,ot,eut,eot",
            "k=ak,ik,uk,ek,ok,euk,eok"
        };

        static readonly string[] consonants = new string[] {
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
            "h=h,ha,hi,hu,he,ho,heu,heo,hya,hyu,hye,hyo,hyeo,hwa,hwi,hwe,hweo",
            "kk=gg,gga,ggi,ggu,gge,ggo,ggeu,ggeo,ggya,ggyu,ggye,ggyo,ggyeo,ggwa,ggwi,ggwe,ggweo",
            "tt=dd,dda,ddi,ddu,dde,ddo,ddeu,ddeo,ddya,ddyu,ddye,ddyo,ddyeo,ddwa,ddwi,ddwe,ddweo",
            "bb=bb,bba,bbi,bbu,bbe,bbo,bbeu,bbeo,bbya,bbyu,bbye,bbyo,bbyeo,bbwa,bbwi,bbwe,bbweo",
            "ss=ss,ssa,ssi,ssu,sse,sso,sseu,sseo,ssya,ssyu,ssye,ssyo,ssyeo,sswa,sswi,sswe,ssweo",
            "jj=jj,jja,jji,jju,jje,jjo,jjeu,jjeo,jjya,jjyu,jjye,jjyo,jjyeo,jjwa,jjwi,jjwe,jjweo"
            };


        static readonly Dictionary<string, string> vowelLookup;
        static readonly Dictionary<string, string> consonantLookup;

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

        public override Phoneme[] Process(Note[] notes, Note? prevNeighbour, Note? nextNeighbour) {
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
                    consonant = con;
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

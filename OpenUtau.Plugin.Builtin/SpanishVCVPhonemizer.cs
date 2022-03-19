using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Spanish VCV Phonemizer", "ES VCV", "Lotte V")]
    public class SpanishVCVPhonemizer : Phonemizer {
        /// <summary>
        /// The lookup table to match a syllable with its tail vowel.
        /// </summary>
        static readonly string[] vowels = new string[] {
            "a=a,ba,da,fa,ga,ha,ja,ka,la,ma,na,pa,ra,sa,ta,va,wa,ya,za,cha,nha,rra,sha,tsa,lla,bya,dya,fya,gya,hya,jya,kya,lya,mya,nya,pya,rya,sya,tya,vya,zya,rrya,bwa,dwa,fwa,gwa,hwa,jwa,kwa,lwa,mwa,pwa,rwa,swa,twa,vwa,zwa,chwa,rrwa,llwa,bla,fla,gla,kla,pla,bra,dra,fra,gra,kra,pra,tra,bua,dua,fua,gua,kua,lua,mua,nua,pua,rua,sua,tua,vua,zua,chua,rrua,llua,bia,dia,fia,gia,hia,jia,kia,lia,mia,nia,pia,ria,sia,tia,via,zia,rria",
            "e=e,be,de,fe,ge,he,je,ke,le,me,ne,pe,re,se,te,ve,we,ye,ze,che,nhe,rre,she,tse,lle,bye,dye,fye,gye,hye,jye,kye,lye,mye,nye,pye,rye,sye,tye,vye,zye,rrye,bwe,dwe,fwe,gwe,hwe,jwe,kwe,lwe,mwe,pwe,rwe,swe,twe,vwe,zwe,chwe,rrwe,llwe,ble,fle,gle,kle,ple,bre,dre,fre,gre,kre,pre,tre,bue,due,fue,gue,kue,lue,mue,nue,pue,rue,sue,tue,vue,zue,chue,rrue,llue,bie,die,fie,gie,hie,jie,kie,lie,mie,nie,pie,rie,sie,tie,vie,zie,rrie",
            "i=i,bi,fi,gi,gi,hi,ji,ki,li,mi,ni,pi,ri,si,ti,vi,wi,yi,zi,chi,nhi,rri,shi,tsi,lli,bwi,dwi,fwi,gwi,hwi,jwi,kwi,lwi,mwi,nwi,pwi,rwi,swi,twi,vwi,zwi,chwi,rrwi,llwi,bli,fli,gli,kli,pli,bri,dri,fri,gri,kri,pri,tri,bui,dui,fui,gui,hui,jui,kui,lui,mui,nui,pui,rui,sui,tui,vui,zui,chui,rrui,llui",
            "o=o,bo,do,fo,go,ho,jo,ko,lo,mo,no,po,ro,so,to,vo,wo,yo,zo,cho,nho,rro,sho,tso,llo,byo,dyo,fyo,gyo,hyo,jyo,kyo,lyo,myo,nyo,pyo,ryo,syo,tyo,vyo,zyo,rryo,bwo,dwo,fwo,gwo,hwo,jwo,kwo,lwo,mwo,pwo,rwo,swo,two,vwo,zwo,chwo,rrwo,llwo,blo,flo,glo,klo,plo,bro,dro,fro,gro,kro,pro,tro,buo,duo,fuo,guo,kuo,luo,muo,nuo,puo,ruo,suo,tuo,vuo,zuo,chuo,rruo,lluo,bio,dio,fio,gio,hio,jio,kio,lio,mio,nio,pio,rio,sio,tio,vio,zio,rrio",
            "u=u,bu,du,fu,gu,hu,ju,ku,lu,mu,nu,pu,ru,su,tu,vu,wu,yu,zu,chu,nhu,rru,shu,tsu,llu,byu,dyu,fyu,gyu,hyu,jyu,kyu,lyu,myu,nyu,pyu,ryu,syu,tyu,vyu,zyu,rryu,blu,flu,glu,klu,plu,bru,dru,fru,gru,kru,pru,tru,biu,diu,fiu,giu,hiu,jiu,kiu,liu,miu,niu,piu,riu,siu,tiu,viu,ziu,rriu",
            "l=l",
            "m=m",
            "n=n",
            "b=b",
            "d=d",
            "f=f",
            "g=g",
            "h=h",
            "k=k",
            "p=p",
            "r=r",
            "s=s",
            "t=t",
            "z=z",
            "ks=ks",
            "hh=hh",
        };

        static readonly Dictionary<string, string> vowelLookup;

        static SpanishVCVPhonemizer() {
            // Converts the lookup table from raw strings to a dictionary for better performance.
            vowelLookup = vowels.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
        }

        private USinger singer;

        // Simply stores the singer in a field.
        public override void SetSinger(USinger singer) => this.singer = singer;

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            var note = notes[0];
            if (!string.IsNullOrEmpty(note.phoneticHint)) {
                // If a hint is present, returns the hint.
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme {
                            phoneme = note.phoneticHint,
                        }
                    },
                };
            }
            // The alias for no previous neighbour note. For example, "- na" for "na".
            var phoneme = $"- {note.lyric}";
            if (prevNeighbour != null) {
                // If there is a previous neighbour note, first get its hint or lyric.
                var lyric = prevNeighbour?.phoneticHint ?? prevNeighbour?.lyric;
                // Get the last unicode element of the hint or lyric. For example, "ya" from "kya" or "- kya".
                var unicode = ToUnicodeElements(lyric);
                // Look up the trailing vowel. For example "a" for "ya".
                if (vowelLookup.TryGetValue(unicode.LastOrDefault() ?? string.Empty, out var vow)) {
                    // Now replace "- na" initially set to "a na".
                    phoneme = $"{vow} {note.lyric}";
                }
            }
            // Get color
            string color = string.Empty;
            int toneShift = 0;
            if (note.phonemeAttributes != null) {
                var attr = note.phonemeAttributes.FirstOrDefault(attr => attr.index == 0);
                color = attr.voiceColor;
                toneShift = attr.toneShift;
            }
            if (singer.TryGetMappedOto(phoneme, note.tone + toneShift, color, out var oto)) {
                phoneme = oto.Alias;
            } else {
                phoneme = note.lyric;
            }
            return new Result {
                phonemes = new Phoneme[] {
                    new Phoneme {
                        phoneme = phoneme,
                    }
                },
            };
        }
    }
}

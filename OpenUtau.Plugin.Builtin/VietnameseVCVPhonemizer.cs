using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Vietnamese VCV Phonemizer", "JA VCV", "AnhDuy-JaniTran")]
    public class VietnameseVCVPhonemizer : Phonemizer {
        /// <summary>
        /// The lookup table to convert a hiragana to its tail vowel.
        /// </summary>
        static readonly string[] vowels = new string[] {
            "a=a,ba,cha,da,fa,ha,ga,ka,kha,la,ma,na,nga,nha,sa,ra,ta,tha,tra,va,ya,za,wa",
            "A=A,bA,chA,dA,fA,hA,gA,kA,khA,lA,mA,nA,ngA,nhA,sA,rA,tA,thA,trA,vA,yA,zA,wA",
            "@=@,b@,ch@,d@,f@,h@,g@,k@,kh@,l@,m@,n@,ng@,nh@,s@,r@,t@,th@,tr@,v@,y@,z@,w@",
            "i=i,bi,chi,di,fi,hi,gi,ki,khi,li,mi,ni,ngi,nhi,si,ri,ti,thi,tri,vi,yi,zi,wi,u@i,uOi",
            "e=e,be,che,de,fe,he,ge,ke,khe,le,me,ne,nge,nhe,se,re,te,the,tre,ve,ye,ze,we",
            "E=E,bE,chE,dE,fE,hE,gE,kE,khE,lE,mE,nE,ngE,nhE,sE,rE,tE,thE,trE,vE,yE,zE,wE",
            "o=o,bo,cho,do,fo,ho,go,ko,kho,lo,mo,no,ngo,nho,so,ro,to,tho,tro,vo,yo,zo,wo",
            "O=O,bO,chO,dO,fO,hO,gO,kO,khO,lO,mO,nO,ngO,nhO,sO,rO,tO,thO,trO,vO,yO,zO,wO",
            "u=u,bu,chu,du,fu,hu,gu,ku,khu,lu,mu,nu,ngu,nhu,su,ru,tu,thu,tru,vu,yu,zu,wu,iEu",
            "U=U,bU,chU,dU,fU,hU,gU,kU,khU,lU,mU,nU,ngU,nhU,sU,rU,tU,thU,trU,vU,yU,zU,wU",
            "m=m,iEm,U@m,uOm",
            "n=n,iEn,U@n,uOn",
            "ng=g",
            "nh=h",
            "ng=iEng,U@ng,,uOng",
        };

        static readonly Dictionary<string, string> vowelLookup;

        static VietnameseVCVPhonemizer() {
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
            // The alias for no previous neighbour note. For example, "- な" for "な".
            var phoneme = $"- {note.lyric}";
            if (prevNeighbour != null) {
                // If there is a previous neighbour note, first get its hint or lyric.
                var lyric = prevNeighbour?.phoneticHint ?? prevNeighbour?.lyric;
                // Get the last unicode element of the hint or lyric. For example, "ゃ" from "きゃ" or "- きゃ".
                var unicode = ToUnicodeElements(lyric);
                // Look up the trailing vowel. For example "a" for "ゃ".
                if (vowelLookup.TryGetValue(unicode.LastOrDefault() ?? string.Empty, out var vow)) {
                    // Now replace "- な" initially set to "a な".
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

using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core {
    public class ChineseCVExpandPhonemizer : Phonemizer {
        static readonly string consonants = "b,p,m,f,d,t,n,l,g,k,h,j,q,x,z,c,s,zh,ch,sh,r,y,w";
        static readonly string vowels = "ai=_ai,uai=_uai,an=_an,ian=_en2,uan=_an,van=_en2,ang=_ang,iang=_ang,uang=_ang,ao=_ao,iao=_ao,ou=_ou,iu=_ou,ong=_ong,iong=_ong,ei=_ei,ui=_ei,uei=_ei,en=_en,un=_un,uen=_un,eng=_eng,in=_in,ing=_ing,vn=_vn";

        static HashSet<string> cSet;
        static Dictionary<string, string> vDict;

        static ChineseCVExpandPhonemizer() {
            cSet = consonants.Split(',').ToHashSet();
            vDict = vowels.Split(',')
                .Select(s => s.Split('='))
                .ToDictionary(a => a[0], a => a[1]);
        }

        private USinger singer;

        public override string Name => "Chinese CV-Expand (整音扩张) Phonemizer";
        public override string Tag => "ZH CVX";
        public override void SetSinger(USinger singer) => this.singer = singer;

        public override Phoneme[] Process(Note note, Note? prev, Note? next) {
            string vowel = string.Empty;
            if (note.lyric.Length > 2 && cSet.Contains(note.lyric.Substring(0, 2))) {
                vowel = note.lyric.Substring(2);
            } else if (note.lyric.Length > 1 && cSet.Contains(note.lyric.Substring(0, 1))) {
                vowel = note.lyric.Substring(1);
            }
            string phoneme0 = TryMapPhoneme(note.lyric, note.tone, singer);
            if (vDict.TryGetValue(vowel, out var phoneme1)) {
                phoneme1 = TryMapPhoneme(phoneme1, note.tone, singer);
                int length1 = 120;
                if (length1 > note.duration / 2) {
                    length1 = note.duration / 2;
                }
                return new Phoneme[] {
                    new Phoneme() {
                        phoneme = phoneme0,
                        duration = note.duration - length1,
                    },
                    new Phoneme() {
                        phoneme = phoneme1,
                        duration = length1,
                    }
                };
            }
            return new Phoneme[] {
                new Phoneme() {
                    phoneme = phoneme0,
                    duration = note.duration,
                }
            };
        }
    }
}

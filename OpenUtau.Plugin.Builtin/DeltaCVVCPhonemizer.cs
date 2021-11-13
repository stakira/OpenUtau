using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenUtau.Api;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("English CVVC Phonemizer", "EN CVVC", "nago")]
    public class DeltaCVVCPhonemizer : SyllableBasedPhonemizer {

        private readonly string[] vowels = "a,A,@,{,V,O,aU,aI,E,3,eI,I,i,oU,OI,U,u".Split(",");
        private readonly string[] consonants = "b,tS,d,D,f,g,h,dZ,k,l,m,n,N,p,r,s,S,t,T,v,w,j,z,Z".Split(",");
        private readonly Dictionary<string, string> dictionaryReplacements = ("AA=A;AA0=A;AA1=A;AA2=A;AE={;AE0={;AE1={;AE2={;AH=V;AH0=V;AH1=V"
        + "AH2=V;AO=O;AO0=O;AO1=O;AO2=O;AW=aU;AW0=aU;AW1=aU;AW2=aU;AY=aI;AY0=aI;AY1=aI;AY2=aI;B=b;CH=tS;D=d;DH=D;EH=E;EH0=E;EH1=E;EH2=E;ER=3"
        + "ER0=3;ER1=3;ER2=3;EY=eI;EY0=eI;EY1=eI;EY2=eI;F=f;G=g;HH=h;IH=I;IH0=I;IH1=I;IH2=I;IY=i;IY0=i;IY1=i;IY2=i;JH=dZ;K=k;L=l;M=m;N=n;NG=N"
        + "OW=oU;OW0=oU;OW1=oU;OW2=oU;OY=OI;OY0=OI;OY1=OI;OY2=OI;P=p;R=r;S=s;SH=S;T=t;TH=T;UH=U;UH0=U;UH1=U;UH2=U;UW=u;UW0=u;UW1=u;UW2=u;V=v"
        + "W=w;Y=j;Z=z;ZH=Z").Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);

        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => consonants;
        protected override string GetDictionaryName() => "cmudict-0_7b.txt";
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => dictionaryReplacements;

        protected override List<string> ProcessSyllable(Syllable syllable) {
            string prevV = syllable.prevV;
            string[] cc = syllable.cc;
            string v = syllable.v;

            string basePhoneme;
            var phonemes = new List<string>();
            if (syllable.IsStartingV) {
                basePhoneme = $"- {v}";
            }
            else if (syllable.IsVV) {
                basePhoneme = v;
            }
            else if (syllable.IsStartingCV) {
                basePhoneme = $"- {cc.Last()}{v}";
                for (var i = 0; i < cc.Length - 1; i++) {
                    phonemes.Add($"- {cc[i]}");
                }
            }
            else { // VCV
                if (cc.Length == 1) {
                    basePhoneme = $"{cc.Last()}{v}";
                } else {
                    basePhoneme = $"- {cc.Last()}{v}";
                }
            }

            phonemes.Add(basePhoneme);
            return phonemes;
        }

        protected override List<string> ProcessEnding(Ending ending) {
            string[] cc = ending.cc;
            string v = ending.prevV;

            var phonemes = new List<string>();
            if (ending.IsEndingV) {
                phonemes.Add($"{v} -");
            }
            else {
                phonemes.Add($"{v} {cc[0]}-");
                for (var i = 1; i < cc.Length; i++) {
                    var cr = $"{cc[i]} -";
                    phonemes.Add(HasOto(cr, ending.tone) ? cr : cc[i]);
                }
            }

            return phonemes;
        }
    }
}

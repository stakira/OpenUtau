using System;
using System.Collections.Generic;
using System.Text;
using OpenUtau.Api;
using System.Linq;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Russian CVC Phonemizer", "RU CVC", "Heiden.BZR")]
    public class RussianCVCPhonemizer : AdvancedPhonemizer {

        private readonly string[] vowels = "a,e,o,u,y,i,M,N".Split(",");
        private readonly string[] consonants = "b',b,v',v,g',g,d',d,z',z,k',k,l',l,m',m,n',n,p',p,r',r,s',s,t',t,f',f,h',h,w',w,j,~,c,4',".Split(",");
        private readonly Dictionary<string, string> aliasesFallback = "ic=yc;y4'=y4;ij=yj;ic-=yc-;y4'-=y4-;ij-=yj-".Split(';')
                .Select(entry => entry.Split('='))
                .ToDictionary(parts => parts[0], parts => parts[1]);
        private readonly string[] burstConsonants = "t,t',k,k',p,p',4',c,b,b',g,g',d,d'".Split(",");
        private readonly Dictionary<string, string> dictionaryReplacements = ("a=a;aa=a;ay=a;b=b;bb=b';c=c;ch=4';d=d;dd=d';ee=e;f=f;ff=f';" +
            "g=g;gg=g';h=h;hh=h';i=i;ii=i;j=~;ja=a;je=e;jo=o;ju=u;k=k;kk=k';l=l;ll=l';m=m;mm=m';n=n;nn=n';oo=o;p=p;pp=p';r=r;rr=r';ae=e;" +
            "s=s;sch=w';sh=w;ss=s';t=t;tt=t';u=u;uj=u;uu=u;v=v;vv=v';y=y;yy=y;z=z;zh=j;zz=z'").Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);
        private readonly string[] noStartConsonants = "t,t',k,k',p,p',4',c".Split(",");

        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => consonants;
        protected override string GetDictionary() => Dictionaries.cmudict_ru;
        protected override Dictionary<string, string> GetAliasesFallback() => aliasesFallback;
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => dictionaryReplacements;

        protected override List<string> TrySyllable(Syllable syllable) {
            string prevV = syllable.prevV;
            string[] cc = syllable.cc;
            string v = syllable.v;

            string basePhoneme;
            var phonemes = new List<string>();
            if (prevV == "") {
                if (cc.Length == 0) {
                    basePhoneme = $"-{v}";
                }
                else {
                    basePhoneme = $"-{cc.Last()}{v}";
                }
                for (var i = 0; i < cc.Length - 1; i++) {
                    if (!noStartConsonants.Contains(cc[0])) {
                        phonemes.Add($"-{cc[0]}");
                    }
                }
            }
            else if (cc.Length == 0) {
                basePhoneme = v;
            }
            else {
                if (cc.Length == 1 || IsShort(syllable) || cc.Last() == "`") {
                    basePhoneme = $"{cc.Last()}{v}";
                }
                else {
                    basePhoneme = $"-{cc.Last()}{v}";
                }
                phonemes.Add($"{prevV}{cc[0]}"); ;
                var offset = burstConsonants.Contains(cc[0]) ? 0 : 1;
                for (var i = offset; i < cc.Length - 1; i++) {
                    phonemes.Add(cc[i]);
                }
            }
            phonemes.Add(basePhoneme);
            return phonemes;
        }

        protected override List<string> TryEnding(Ending ending) {
            string[] cc = ending.cc;
            string v = ending.prevV;

            var phonemes = new List<string>();
            if (cc.Length == 0) {
                phonemes.Add($"{v}-");
            }
            else {
                phonemes.Add($"{v}{cc[0]}-");
                for (var i = 1; i < cc.Length; i++) {
                    phonemes.Add(cc[i]);
                }
            }

            return phonemes;
        }

    }
}

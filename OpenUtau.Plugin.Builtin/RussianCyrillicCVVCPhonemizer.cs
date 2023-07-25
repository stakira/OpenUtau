using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenUtau.Api;
using OpenUtau.Core.G2p;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Russian Cyrillic CVVC Phonemizer", "RU CYR-CVVC", "Lotte V", language: "RU")]
    public class RussianCyrillicCVVCPhonemizer : SyllableBasedPhonemizer {

        /// <summary>
        /// Russian phonemizer for Cyrillic-aliased CVVC banks.
        /// It currently only supports standalone consonants, rather than cluster transitions.
        /// Please note that phonetic hints are still in Roman characters, e.g. [p' a t'] ("пять")
        /// </summary>

        private readonly string[] vowels = "a,e,i,o,u,y".Split(',');
        private readonly string[] consonants = "b,v,g,d,zh,z,k,l,m,n,p,r,s,t,f,h,ts,sh,b',v',j,g',d',z',k',l',m',n',p',r',s',t',f',h',ch,sch,`".Split(',');
        private readonly Dictionary<string, string> dictionaryReplacements = ("a=a;aa=a;ay=a;b=b;bb=b';c=ts;ch=ch;d=d;dd=d';ee=e;" +
            "f=f;ff=f';g=g;gg=g';h=h;hh=h';i=i;ii=i;j=j;ja=a;je=e;jo=o;ju=u;k=k;kk=k';l=l;ll=l';m=m;mm=m';n=n;nn=n';oo=o;ae=e;" +
            "p=p;pp=p';r=r;rr=r';s=s;sch=sch;sh=sh;ss=s';t=т;tt=t';u=u;uj=u;uu=u;v=v;vv=v';y=y;yy=y;z=z;zh=zh;zz=z'").Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);

        private readonly string[] burstConsonants = "b,b',g,g',d,d',k,k',p,p',r,r',t,t',ts,ch,б,бь,г,гь,д,дь,к,кь,п,пь,р,рь,т,ть,ц,ч".Split(',');
        private readonly string[] hardConsonants = "b,v,g,d,zh,z,k,l,m,n,p,r,s,r,f,h,ts,sh".Split(',');
        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => consonants;
        protected override string GetDictionaryName() => "cmudict_ru.txt";
        protected override IG2p LoadBaseDictionary() => new RussianG2p();
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => dictionaryReplacements;

        protected override List<string> ProcessSyllable(Syllable syllable) {
            string prevV = syllable.prevV;
            string[] cc = syllable.cc;
            string v = syllable.v;

            string basePhoneme;
            var phonemes = new List<string>();

            if (syllable.IsStartingV) {
                basePhoneme = $"- {v}";
            } else if (syllable.IsVV) {
                var vv = $"{prevV} {v}";
                if (!CanMakeAliasExtension(syllable)) {
                    vv = ValidateAlias(vv);
                    basePhoneme = vv;
                } else {
                    // the previous alias will be extended
                    basePhoneme = null;
                }
            } else if (syllable.IsStartingCVWithOneConsonant) {
                // TODO: move to config -CV or -C CV
                var rcv = $"- {cc[0]}{v}";
                rcv = ValidateAlias(rcv);
                basePhoneme = rcv;
            } else if (syllable.IsStartingCVWithMoreThanOneConsonant) {
                var cv = $"{cc.Last()}{v}";
                cv = ValidateAlias(cv);
                basePhoneme = cv;
                for (var i = 0; i < cc.Length - 1; i++) {
                    phonemes.Add($"- {ValidateAlias(cc[i])}");
                }
            } else { // VCV
                var cv = $"{cc.Last()}{v}";
                var vc = $"{prevV} {cc[0]}";
                cv = ValidateAlias(cv);
                basePhoneme = cv;
                vc = ValidateAlias(vc);
                phonemes.Add(vc);
                for (var i = 0; i < cc.Length - 1; i++) {
                    cc[i] = ValidateAlias(cc[i]);
                    cc[0] = ValidateAlias(cc[0]);
                    phonemes.Add(cc[i]);
                    //if (!burstConsonants.Contains(cc[0]) && cc[0].Contains(cc[i]) || syllable.IsStartingCVWithMoreThanOneConsonant && cc[0].Contains(cc[i])) {
                    //    phonemes.Remove(cc[0]);
                    //}
                    //if (!cc.Last().Contains(cc[i])) {
                    //    phonemes.Remove(cc.Last());
                    //}
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
                var vr = $"{v} -";
                phonemes.Add(vr);
            } else if (ending.IsEndingVCWithOneConsonant) {
                var cr = $"{cc[0]} -";
                // TODO: move to config VC- or VC C-
                var vc = $"{v} {cc[0]}";
                vc = ValidateAlias(vc);
                phonemes.Add(vc);
                cr = ValidateAlias(cr);
                cc[0] = ValidateAlias(cc[0]);
                if (HasOto(cr, ending.tone) && !burstConsonants.Contains(cc[0])) {
                    phonemes.Add(cr);
                } else {
                    TryAddPhoneme(phonemes, ending.tone, cc[0]);
                }
            } else {
                var vc = $"{v} {cc[0]}";
                vc = ValidateAlias(vc);
                phonemes.Add(vc);
                var cr = $"{cc.Last()} -";
                for (var i = 0; i < cc.Length - 1; i++) {
                    phonemes.Add(cc[i]);
                    phonemes.Add(cc.Last());
                    if (!burstConsonants.Contains(cc[0]) && cc[0] != cc[i]) {
                        phonemes.Remove(cc[0]);
                    }
                }
                cr = ValidateAlias(cr);
                if (!burstConsonants.Contains(cc.Last())) {
                    phonemes.Add(cr);
                }
            }
            return phonemes;
        }

        // Cyrillic conversion
        protected override string ValidateAlias(string alias) {
            foreach (var consonant in new[] { "b'" }) {
                alias = alias.Replace(consonant, "бь");
            }
            foreach (var consonant in new[] { "v'" }) {
                alias = alias.Replace(consonant, "вь");
            }
            foreach (var consonant in new[] { "g'" }) {
                alias = alias.Replace(consonant, "гь");
            }
            foreach (var consonant in new[] { "d'" }) {
                alias = alias.Replace(consonant, "дь");
            }
            foreach (var consonant in new[] { "z'" }) {
                alias = alias.Replace(consonant, "зь");
            }
            foreach (var consonant in new[] { "k'" }) {
                alias = alias.Replace(consonant, "кь");
            }
            foreach (var consonant in new[] { "l'" }) {
                alias = alias.Replace(consonant, "ль");
            }
            foreach (var consonant in new[] { "m'" }) {
                alias = alias.Replace(consonant, "мь");
            }
            foreach (var consonant in new[] { "n'" }) {
                alias = alias.Replace(consonant, "нь");
            }
            foreach (var consonant in new[] { "p'" }) {
                alias = alias.Replace(consonant, "пь");
            }
            foreach (var consonant in new[] { "r'" }) {
                alias = alias.Replace(consonant, "рь");
            }
            foreach (var consonant in new[] { "s'" }) {
                alias = alias.Replace(consonant, "сь");
            }
            foreach (var consonant in new[] { "t'" }) {
                alias = alias.Replace(consonant, "ть");
            }
            foreach (var consonant in new[] { "f'" }) {
                alias = alias.Replace(consonant, "фь");
            }
            foreach (var consonant in new[] { "h'" }) {
                alias = alias.Replace(consonant, "хь");
            }
            if (alias.Contains("ьа")) {
                return alias.Replace("ьа", "я");
            }
            if (alias.Contains("ьэ")) {
                return alias.Replace("ьэ", "е");
            }
            if (alias.Contains("ьо")) {
                return alias.Replace("ьо", "ё");
            }
            if (alias.Contains("ьу")) {
                return alias.Replace("ьу", "ю");
            }
            foreach (var consonant in hardConsonants) {
                foreach (var vowel in new[] { "i" }) {
                    alias = alias.Replace(consonant + "i", consonant + "y");
                }
            }
            foreach (var consonant in new[] { "ь", "ч", "щ", "й" }) {
                foreach (var vowel in new[] { "ы" }) {
                    alias = alias.Replace(consonant + vowel, consonant + "и");
                }
            }
            foreach (var consonant in new[] { "zh", "sh" }) {
                foreach (var vowel in new[] { "y" }) {
                    alias = alias.Replace(consonant + "y", consonant + "i");
                }
            }
            foreach (var consonant in new[] { "'", "ch", "sch", "j" }) {
                foreach (var vowel in new[] { "y" }) {
                    alias = alias.Replace(consonant + vowel, consonant + "i");
                }
            }
            foreach (var consonant in new[] { "'" }) {
                foreach (var vowel in new[] { "i" }) {
                    alias = alias.Replace("'", "");
                }
            }
            foreach (var consonant in new[] { "'" }) {
                foreach (var vowel in new[] { "y" }) {
                    alias = alias.Replace("'", "");
                }
            }
            if (alias.Contains("ja")) {
                return alias.Replace("ja", "я");
            }
            if (alias.Contains("je")) {
                return alias.Replace("je", "е");
            }
            if (alias.Contains("jo")) {
                return alias.Replace("jo", "ё");
            }
            if (alias.Contains("ju")) {
                return alias.Replace("ju", "ю");
            }
            foreach (var vowel in new[] { "a" }) {
                alias = alias.Replace("a", "а");
            }
            foreach (var vowel in new[] { "e" }) {
                alias = alias.Replace("e", "э");
            }
            foreach (var vowel in new[] { "i" }) {
                alias = alias.Replace("i", "и");
            }
            foreach (var vowel in new[] { "o" }) {
                alias = alias.Replace("o", "о");
            }
            foreach (var vowel in new[] { "u" }) {
                alias = alias.Replace("u", "у");
            }
            foreach (var vowel in new[] { "y" }) {
                alias = alias.Replace("y", "ы");
            }
            foreach (var consonant in new[] { "sch" }) {
                alias = alias.Replace(consonant, "щ");
            }
            foreach (var consonant in new[] { "ts" }) {
                alias = alias.Replace(consonant, "ц");
            }
            foreach (var consonant in new[] { "ch" }) {
                alias = alias.Replace(consonant, "ч");
            }
            foreach (var consonant in new[] { "sh" }) {
                alias = alias.Replace(consonant, "ш");
            }
            foreach (var consonant in new[] { "b" }) {
                alias = alias.Replace(consonant, "б");
            }
            foreach (var consonant in new[] { "v" }) {
                alias = alias.Replace(consonant, "в");
            }
            foreach (var consonant in new[] { "g" }) {
                alias = alias.Replace(consonant, "г");
            }
            foreach (var consonant in new[] { "d" }) {
                alias = alias.Replace(consonant, "д");
            }
            foreach (var consonant in new[] { "zh" }) {
                alias = alias.Replace(consonant, "ж");
            }
            foreach (var consonant in new[] { "z" }) {
                alias = alias.Replace(consonant, "з");
            }
            foreach (var consonant in new[] { "k" }) {
                alias = alias.Replace(consonant, "к");
            }
            foreach (var consonant in new[] { "l" }) {
                alias = alias.Replace(consonant, "л");
            }
            foreach (var consonant in new[] { "m" }) {
                alias = alias.Replace(consonant, "м");
            }
            foreach (var consonant in new[] { "n" }) {
                alias = alias.Replace(consonant, "н");
            }
            foreach (var consonant in new[] { "p" }) {
                alias = alias.Replace(consonant, "п");
            }
            foreach (var consonant in new[] { "r" }) {
                alias = alias.Replace(consonant, "р");
            }
            foreach (var consonant in new[] { "s" }) {
                alias = alias.Replace(consonant, "с");
            }
            foreach (var consonant in new[] { "t" }) {
                alias = alias.Replace(consonant, "т");
            }
            foreach (var consonant in new[] { "f" }) {
                alias = alias.Replace(consonant, "ф");
            }
            foreach (var consonant in new[] { "h" }) {
                alias = alias.Replace(consonant, "х");
            }
            foreach (var consonant in new[] { "j" }) {
                alias = alias.Replace(consonant, "й");
            }
            if (alias.Contains("i")) {
                return alias.Replace("i", "и");
            }
            if (alias.Contains("ьа")) {
                return alias.Replace("ьа", "я");
            }
            if (alias.Contains("ье")) {
                return alias.Replace("ье", "е");
            }
            if (alias.Contains("ьи")) {
                return alias.Replace("ьи", "и");
            }
            if (alias.Contains("ьо")) {
                return alias.Replace("ьо", "ё");
            }
            if (alias.Contains("ьу")) {
                return alias.Replace("ьу", "ю");
            }
            foreach (var vowel in new[] { "а " }) {
                alias = alias.Replace("а ", "a ");
            }
            foreach (var vowel in new[] { "э " }) {
                alias = alias.Replace("э ", "e ");
            }
            foreach (var vowel in new[] { "и " }) {
                alias = alias.Replace("и ", "i ");
            }
            foreach (var vowel in new[] { "о " }) {
                alias = alias.Replace("о ", "o ");
            }
            foreach (var vowel in new[] { "у " }) {
                alias = alias.Replace("у ", "u ");
            }
            foreach (var vowel in new[] { "ы " }) {
                alias = alias.Replace("ы ", "y ");
            }
            foreach (var consonant in hardConsonants) {
                foreach (var vowel in new[] { "и" }) {
                    alias = alias.Replace(consonant + vowel, consonant + "ы");
                }
            }
            foreach (var consonant in new[] { "ж", "ц", "ш", "ч", "щ" }) {
                foreach (var vowel in new[] { "э" }) {
                    alias = alias.Replace(consonant + "э", consonant + "е");
                }
            }
            foreach (var consonant in new[] { "`" }) {
                alias = alias.Replace(consonant, "・");
            }
            return alias;
        }
    }
}

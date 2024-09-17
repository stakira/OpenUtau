using OpenUtau.Api;
using OpenUtau.Core.G2p;

namespace OpenUtau.Core.DiffSinger {
    [Phonemizer("DiffSinger Italian Phonemizer", "DIFFS IT", language: "IT")]
    public class DiffSingerItalianPhonemizer : DiffSingerG2pPhonemizer {
        protected override string GetDictionaryName() => "dsdict-it.yaml";
        protected override string GetLangCode()=>"it";
        protected override IG2p LoadBaseG2p() => new ItalianG2p();
        protected override string[] GetBaseG2pVowels() => new string[] {
            "a", "a1", "e", "e1", "EE", "i", "i1", "o", "o1", "OO", "u", "u1"
        };

        protected override string[] GetBaseG2pConsonants() => new string[] {
            "b", "d", "dz", "dZZ", "f", "g", "JJ", "k", "l", "LL", "m", "n",
            "nf", "ng", "p", "r", "rr", "s", "SS", "t", "ts", "tSS", "v", "w", "y", "z"
        };
    }
}

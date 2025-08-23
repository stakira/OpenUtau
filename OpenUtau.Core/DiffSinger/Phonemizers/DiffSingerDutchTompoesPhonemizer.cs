using OpenUtau.Api;
using OpenUtau.Core.G2p;

namespace OpenUtau.Core.DiffSinger
{
    [Phonemizer("DiffSinger Dutch Tompoes Phonemizer", "DIFFS NL TOMPOES", language: "NL")]
    public class DiffSingerDutchTompoesPhonemizer : DiffSingerG2pPhonemizer
    {
        protected override string GetDictionaryName()=> "dsdict-nl-tompoes.yaml";
        public override string GetLangCode()=>"nl";
        protected override IG2p LoadBaseG2p() => new DutchTompoesG2p();
        protected override string[] GetBaseG2pVowels() => new string[] {
            "a", "aa", "aai", "au", "e", "ee", "eeu", "ei",
            "er", "eu", "ex", "i", "ie", "ieu", "o", "oe",
            "oei", "oi", "oo", "u", "ui", "uu", "uw"
        };

        protected override string[] GetBaseG2pConsonants() => new string[] {
            "b", "d", "f", "g", "h", "j", "k", "l", "m", "n", "ng", "p", "r",
            "s", "sj", "t", "tj", "v", "w", "z", "q", "vf", "cl"
        };
    }
}

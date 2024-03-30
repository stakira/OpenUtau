using OpenUtau.Api;
using OpenUtau.Core.G2p;

namespace OpenUtau.Core.DiffSinger
{
    [Phonemizer("DiffSinger Phonemizer", "French", language: "DiffSinger")]
    public class DiffSingerFrenchPhonemizer : DiffSingerG2pPhonemizer
    {
        protected override string GetDictionaryName()=>"dsdict-fr.yaml";
        protected override IG2p LoadBaseG2p() => new MillefeuilleG2p();
        protected override string[] GetBaseG2pVowels() => new string[] {
            "ah", "eh", "ae", "ee", "oe", "ih", "oh", "oo", "ou",
            "uh", "en", "in", "on",
        };

        protected override string[] GetBaseG2pConsonants() => new string[] {
            "uy", "y", "w", "f", "k", "p", "s", "sh",
            "t", "h", "b", "d", "g", "l", "m", "n", "r", "v", "z", "j", "ng", "q"
        };
    }
}

using OpenUtau.Api;
using OpenUtau.Core.G2p;

namespace OpenUtau.Core.DiffSinger
{
    [Phonemizer("DiffSinger English X Phonemizer", "DIFFS EN X", language: "EN")]
    public class DiffSingerEnglishXPhonemizer : DiffSingerG2pPhonemizer
    {
        protected override string GetDictionaryName()=>"dsdict-enx.yaml";
        protected override IG2p LoadBaseG2p() => new ArpaXG2p();
        protected override string[] GetBaseG2pVowels() => new string[] {
            "aa", "ae", "ah", "ao", "aw", "ay", "ax", "eh", "er", 
            "ey", "ih", "iy", "ow", "oy", "uh", "uw"
        };

        protected override string[] GetBaseG2pConsonants() => new string[] {
            "b", "ch", "d", "dh", "dx", "f", "g", "hh", "jh", "k", "l", "m", "n", 
            "ng", "p", "r", "s", "sh", "t", "th", "v", "w", "y", "z", "zh"
        };
    }
}
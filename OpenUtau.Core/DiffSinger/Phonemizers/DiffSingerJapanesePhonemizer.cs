using System.Collections.Generic;

using OpenUtau.Api;
using OpenUtau.Core.G2p;

namespace OpenUtau.Core.DiffSinger {
    [Phonemizer("DiffSinger Japanese Phonemizer", "DIFFS JA", language: "JA")]
    public class DiffSingerJapanesePhonemizer : DiffSingerG2pPhonemizer {
        protected override string GetDictionaryName()=>"dsdict-ja.yaml";
        protected override IG2p LoadBaseG2p() => new JapaneseMonophoneG2p();
        protected override string[] GetBaseG2pVowels() => new string[] {
            "A", "AP", "E", "I", "N", "O", "SP", "U",
            "a", "e", "i", "o", "u"
        };

        protected override string[] GetBaseG2pConsonants() => new string[] {
            "b", "by", "d", "dy", "f", "g", "gw", "gy", "h", "hy", "j", "k",
            "kw", "ky", "m", "my", "n", "ny", "p", "py", "r", "ry", "s", "sh",
            "t", "ts", "ty", "v", "w", "y", "z"
        };
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.G2p;

namespace OpenUtau.Core.DiffSinger {
    [Phonemizer("DiffSinger Japanese Phonemizer", "DIFFS JA", language: "JA")]
    public class DiffSingerJapanesePhonemizer : DiffSingerG2pPhonemizer {
        protected override string GetDictionaryName()=>"dsdict-ja.yaml";
        protected override string GetLangCode()=>"ja";
        protected override IG2p LoadBaseG2p() => new JapaneseMonophoneG2p();
        protected override string[] GetBaseG2pVowels() => new string[] {
            "A", "AP", "E", "I", "N", "O", "SP", "U",
            "a", "e", "i", "o", "u"
        };

        protected override string[] GetBaseG2pConsonants() => new string[] {
            "b", "by", "ch", "cl", "d", "dy", "f", "g", "gw", "gy", "h", "hy",
            "j", "k", "kw", "ky", "m", "my", "n", "ng", "ngy", "ny", "p", "py",
            "r", "ry", "s", "sh", "t", "ts", "ty", "v", "w", "y", "z"
        };

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevs) {
            if (notes[0].lyric == "-") {
                return MakeSimpleResult("SP");
            }
            if (!partResult.TryGetValue(notes[0].position, out var phonemes)) {
                throw new Exception("Part result not found");
            }
            return new Result {
                phonemes = phonemes
                    .Select((tu) => new Phoneme() {
                        phoneme = tu.Item1,
                        position = tu.Item2,
                    })
                    .ToArray(),
            };
        }
    }
}

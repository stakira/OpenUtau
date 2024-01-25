using System.Collections.Generic;
using OpenUtau.Api;
using OpenUtau.Core.G2p;
using System.Linq;

namespace OpenUtau.Core.DiffSinger {
    [Phonemizer("DiffSinger Jyutping Phonemizer", "DIFFS ZH-YUE", language: "ZH")]
    public class DiffSingerJyutpingPhonemizer : DiffSingerBasePhonemizer {
        protected override string[] Romanize(IEnumerable<string> lyrics) {
            return ZhG2p.CantoneseInstance.Convert(lyrics.ToList(), false, true).Split(" ");
        }
    }
}

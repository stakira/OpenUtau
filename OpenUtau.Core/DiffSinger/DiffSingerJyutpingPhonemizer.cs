using System.Collections.Generic;
using OpenUtau.Api;
using G2p;
using System.Linq;

namespace OpenUtau.Core.DiffSinger {
    [Phonemizer("DiffSinger Jyutping Phonemizer", "DIFFS ZH-YUE", language: "ZH")]
    public class DiffSingerJyutpingPhonemizer : DiffSingerBasePhonemizer {
        protected override string[] Romanize(IEnumerable<string> lyrics) {
            var YueG2p = new ZhG2p("cantonese");
            return YueG2p.Convert(lyrics.ToList(), false, true).Split(" ");
        }
    }
}

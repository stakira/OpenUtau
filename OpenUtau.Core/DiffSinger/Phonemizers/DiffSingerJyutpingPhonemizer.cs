using System.Collections.Generic;
using OpenUtau.Api;
using OpenUtau.Core.G2p;
using System.Linq;

namespace OpenUtau.Core.DiffSinger {
    [Phonemizer("DiffSinger Phonemizer", "Cantonese")]
    public class DiffSingerJyutpingPhonemizer : DiffSingerBasePhonemizer {
        protected override string GetDictionaryName() => "dsdict-zh-yue.yaml";
        protected override string[] Romanize(IEnumerable<string> lyrics) {
            return ZhG2p.CantoneseInstance.Convert(lyrics.ToList(), false, true).Split(" ");
        }
    }
}

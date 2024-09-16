using System.Collections.Generic;
using System.Linq;
using IKg2p;
using OpenUtau.Api;

namespace OpenUtau.Core.DiffSinger {
    [Phonemizer("DiffSinger Phonemizer", "Cantonese", language: "DiffSinger")]
    public class DiffSingerJyutpingPhonemizer : DiffSingerBasePhonemizer {
        protected override string GetDictionaryName() => "dsdict-zh-yue.yaml";
        protected override string GetLangCode()=>"yue";
        protected override string[] Romanize(IEnumerable<string> lyrics) {
            List<G2pRes> g2pResults = ZhG2p.CantoneseInstance.Convert(lyrics.ToList(), false, false);
            return g2pResults.Select(res => res.syllable).ToArray();
        }
    }
}

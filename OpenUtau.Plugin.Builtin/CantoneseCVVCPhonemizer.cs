using System.Collections.Generic;
using System.Linq;
using IKg2p;
using OpenUtau.Api;

namespace OpenUtau.Plugin.Builtin {
    /// <summary>
    /// Cantonese CVVC phonemizer.
    /// It works similarly to the Chinese CVVC phonemizer, including presamp.ini requirement.
    /// The big difference is that it converts hanzi to jyutping instead of pinyin.
    /// </summary>
    [Phonemizer("Cantonese CVVC Phonemizer", "ZH-YUE CVVC", "Lotte V", language: "ZH-YUE")]
    public class CantoneseCVVCPhonemizer : ChineseCVVCPhonemizer {
        protected override string[] Romanize(IEnumerable<string> lyrics) {
            List<G2pRes> g2pResults = ZhG2p.CantoneseInstance.Convert(lyrics.ToList(), false, false);
            return g2pResults.Select(res => res.syllable).ToArray();
        }
    }
}

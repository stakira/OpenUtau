using System.Collections.Generic;

using OpenUtau.Api;

namespace OpenUtau.Core.DiffSinger {
    [Phonemizer("DiffSinger Chinese Phonemizer", "DIFFS ZH", language: "ZH")]
    public class DiffSingerChinesePhonemizer : DiffSingerBasePhonemizer {
        protected override string GetDictionaryName()=>"dsdict-zh.yaml";
        protected override string GetLangCode()=>"zh";
        protected override string[] Romanize(IEnumerable<string> lyrics) {
            return BaseChinesePhonemizer.Romanize(lyrics);
        }
    }
}

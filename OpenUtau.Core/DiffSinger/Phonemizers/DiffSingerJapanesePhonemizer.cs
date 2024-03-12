using System.Collections.Generic;

using OpenUtau.Api;

namespace OpenUtau.Core.DiffSinger {
    [Phonemizer("DiffSinger Japanese Phonemizer", "DIFFS JA", language: "JA")]
    public class DiffSingerJapanesePhonemizer : DiffSingerBasePhonemizer {
        protected override string GetDictionaryName()=>"dsdict-ja.yaml";
    }
}

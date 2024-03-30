using System.Collections.Generic;

using OpenUtau.Api;

namespace OpenUtau.Core.DiffSinger {
    [Phonemizer("DiffSinger Phonemizer", "Japanese", language: "DiffSinger")]
    public class DiffSingerJapanesePhonemizer : DiffSingerBasePhonemizer {
        protected override string GetDictionaryName()=>"dsdict-ja.yaml";
    }
}

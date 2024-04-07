using OpenUtau.Api;

namespace OpenUtau.Core.DiffSinger {
    [Phonemizer("DiffSinger Korean Phonemizer", "DIFFS KO", language: "KO", author: "EX3")]
    public class DiffSingerKoreanPhonemizer : DiffSingerBasePhonemizer {
        protected override string GetDictionaryName() => "dsdict-ko.yaml";
    }
}
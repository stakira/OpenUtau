using System.Collections.Generic;

using OpenUtau.Api;

namespace OpenUtau.Core.DiffSinger
{
    [Phonemizer("DiffSinger Phonemizer", "Chinese", language: "DiffSinger")]
    public class DiffSingerChinesePhonemizer : DiffSingerBasePhonemizer
    {
        protected override string[] Romanize(IEnumerable<string> lyrics) {
            return BaseChinesePhonemizer.Romanize(lyrics);
        }
    }
}

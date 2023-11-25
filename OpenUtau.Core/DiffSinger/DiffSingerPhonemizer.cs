using System.Collections.Generic;

using OpenUtau.Api;

namespace OpenUtau.Core.DiffSinger
{
    [Phonemizer("DiffSinger Phonemizer", "DIFFS")]
    public class DiffSingerPhonemizer : DiffSingerBasePhonemizer
    {
        protected override string[] Romanize(IEnumerable<string> lyrics) {
            return BaseChinesePhonemizer.Romanize(lyrics);
        }
    }
}

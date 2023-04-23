using OpenUtau.Api;
using OpenUtau.Plugin.Builtin;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Plugins {
    public class EnDelta2Test : PhonemizerTestBase {
        public EnDelta2Test(ITestOutputHelper output) : base(output) { }
        protected override Phonemizer CreatePhonemizer() {
            return new ENDeltaVer2Phonemizer();
        }

        [Theory]
        [InlineData("en_delta7",
            new string[] { "my", "test" },
            new string[] { "C4", "C4" },
            new string[] { "", "", },
            new string[] { "ma", "I", "I t", "tE", "E s", "s t-" })]
        public void BasicPhonemizingTest(string singerName, string[] lyrics, string[] tones, string[] colors, string[] aliases) {
            RunPhonemizeTest(singerName, lyrics, tones, colors, aliases);
        }
    }
}

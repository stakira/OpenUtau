using OpenUtau.Api;
using OpenUtau.Plugin.Builtin;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Plugins {
    public class EnDelta1Test : PhonemizerTestBase {
        public EnDelta1Test(ITestOutputHelper output) : base(output) { }
        protected override Phonemizer CreatePhonemizer() {
            return new ENDeltaVer1Phonemizer();
        }

        [Theory]
        [InlineData("en_delta0",
            new string[] { "my", "test" },
            new string[] { "C4", "C4" },
            new string[] { "", "", },
            new string[] { "- maI", "aI t", "tE", "E st-" })]
        public void BasicPhonemizingTest(string singerName, string[] lyrics, string[] tones, string[] colors, string[] aliases) {
            RunPhonemizeTest(singerName, lyrics, tones, colors, aliases);
        }
    }
}

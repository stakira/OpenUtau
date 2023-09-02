using OpenUtau.Api;
using OpenUtau.Plugin.Builtin;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Plugins {
    public class EnXSampaTest : PhonemizerTestBase {
        public EnXSampaTest(ITestOutputHelper output) : base(output) { }
        protected override Phonemizer CreatePhonemizer() {
            return new EnXSampaPhonemizer();
        }

        [Theory]
        [InlineData("en_delta0",
            new string[] { "my", "test" },
            new string[] { "", "" },
            new string[] { "C4", "C4" },
            new string[] { "", "", },
            new string[] { "- maI", "aI t", "tE", "E st-" })]
        public void BasicPhonemizingTest(string singerName, string[] lyrics, string[] alts, string[] tones, string[] colors, string[] aliases) {
            RunPhonemizeTest(singerName, lyrics, alts, tones, colors, aliases);
        }
    }
}

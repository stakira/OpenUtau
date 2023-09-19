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
            new string[] { "- maI", "aI t", "tE", "E st-" })]
        public void BasicPhonemizingTest(string singerName, string[] lyrics, string[] aliases) {
            SameAltsTonesColorsTest(singerName, lyrics, aliases, "", "C4", "");
        }
    }
}

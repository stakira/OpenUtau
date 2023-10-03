using OpenUtau.Api;
using OpenUtau.Plugin.Builtin;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Plugins {
    public class EnVCCVTest : PhonemizerTestBase {
        public EnVCCVTest(ITestOutputHelper output) : base(output) { }
        protected override Phonemizer CreatePhonemizer() {
            return new EnglishVCCVPhonemizer();
        }

        [Theory]
        [InlineData("en_vccv",
            new string[] { "test", "words" },
            new string[] { "-te", "es-", "st", "w3", "3d-", "dz-" })]
        public void BasicPhonemizingTest(string singerName, string[] lyrics, string[] aliases) {
            SameAltsTonesColorsTest(singerName, lyrics, aliases, "", "C4", "");
        }
    }
}

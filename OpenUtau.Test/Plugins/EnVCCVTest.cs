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
            new string[] { "C4", "C4" },
            new string[] { "", "", },
            new string[] { "-te", "e st", "tw", "w3", "3 d", "dz-" })]
        public void BasicPhonemizingTest(string singerName, string[] lyrics, string[] tones, string[] colors, string[] aliases) {
            RunPhonemizeTest(singerName, lyrics, tones, colors, aliases);
        }
    }
}

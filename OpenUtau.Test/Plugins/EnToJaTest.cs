using OpenUtau.Api;
using OpenUtau.Plugin.Builtin;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Plugins {
    public class EnToJaTest : PhonemizerTestBase {
        public EnToJaTest(ITestOutputHelper output) : base(output) { }
        protected override Phonemizer CreatePhonemizer() {
            return new ENtoJAPhonemizer();
        }

        [Theory]
        [InlineData("ja_cvvc",
            new string[] { "test", "words" },
            new string[] { "C3", "C3" },
            new string[] { "", "", },
            new string[] { "て_A3", "e s_A3", "と_A3", "うぉ_A3", "o d_A3", "ず_A3" })]
        public void BasicPhonemizingTest(string singerName, string[] lyrics, string[] tones, string[] colors, string[] aliases) {
            RunPhonemizeTest(singerName, lyrics, tones, colors, aliases);
        }

        [Theory]
        [InlineData("ja_cvvc",
            new string[] { "test", "words" },
            new string[] { "C3", "C4" },
            new string[] { "", "", },
            new string[] { "て_A3", "e s_A3", "と_A3", "うぉ_C4", "o d_C4", "ず_C4" })]
        public void MultipitchTest(string singerName, string[] lyrics, string[] tones, string[] colors, string[] aliases) {
            RunPhonemizeTest(singerName, lyrics, tones, colors, aliases);
        }

        [Theory]
        [InlineData("ja_cvvc",
            new string[] { "test", "words" },
            new string[] { "C4", "C4" },
            new string[] { "", "強", },
            new string[] { "て_C4", "e s_強B3", "と_C4", "うぉ_C4", "o d_C4", "ず_C4" })]
        // Colors are per-phoneme
        public void VoiceColorTest(string singerName, string[] lyrics, string[] tones, string[] colors, string[] aliases) {
            RunPhonemizeTest(singerName, lyrics, tones, colors, aliases);
        }
    }
}

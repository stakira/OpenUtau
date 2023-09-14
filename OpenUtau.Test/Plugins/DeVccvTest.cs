using OpenUtau.Api;
using OpenUtau.Plugin.Builtin;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Plugins {
    public class DeVccvTest : PhonemizerTestBase {
        public DeVccvTest(ITestOutputHelper output) : base(output) { }

        protected override Phonemizer CreatePhonemizer() {
            return new GermanVCCVPhonemizer();
        }

        [Theory]
        [InlineData("de_vccv",
            new string[] { "guten", "Tag" },
            new string[] { "", "" },
            new string[] { "A2", "A2" },
            new string[] { "", "" },
            new string[] { "- guA2", "u tA2", "t@A2", "@n", "ntA2", "taA2", "akA2" })]
        [InlineData("de_vccv",
            new string[] { "guten", "+", "Tag" },
            new string[] { "", "", "" },
            new string[] { "D3", "G3", "D3" },
            new string[] { "", "", "" },
            new string[] { "- guD3", "u tD3", "t@G3", "@nG3", "ntG3", "taD3", "akD3" })]
        [InlineData("de_vccv",
            new string[] { "Mond", "+", "+", "+", "Licht", "+" },
            new string[] { "", "", "", "", "", "" },
            new string[] { "G3", "D3", "G3", "G3", "D3", "G3" },
            new string[] { "", "", "", "", "", "" },
            new string[] { "- moG3", "onG3", "nt -G3", "t lG3", "lID3", "ICG3", "Ct -G3" })]
        public void PhonemizeTest(string singerName, string[] lyrics, string[] alts, string[] tones, string[] colors, string[] aliases) {
            RunPhonemizeTest(singerName, lyrics, alts, tones, colors, aliases);
        }
    }
}

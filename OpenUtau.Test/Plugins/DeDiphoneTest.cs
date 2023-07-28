using OpenUtau.Api;
using OpenUtau.Plugin.Builtin;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Plugins {
    public class DeDiphoneTest : PhonemizerTestBase {
        public DeDiphoneTest(ITestOutputHelper output) : base(output) { }

        protected override Phonemizer CreatePhonemizer() {
            return new GermanDiphonePhonemizer();
        }

        [Theory]
        [InlineData("de_diphone",
            new string[] { "guten", "Tag" },
            new string[] { "", "" },
            new string[] { "C4", "C4" },
            new string[] { "", "" },
            new string[] { "- g_C4", "g uw_C4", "uw t_C4", "t ax_C4", "ax n_C4", "n t", "t aa_C4", "aa k_C4", "k -_C4" })]
        [InlineData("de_diphone",
            new string[] { "guten", "+", "Tag" },
            new string[] { "", "", "" },
            new string[] { "F3", "F4", "C4" },
            new string[] { "", "", "" },
            new string[] { "- g_F3", "g uw_F3", "uw t_F4", "t ax_F4", "ax n_F4", "n t", "t aa_C4", "aa k_C4", "k -_C4" })]
        [InlineData("de_diphone",
            new string[] { "Mond", "+", "+", "+", "Licht", "+" },
            new string[] { "", "", "", "", "", "" },
            new string[] { "F4", "C4", "F4", "F4", "C4", "F4" },
            new string[] { "", "", "", "", "", "" },
            new string[] { "- m_F4", "m ooh_F4", "ooh n_F4", "n t", "t l", "l ih_C4", "ih cc_F4", "cc t", "t -_F4" })]
        public void PhonemizeTest(string singerName, string[] lyrics, string[] alts, string[] tones, string[] colors, string[] aliases) {
            RunPhonemizeTest(singerName, lyrics, alts, tones, colors, aliases);
        }
    }
}

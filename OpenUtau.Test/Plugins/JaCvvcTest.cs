using OpenUtau.Api;
using OpenUtau.Plugin.Builtin;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Plugins {
    public class JaCvvcTest : PhonemizerTestBase {
        public JaCvvcTest(ITestOutputHelper output) : base(output) { }

        protected override Phonemizer CreatePhonemizer() {
            return new JapaneseCVVCPhonemizer();
        }

        [Theory]
        [InlineData("ja_cvvc",
            new string[] { "あ", "+", "あ", "-" },
            new string[] { "C4", "C4", "C4", "C4" },
            new string[] { "", "", "弱", "" },
            new string[] { "- あ_C4", "a あ_弱C4", "-" })]
        [InlineData("ja_cvvc",
            new string[] { "お", "にょ", "ひょ", "みょ", "びょ", "ぴょ", "りょ" },
            new string[] { "A3", "C4", "D4", "E4", "F4", "G3", "F3" },
            new string[] { "", "弱", "", "", "強", "", "" },
            new string[] { "- お_A3", "o ny_A3", "にょ_弱C4", "o hy_弱C4", "ひょ_C4", "o my_C4", "みょ_F4", "o by_F4", "びょ_強F4", "o py_強F4", "ぴょ_A3", "o ry_A3", "りょ_A3" })]
        [InlineData("ja_cvvc",
            new string[] { "ラ", "リ", "ル", "ら" },
            new string[] { "C4", "C4", "C4", "C4" },
            new string[] { "", "", "", "" },
            new string[] { "ラ_C4", "a ly_C4", "リ_C4", "i l_C4", "ル_C4", "u r_C4", "ら_C4" })]
        public void PhonemizeTest(string singerName, string[] lyrics, string[] tones, string[] colors, string[] aliases) {
            RunPhonemizeTest(singerName, lyrics, tones, colors, aliases);
        }
    }
}

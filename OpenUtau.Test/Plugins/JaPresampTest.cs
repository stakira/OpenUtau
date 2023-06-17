using OpenUtau.Api;
using OpenUtau.Plugin.Builtin;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Plugins {
    public class JaPresampTest : PhonemizerTestBase {
        public JaPresampTest(ITestOutputHelper output) : base(output) { }

        protected override Phonemizer CreatePhonemizer() {
            return new JapanesePresampPhonemizer();
        }

        [Theory]
        [InlineData("ja_presamp",
            new string[] { "あ", "+", "あ", "-" },
            new string[] { "", "", "", "" },
            new string[] { "C4", "C4", "C4", "C4" },
            new string[] { "", "", "波", "" },
            new string[] { "- あ_D4", "a あ波_D4", "a -_D4" })]
        [InlineData("ja_presamp",
            new string[] { "お", "にょ", "ひょ", "みょ", "びょ", "ぴょ", "りょ", "R" },
            new string[] { "", "", "", "", "", "", "", "" },
            new string[] { "A3", "C4", "D4", "E4", "F4", "G4", "A4", "A4" },
            new string[] { "", "", "波", "星", "星", "貝", "貝", "貝" },
            new string[] { "- お_A3", "o にょ_D4", "o C_D4", "ひょ波_D4", "o みょ星_E4", "o びょ星_E4", "o p'星_E4", "ぴょ貝_F4", "o 4'貝_F4", "りょ貝_A4", "o R貝_A4" })]
        [InlineData("ja_presamp",
            new string[] { "- ず", "u t", "と", "お・", "o R" },
            new string[] { "", "", "", "", "" },
            new string[] { "A3", "A3", "C4", "D4", "D4" },
            new string[] { "", "", "", "", "" },
            new string[] { "- ず_A3", "u t_A3", "と_D4", "o ・_D4", "・ お_D4",  "o R_D4" })]
        [InlineData("ja_presamp",
            new string[] { "ri", "p", "re", "i", "s" }, // [PRIORITY] p,s
            new string[] { "", "", "", "", "" },
            new string[] { "C4", "C4", "C4", "C4", "C4" },
            new string[] { "", "", "", "", "" },
            new string[] { "- り_D4", "i p_D4", "p", "れ_D4", "e い_D4", "i s_D4", "s" })]
        [InlineData("ja_presamp",
            new string[] { "\u304c", "\u304b\u3099", "\u30f4", "\u30a6\u3099" }, // が, が, ヴ, ヴ
            new string[] { "", "", "", "" },
            new string[] { "A3", "C4", "D4", "E4" },
            new string[] { "", "", "", "" },
            new string[] { "- が_A3", "a が_D4", "a ヴ_D4", "u ヴ_F4" })]
        public void PhonemizeTest(string singerName, string[] lyrics, string[] alts, string[] tones, string[] colors, string[] aliases) {
            RunPhonemizeTest(singerName, lyrics, alts, tones, colors, aliases);
        }
    }
}

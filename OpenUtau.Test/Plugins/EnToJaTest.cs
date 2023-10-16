using System.Xml.Linq;
using System;
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
            new string[] { "て_C4", "e s_C4", "と_C4", "うぉ_C4", "o d_C4", "ず_C4" })]
        [InlineData("ja_vcv",
            new string[] { "- てA3", "e すA3", "u とA3", "o うぉA3", "o どA3", "o ずA3" })]
        [InlineData("ja_cv",
            new string[] { "て", "す", "と", "を", "ど", "ず" })]
        public void BasicPhonemizingTest(string singerName, string[] aliases) {
            SameAltsTonesColorsTest(singerName, aliases,
                new string[] { "test", "words" });
        }

        [Theory]
        [InlineData("ja_cvvc",
            new string[] { "C3", "C4" },
            new string[] { "て_A3", "e s_A3", "と_A3", "うぉ_C4", "o d_C4", "ず_C4" })]
        [InlineData("ja_vcv",
            new string[] { "C4", "C5" },
            new string[] { "- てA3", "e すA3", "u とA3", "o うぉC5", "o どC5", "o ずC5" })]
        public void MultipitchTest(string singerName, string[] tones, string[] aliases) {
            RunPhonemizeTest(singerName, new string[] { "test", "words" },
                RepeatString(2, ""), tones, RepeatString(2, ""), aliases);
        }

        [Theory]
        [InlineData("ja_cvvc",
            new string[] { "", "強", },
            new string[] { "て_C4", "e s_強B3", "と_C4", "うぉ_C4", "o d_C4", "ず_C4" })]
        [InlineData("ja_vcv",
            new string[] { "", "Clear", },
            new string[] { "- てA3", "e すCA3", "u とA3", "o うぉA3", "o どA3", "o ずA3" })]
        [InlineData("ja_presamp",
            new string[] { "", "星", },
            new string[] { "- て_D4", "e s星_B3", "と_D4", "o うぉ_D4", "o d_D4", "ず_D4" })]
        // Colors are per-phoneme
        public void VoiceColorTest(string singerName, string[] colors, string[] aliases) {
            RunPhonemizeTest(singerName, new string[] { "test", "words" },
                RepeatString(2, ""), RepeatString(2, "C4"), colors, aliases);
        }

        [Theory]
        [InlineData("ja_cvvc",
            new string[] { "て_C4", "e s_C4", "て_C4", "e n_C4" })]
        [InlineData("ja_vcv",
            new string[] { "- てA3", "e すA3", "u てA3", "e んA3", "n RA3" })]
        [InlineData("ja_cv",
            new string[] { "て", "す", "て", "ん" })]
        [InlineData("ja_presamp",
            new string[] { "- て_D4", "e s_D4", "て_D4", "e n_D4" })]
        // Should have one て only, not become てえ
        public void SyllableExtendTest(string singerName, string[] aliases) {
            SameAltsTonesColorsTest(singerName, aliases,
                new string[] { "testing", "+*", "+", "+" });
        }

        [Theory]
        [InlineData("ja_cvvc",
            new string[] { "- お_C4", "o ry_C4", "りゅ_C4", "u ky_C4", "きゅ_C4", "u ts_C4", "つ_C4", "u n_C4" })]
        [InlineData("ja_vcv",
            new string[] { "- おA3", "o りゅA3", "u きゅA3", "u つA3", "u んA3", "n RA3" })]
        [InlineData("ja_cv",
            new string[] { "お", "りゅ", "きゅ", "つ", "ん" })]
        [InlineData("ja_presamp",
            new string[] { "- お_D4", "o りゅ_D4", "u きゅ_D4", "u つ_D4", "u n_D4" })] // preferring VCV with same tone/color
        public void SyllableSpecialClusterTest(string singerName, string[] aliases) {
            SameAltsTonesColorsTest(singerName, aliases,
                new string[] { "all", "you", "cute", "soon" });
        }

        [Theory]
        [InlineData("ja_cvvc",
            new string[] { "- あ_C4", "a s_C4", "た_C4", "a w_C4" })]
        [InlineData("ja_vcv",
            new string[] { "- あA3", "a すA3", "u たA3", "a うA3" })]
        [InlineData("ja_cv",
            new string[] { "あ", "す", "た", "う" })]
        [InlineData("ja_presamp",
            new string[] { "- あ_D4", "a s_D4", "た_D4", "a w_D4" })]
        public void SyllableClusterTest(string singerName, string[] aliases) {
            SameAltsTonesColorsTest(singerName, aliases,
                new string[] { "a", "star" });
        }

        [Theory]
        [InlineData("ja_cvvc",
            new string[] { "ヴ_C4" })]
        [InlineData("ja_vcv",
            new string[] { "- ヴA3", "u RA3" })]
        [InlineData("ja_cv",
            new string[] { "ふ" })]
        [InlineData("ja_presamp",
            new string[] { "- ヴ_D4", "u R_D4" })]
        public void SyllableConditionalAltTest(string singerName, string[] aliases) {
            SameAltsTonesColorsTest(singerName, aliases,
                new string[] { "vu" });
        }

        [Theory]
        [InlineData("ja_cvvc",
            new string[] { "てぃ_C4" })]
        [InlineData("ja_vcv",
            new string[] { "- てぃA3", "i RA3" })]
        [InlineData("ja_cv",
            new string[] { "て", "い" })]
        [InlineData("ja_presamp",
            new string[] { "- てぃ_D4", "i R_D4" })]
        public void SyllableExtraCvTest(string singerName, string[] aliases) {
            SameAltsTonesColorsTest(singerName, aliases,
                new string[] { "tea" });
        }

        [Theory]
        [InlineData("ja_cvvc",
            new string[] { "- あ_C4" })]
        [InlineData("ja_vcv",
            new string[] { "- あA3", "a RA3" })]
        [InlineData("ja_cv",
            new string[] { "あ" })]
        [InlineData("ja_presamp",
            new string[] { "- あ_D4", "a R_D4" })]
        public void EndingVowelTest(string singerName, string[] aliases) {
            SameAltsTonesColorsTest(singerName, aliases,
                new string[] { "a" });
        }

        [Theory]
        [InlineData("ja_cvvc",
            new string[] { "- え_C4", "e n_C4" })]
        [InlineData("ja_vcv",
            new string[] { "- えA3", "e んA3", "n RA3" })]
        [InlineData("ja_cv",
            new string[] { "え", "ん" })]
        [InlineData("ja_presamp",
            new string[] { "- え_D4", "e n_D4" })]
        public void EndingNasalTest(string singerName, string[] aliases) {
            SameAltsTonesColorsTest(singerName, aliases,
                new string[] { "an" });
        }

        [Theory]
        [InlineData("ja_cvvc",
            new string[] { "ば_C4", "a w_C4", "- ん_C4" })]
        [InlineData("ja_vcv",
            new string[] { "- ばA3", "a うA3", "u んA3", "n RA3" })]
        [InlineData("ja_cv",
            new string[] { "ば", "う", "ん" })]
        [InlineData("ja_presamp",
            new string[] { "- ば_D4", "a w_D4", "- ん_D4", "n R_D4" })] // TODO: make this nicer
        public void EndingClusterNasalTest(string singerName, string[] aliases) {
            SameAltsTonesColorsTest(singerName, aliases,
                new string[] { "barn" });
        }

        [Theory]
        [InlineData("ja_cvvc",
            new string[] { "- あ_C4", "a p_C4" })]
        [InlineData("ja_vcv",
            new string[] { "- あA3", "a ぷA3" })]
        [InlineData("ja_cv",
            new string[] { "あ", "ぷ" })]
        [InlineData("ja_presamp",
            new string[] { "- あ_D4", "a p_D4" })]
        public void EndingSingleConsonantTest(string singerName, string[] aliases) {
            SameAltsTonesColorsTest(singerName, aliases,
                new string[] { "up" });
        }

        [Theory]
        [InlineData("ja_cvvc",
            new string[] { "- あ_C4", "a v_C4" })]
        [InlineData("ja_vcv",
            new string[] { "- あA3", "a ヴA3" })]
        [InlineData("ja_cv",
            new string[] { "あ", "ふ" })]
        [InlineData("ja_presamp",
            new string[] { "- あ_D4", "a v_D4" })]
        public void EndingConditionalAltTest(string singerName, string[] aliases) {
            SameAltsTonesColorsTest(singerName, aliases,
                new string[] { "of" });
        }

        [Theory]
        [InlineData("ja_cvvc",
            new string[] { "- あ_C4", "a n_C4", "ど_C4" })]
        [InlineData("ja_vcv",
            new string[] { "- あA3", "a んA3", "n どA3" })]
        [InlineData("ja_cv",
            new string[] { "あ", "ん", "ど" })]
        [InlineData("ja_presamp",
            new string[] { "- あ_D4", "a n_D4", "ど_D4" })]
        public void EndingClusterTest(string singerName, string[] aliases) {
            SameAltsTonesColorsTest(singerName, aliases,
                new string[] { "and" });
        }

        [Theory]
        [InlineData("ja_cvvc",
            new string[] { "- え_C4", "e ts_C4", "つ_C4" })]
        [InlineData("ja_vcv",
            new string[] { "- えA3", "e つA3" })]
        [InlineData("ja_cv",
            new string[] { "え", "つ" })]
        [InlineData("ja_presamp",
            new string[] { "- え_D4", "e ts_D4", "つ_D4" })]
        public void EndingSpecialClusterTest(string singerName, string[] aliases) {
            SameAltsTonesColorsTest(singerName, aliases,
                new string[] { "its" });
        }

        [Theory]
        [InlineData("ja_vcv",
            new string[] { "its", "its" },
            new string[] { "- えA3", "e つぇA3", "e つA3" })]
        [InlineData("ja_vcv",
            new string[] { "itch", "itch" },
            new string[] { "- えA3", "e ちぇA3", "e ちゅA3" })]
        [InlineData("ja_vcv",
            new string[] { "age", "age" },
            new string[] { "- えA3", "e いA3", "i じぇA3", "e いA3", "i じゅA3" })]
        public void EndingVcvAffricateTest(string singerName, string[] lyrics, string[] aliases) {
            SameAltsTonesColorsTest(singerName, aliases, lyrics);
        }

        [Theory]
        [InlineData(
            new string[] { "a", "the" },
            new string[] { "- あ_C4", "a d_C4", "だ_C4" })]
        [InlineData(
            new string[] { "a", "thin" },
            new string[] { "- あ_C4", "a s_C4", "せ_C4", "e n_C4" })]
        [InlineData(
            new string[] { "a", "zha" },
            new string[] { "- あ_C4", "a sh_C4", "しゃ_C4" })]
        [InlineData(
            new string[] { "a", "ra" },
            new string[] { "- あ_C4", "a w_C4", "わ_C4" })]
        [InlineData(
            new string[] { "a", "la" },
            new string[] { "- あ_C4", "a r_C4", "ら_C4" })]
        [InlineData(
            new string[] { "all", "you" },
            new string[] { "- お_C4", "o ry_C4", "りゅ_C4" })]
        public void SyllableDigraphVCTest(string[] lyrics, string[] aliases) {
            SameAltsTonesColorsTest("ja_cvvc", aliases, lyrics);
        }

        [Theory]
        [InlineData(
            new string[] { "you", "too" },
            new string[] { "C4", "C4" },
            new string[] { "", "" },
            new string[] { "- ゆ_D4", "u とぅ_D4", "u R_D4" })] // same tone/color uses VCV
        [InlineData(
            new string[] { "you", "too" },
            new string[] { "C4", "C3" },
            new string[] { "", "" },
            new string[] { "- ゆ_D4", "u t_D4", "とぅ_A3", "u R_A3" })] // diff tone uses CVVC
        [InlineData(
            new string[] { "you", "too" },
            new string[] { "C4", "C4" },
            new string[] { "", "波" },
            new string[] { "- ゆ_D4", "u t波_D4", "とぅ_D4", "u R_D4" })] // diff color uses CVVC (test api misaligns colors, verify in UI)
        public void PreferVCVTest(string[] lyrics, string[] tones, string[] colors, string[] aliases) {
            RunPhonemizeTest("ja_presamp", lyrics, RepeatString(lyrics.Length, ""), tones, colors, aliases);
        }

        private void SameAltsTonesColorsTest(string singerName, string[] aliases, string[] lyrics) { 
            SameAltsTonesColorsTest(singerName, lyrics, aliases, "", "C4", "");
        }
    }
}

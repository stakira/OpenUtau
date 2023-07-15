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
            new string[] { "て_A3", "e s_A3", "と_A3", "うぉ_A3", "o d_A3", "ず_A3" })]
        [InlineData("ja_vcv",
            new string[] { "- てA3", "e すA3", "u とA3", "o うぉA3", "o どA3", "o ずA3" })]
        [InlineData("ja_cv",
            new string[] { "て", "す", "と", "を", "ど", "ず" })]
        public void BasicPhonemizingTest(string singerName, string[] aliases) {
            RunPhonemizeTest(singerName, new string[] { "test", "words" }, 
                RepeatString(2, ""), RepeatString(2, "C3"), RepeatString(2, ""), aliases);
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
            new string[] { "て","す","て","ん" })]
        // Should have one て only, not become てえ
        public void ExtendSyllableTest(string singerName, string[] aliases) {
            RunPhonemizeTest(singerName, new string[] { "testing", "+*", "+", "+" },
                RepeatString(4, ""), RepeatString(4, "C4"), RepeatString(4, ""), aliases);
        }

        [Theory]
        [InlineData("ja_cvvc",
            new string[] { "- お_C4", "o ry_C4", "りゅ_C4", "u ky_C4", "きゅ_C4", "u ts_C4", "つ_C4", "u n_C4" })]
        [InlineData("ja_vcv",
            new string[] { "- おA3", "o りゅA3", "u きゅA3", "u つA3", "u んA3", "n RA3" })]
        [InlineData("ja_cv",
            new string[] { "お", "りゅ", "きゅ", "つ", "ん" })]
        public void SpecialClusterTest(string singerName, string[] aliases) {
            RunPhonemizeTest(singerName, new string[] { "all", "you", "cute", "soon" },
                RepeatString(4, ""), RepeatString(4, "C4"), RepeatString(4, ""), aliases);
        }

        private string[] RepeatString(int count, string s) {
            string[] array = new string[count];
            Array.Fill(array, s);
            return array;
        }
    }
}

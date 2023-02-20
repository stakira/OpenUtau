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
        // Basic phonemizing
        [InlineData("ja_cvvc",
            new string[] { "test", "words" },
            new string[] { "C3", "C3" },
            new string[] { "", "", },
            new string[] { "place", "holder" })]
        //// Multipitch
        //[InlineData("ja_cvvc",
        //    new string[] { "test", "words" },
        //    new string[] { "C3", "C4" },
        //    new string[] { "", "", },
        //    new string[] { "place", "holder" })]
        //// Voice colors
        //[InlineData("ja_vcv",
        //    new string[] { "test", "words" },
        //    new string[] { "C3", "C3" },
        //    new string[] { "", "Power", },
        //    new string[] { "- t", "t eh", "eh s_P", "s t", "t w", "w er", "er d", "d z", "z -" })]
        public void PhonemizeTest(string singerName, string[] lyrics, string[] tones, string[] colors, string[] aliases) {
            RunPhonemizeTest(singerName, lyrics, tones, colors, aliases);
        }
    }
}

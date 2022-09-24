using OpenUtau.Api;
using OpenUtau.Plugin.Builtin;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Plugins {
    public class EnArpaTest : PhonemizerTestBase {
        public EnArpaTest(ITestOutputHelper output) : base(output) { }

        protected override Phonemizer CreatePhonemizer() {
            return new ArpasingPhonemizer();
        }

        [Theory]
        [InlineData("en_arpa",
            new string[] { "good", "morning" },
            new string[] { "C4", "C4" },
            new string[] { "", "" },
            new string[] { "- g_3", "g uh_3", "uh d_3", "d m_3", "m ao_3", "ao r_3", "r n_3", "n ih_3", "ih ng_3", "ng -_3" })]
        [InlineData("en_arpa",
            new string[] { "good", "morning", "-" },
            new string[] { "A3", "F4", "C4" },
            new string[] { "", "", "" },
            new string[] { "- g_3", "g uh_3", "uh d_3", "d m_3", "m ao", "ao r", "r n", "n ih", "ih ng", "ng -_3" })]
        [InlineData("en_arpa",
            new string[] { "moon", "+", "+", "+", "star", "+" },
            new string[] { "F4", "C4", "F4", "F4", "C4", "F4" },
            new string[] { "Whisper", "", "", "", "", "" },
            new string[] { "- m_W", "m uw", "uw n", "n s", "s t_3", "t aa_3", "aa r", "r -" })]
        public void PhonemizeTest(string singerName, string[] lyrics, string[] tones, string[] colors, string[] aliases) {
            RunPhonemizeTest(singerName, lyrics, tones, colors, aliases);
        }
    }
}

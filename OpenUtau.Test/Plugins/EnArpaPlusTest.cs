using OpenUtau.Api;
using OpenUtau.Plugin.Builtin;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Plugins {
    public class EnArpaPlusTest : PhonemizerTestBase {
        public EnArpaPlusTest(ITestOutputHelper output) : base(output) { }

        protected override Phonemizer CreatePhonemizer() {
            return new ArpasingPlusPhonemizer();
        }

        [Theory]
        [InlineData("en_arpa-plus",
            new string[] { "good", "morning", },
            new string[] { "A#3", "A#3" },
            new string[] { "", "" },
            new string[] { "- g_C3", "g uh_C3", "uh d_C3", "d m_C3", "m ao_C3", "ao r_C3", "r n_C3", "n ih_C3", "ih ng_C3", "ng -_C3" })]
        [InlineData("en_arpa-plus",
            new string[] { "good", "morning" },
            new string[] { "C3", "C3" },
            new string[] { "", "" },
            new string[] { "- g_C3", "g uh_C3", "uh d_C3", "d m_C3", "m ao_C3", "ao r_C3", "r n_C3", "n ih_C3", "ih ng_C3", "ng -_C3" })]
        public void PhonemizeTest(string singerName, string[] lyrics, string[] tones, string[] colors, string[] aliases) {
            RunPhonemizeTest(singerName, lyrics, RepeatString(lyrics.Length, ""), tones, colors, aliases);
        }

        [Fact]
        public void ColorTest() {
            RunPhonemizeTest("en_arpa-plus", new NoteParams[] {
                new NoteParams {
                    lyric = "hi",
                    hint = "",
                    tone = "A#3",
                    phonemes = new PhonemeParams[] {
                        new PhonemeParams {
                            alt = 0,
                            shift = 0,
                            color = "",
                        },
                        new PhonemeParams {
                            alt = 0,
                            shift = 0,
                            color = "Whisper",
                        },
                        new PhonemeParams {
                            alt = 0,
                            shift = 0,
                            color = "",
                        }
                    }
                }
            }, new string[] { "- hh_C3", "hh ay_W", "ay -_C3" });
        }
        public void SyllableTest(string lyric, string hint, string[] aliases) {
            RunPhonemizeTest("en_arpa-plus", new NoteParams[] { new NoteParams { lyric = lyric, hint = hint, tone = "C3", phonemes = SamePhonemeParams(4, 0, 0, "") } }, aliases);
        }
        [Theory]
        [InlineData("read", "", new string[] { "- r_C3", "r eh_C3", "eh d_C3", "d -_C3" })]
        [InlineData("read", "r iy d", new string[] { "- r_C3", "r iy_C3", "iy d_C3", "d -_C3" })]

        [InlineData("asdfjkl", "r iy d", new string[] { "- r_C3", "r iy_C3", "iy d_C3", "d -_C3" })]
        [InlineData("", "r iy d", new string[] { "- r_C3", "r iy_C3", "iy d_C3", "d -_C3" })]

        public void SyllableExternalEndingTest(string lyric, string hint, string[] aliases) {
            RunPhonemizeTest("en_arpa-plus", new NoteParams[] { new NoteParams { lyric = lyric, hint = hint, tone = "C3", phonemes = SamePhonemeParams(4, 0, 0, "") } }, aliases);
        }
        [Theory]
        [InlineData("more", "m aor", new string[] { "- m_C3", "m ao_C3", "ao r_C3", "r -_C3" })]
        [InlineData("'a", "q ax hh", new string[] { "- q_C3", "q ax_C3", "ax hh_C3", "hh -_C3" })]

        public void SyllableCCVTest(string lyric, string hint, string[] aliases) {
            RunPhonemizeTest("en_arpa-plus", new NoteParams[] { new NoteParams { lyric = lyric, hint = hint, tone = "C3", phonemes = SamePhonemeParams(4, 0, 0, "") } }, aliases);
        }
        [Theory]
        [InlineData("trusting", "", new string[] { "- tr_C3", "tr ah_C3", "ah st_C3", "st ih_C3", "ih ng_C3", "ng -_C3" })]
        [InlineData("drive", "", new string[] { "- dr_C3", "dr ay_C3", "ay v_C3", "v -_C3" })]

        public void SyllableFallbackTest(string lyric, string hint, string[] aliases) {
            RunPhonemizeTest("en_arpa-plus", new NoteParams[] { new NoteParams { lyric = lyric, hint = hint, tone = "C3", phonemes = SamePhonemeParams(4, 0, 0, "") } }, aliases);
        }
        [Theory]
        [InlineData("kroidroi", "", new string[] { "- kr_C3", "kr oy_C3", "iy dr_C3", "dr oy_C3", "oy -_C3" })]
        [InlineData("whhat", "",  new string[] { "- hh_C3", "hh uw_C3", "w ah_C3", "ah t_C3", "t -_C3" })]

        public void HintTest(string lyric, string hint, string[] aliases) {
            RunPhonemizeTest("en_arpa-plus", new NoteParams[] { new NoteParams { lyric = lyric, hint = hint, tone = "C3", phonemes = SamePhonemeParams(4, 0, 0, "")} }, aliases);
        }
    }
}

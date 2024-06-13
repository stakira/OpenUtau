using OpenUtau.Api;
using OpenUtau.Plugin.Builtin;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Plugins {
    public class EnVCCVTest : PhonemizerTestBase {
        public EnVCCVTest(ITestOutputHelper output) : base(output) { }
        protected override Phonemizer CreatePhonemizer() {
            return new EnglishVCCVPhonemizer();
        }

        [Theory]
        [InlineData("en_vccv",
            new string[] { "test", "words" },
            new string[] { "-te", "es-", "st", "w3", "3d-", "dz-" })]
        public void BasicPhonemizingTest(string singerName, string[] lyrics, string[] aliases) {
            SameAltsTonesColorsTest(singerName, lyrics, aliases, "", "C4", "");
        }

        [Fact]
        public void ToneShiftTest() {
            RunPhonemizeTest("en_vccv", new NoteParams[] {
                new NoteParams {
                    lyric = "hi",
                    hint = "",
                    tone = "C4",
                    phonemes = new PhonemeParams[] {
                        new PhonemeParams {
                            alt = 0,
                            shift = 0,
                            color = "",
                        },
                        new PhonemeParams {
                            alt = 0,
                            shift = 12,
                            color = "",
                        },
                    }
                }
            }, new string[] { "-hI", "I-_H" });
        }

        [Theory]
        [InlineData("read", "", new string[] { "-re", "ed-" })]
        [InlineData("read", "r E d", new string[] { "-rE", "Ed-" })]

        [InlineData("asdfjkl", "r E d", new string[] { "-rE", "Ed-" })]
        [InlineData("", "r E d", new string[] { "-rE", "Ed-" })]
        public void HintTest(string lyric, string hint, string[] aliases) {
            RunPhonemizeTest("en_vccv", new NoteParams[] { new NoteParams { lyric = lyric, hint = hint, tone = "C4", phonemes = SamePhonemeParams(4, 0, 0, "") } }, aliases);
        }
    }
}

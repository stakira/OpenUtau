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
            new string[] { "C4", "C4", "C4", "C4" },
            new string[] { "", "", "波", "" },
            new string[] { "- あ_D4", "a あ波_D4", "a -_D4" })]
        [InlineData("ja_presamp",
            new string[] { "- ず", "u t", "と", "お・", "o R" },
            new string[] { "A3", "A3", "C4", "D4", "D4" },
            new string[] { "", "", "", "", "" },
            new string[] { "- ず_A3", "u t_A3", "と_D4", "o ・_D4", "・ お_D4",  "o R_D4" })]
        [InlineData("ja_presamp",
            new string[] { "\u304c", "\u304b\u3099", "\u30f4", "\u30a6\u3099" }, // が, が, ヴ, ヴ
            new string[] { "A3", "C4", "D4", "E4" },
            new string[] { "", "", "", "" },
            new string[] { "- が_A3", "a が_D4", "a ヴ_D4", "u ヴ_F4" })]
        public void PhonemizeTest(string singerName, string[] lyrics, string[] tones, string[] colors, string[] aliases) {
            RunPhonemizeTest(singerName, lyrics, RepeatString(lyrics.Length, ""), tones, colors, aliases);
        }

        [Fact]
        public void VcColorTest() {
            RunPhonemizeTest("ja_presamp", new NoteParams[] {
                    new NoteParams {
                        lyric = "あ",
                        hint = "",
                        tone = "C4",
                        phonemes = new PhonemeParams[] {
                            new PhonemeParams {
                                color = "",
                                shift = 0,
                                alt = 0
                            },
                            new PhonemeParams {
                                color = "星",
                                shift = 0,
                                alt = 0
                            }
                        }
                    },
                    new NoteParams {
                        lyric = "k",
                        hint = "",
                        tone = "C4",
                        phonemes = SamePhonemeParams(1, 0, 0, "")
                    }
                },
                new string[] { "- あ_D4", "a k星_B3", "k" });
        }

        /// <summary>
        /// Second phoneme params are ignored here
        /// </summary>
        [Fact]
        public void VcvColorTest() {
            RunPhonemizeTest("ja_presamp", new NoteParams[] {
                    new NoteParams {
                        lyric = "あ",
                        hint = "",
                        tone = "C4",
                        phonemes = new PhonemeParams[] {
                            new PhonemeParams {
                                color = "",
                                shift = 0,
                                alt = 0
                            },
                            new PhonemeParams {
                                color = "星",
                                shift = 0,
                                alt = 0
                            }
                        }
                    },
                    new NoteParams {
                        lyric = "か",
                        hint = "",
                        tone = "C4",
                        phonemes = SamePhonemeParams(1, 0, 0, "")
                    }
                },
                new string[] { "- あ_D4", "a か_D4" });
        }

        [Theory]
        [InlineData("ja_presamp",
            new string[] { "ri", "p", "re", "i", "s" }, // [PRIORITY] p,s
            new string[] { "- り_D4", "i p_D4", "p", "れ_D4", "e い_D4", "i s_D4", "s" })]
        public void PriorityTest(string singerName, string[] lyrics, string[] aliases) {
            SameAltsTonesColorsTest(singerName, lyrics, aliases, "", "C4", "");
        }
    }
}

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

        // General
        [Fact]
        public void JaPlusMinusTest() {
            RunPhonemizeTest("ja_vcv_integration", new NoteParams[] {
                new NoteParams { lyric = "あ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "+", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "あ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "+~", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "R", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") }
            },
            ["- あ", "a あ", "a R"]);
        }
        [Fact]
        public void JaUnicordTest() { // が, が, ヴ, ヴ
            RunPhonemizeTest("ja_vcv_integration", new NoteParams[] {
                new NoteParams { lyric = "\u304c", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "\u304b\u3099", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "\u30f4", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "\u30a6\u3099", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") }
            },
            ["- が", "a が", "a ヴ", "u ヴ"]);
        }
        [Fact]
        public void JaPriorityTest() { // [PRIORITY] p
            RunPhonemizeTest("ja_cvvc_integration", new NoteParams[] {
                new NoteParams { lyric = "ri", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "p", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "re", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "i", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "s", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") }
            },
            ["り", "i p", "p", "れ", "e い", "i s"]);
        }

        // CV
        [Fact]
        public void CvTest() {
            RunPhonemizeTest("ja_cv_integration", new NoteParams[] {
                new NoteParams { lyric = "あ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "か", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "さ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "た", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "な", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") }
            },
            ["あ", "か", "さ", "た", "な"]);
        }
        [Fact]
        public void CvColorTest() {
            RunPhonemizeTest("ja_cv_integration", new NoteParams[] {
                new NoteParams { lyric = "あ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "か", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "さ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "cv") },
                new NoteParams { lyric = "た", hint = "", tone = "G4", phonemes = SamePhonemeParams(1, 0, 0, "cv") },
                new NoteParams { lyric = "な", hint = "", tone = "G4", phonemes = SamePhonemeParams(1, 0, 0, "cv") }
            },
            ["あ", "か", "さ_CV_C4", "た_CV_G4", "な_CV_G4"]);
        }
        [Fact]
        public void CvIntegrationTest() {
            RunPhonemizeTest("ja_cv_integration", new NoteParams[] {
                new NoteParams { lyric = "あ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "か", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "cvvc") },
                new NoteParams { lyric = "さ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "cvvc") },
                new NoteParams { lyric = "た", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "vcv") },
                new NoteParams { lyric = "な", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "cv") },
                new NoteParams { lyric = "R", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "cv") }
            },
            ["あ", "か_CVVC_C4", "a s_CVVC_C4", "さ_CVVC_C4", "a た_VCV_D4", "な_CV_C4", "R"]);
        }
        [Fact]
        public void CvGlottalTest() {
            RunPhonemizeTest("ja_cv_integration", new NoteParams[] {
                new NoteParams { lyric = "あ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "あ・", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "か", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "cvvc") },
                new NoteParams { lyric = "あ・", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "cvvc") },
                new NoteParams { lyric = "か", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "cvvc") },
                new NoteParams { lyric = "あ・", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "vcv") },
                new NoteParams { lyric = "た", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "vcv") },
                new NoteParams { lyric = "あ・", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "vcv") },
                new NoteParams { lyric = "R", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "vcv") }
            },
            ["あ", "あ", "か_CVVC_C4", "a ・_CVVC_C4", "・ あ_CVVC_C4", "a k_CVVC_C4", "か_CVVC_C4", "a あ・_VCV_D4", "a た_VCV_D4", "a あ・_VCV_D4", "a R_VCV_D4"]);
        }

        // CVVC
        [Fact]
        public void CvvcTest() {
            RunPhonemizeTest("ja_cvvc_integration", new NoteParams[] {
                new NoteParams { lyric = "あ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "あ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "っ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "か", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "さ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "a t", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "た", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "な", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") }
            },
            ["- あ", "a あ", "a k", "か", "a s", "さ", "a t", "た", "a n", "な"]);
        }
        [Fact]
        public void CvvcPreCTest() {
            RunPhonemizeTest("ja_cvvc_integration", new NoteParams[] {
                new NoteParams { lyric = "さ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "さ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") }
            },
            ["- s", "さ", "a s", "さ"]);
        }
        [Fact]
        public void CvvcColorTest() {
            RunPhonemizeTest("ja_cvvc_integration", new NoteParams[] {
                new NoteParams { lyric = "あ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "か", hint = "", tone = "C4",
                    phonemes = new PhonemeParams[] {
                        new PhonemeParams {
                            color = "",
                            shift = 0,
                            alt = 0
                        },
                        new PhonemeParams {
                            color = "cvvc",
                            shift = 0,
                            alt = 0
                        }
                    }
                },
                new NoteParams { lyric = "さ", hint = "", tone = "F4", phonemes = SamePhonemeParams(1, 0, 0, "cvvc") },
                new NoteParams { lyric = "た", hint = "", tone = "C4",
                    phonemes = new PhonemeParams[] {
                        new PhonemeParams {
                            color = "cvvc",
                            shift = 0,
                            alt = 0
                        },
                        new PhonemeParams {
                            color = "",
                            shift = 0,
                            alt = 0
                        }
                    }
                },
                new NoteParams { lyric = "な", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "cvvc") }
            },
            ["- あ", "a k", "か", "a s_CVVC_C4", "さ_CVVC_F4", "a t_CVVC_F4", "た_CVVC_C4", "a n", "な_CVVC_C4"]);
        }
        [Fact]
        public void CvvcIntegrationTest() {
            RunPhonemizeTest("ja_cvvc_integration", new NoteParams[] {
                new NoteParams { lyric = "あ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "か", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "cvvc") },
                new NoteParams { lyric = "さ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "cvvc") },
                new NoteParams { lyric = "た", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "vcv") },
                new NoteParams { lyric = "な", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "cv") },
                new NoteParams { lyric = "R", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") }
            },
            ["- あ", "a k", "か_CVVC_C4", "a s_CVVC_C4", "さ_CVVC_C4", "a た_VCV_D4", "な_CV_C4", "a R"]);
        }
        [Fact]
        public void CvvcGlottalTest() {
            RunPhonemizeTest("ja_cvvc_integration", new NoteParams[] {
                new NoteParams { lyric = "あ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "あ・", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "あ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "・", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "あ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "か", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "cvvc") },
                new NoteParams { lyric = "あ・", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "cvvc") },
                new NoteParams { lyric = "か", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "cvvc") },
                new NoteParams { lyric = "あ・", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "vcv") },
                new NoteParams { lyric = "た", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "vcv") },
                new NoteParams { lyric = "あ・", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "vcv") },
                new NoteParams { lyric = "R", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "vcv") }
            },
            ["- あ", "a ・", "・ あ", "a あ", "a ・", "・ あ", "a k", "か_CVVC_C4", "a ・_CVVC_C4", "・ あ_CVVC_C4", "a k_CVVC_C4", "か_CVVC_C4", "a あ・_VCV_D4", "a た_VCV_D4", "a あ・_VCV_D4", "a R_VCV_D4"]);
        }

        // VCV
        [Fact]
        public void VcvTest() {
            RunPhonemizeTest("ja_vcv_integration", new NoteParams[] {
                new NoteParams { lyric = "あ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "あ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "っ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "か", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "さ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "た", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "R", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") }
            },
            ["- あ", "a あ", "っ", "- か", "a さ", "a た", "a R"]);
        }
        [Fact]
        public void VcvColorTest() {
            RunPhonemizeTest("ja_vcv_integration", new NoteParams[] {
                new NoteParams { lyric = "あ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "か", hint = "", tone = "C4",
                    phonemes = new PhonemeParams[] {
                        new PhonemeParams {
                            color = "vcv",
                            shift = 0,
                            alt = 0
                        },
                        new PhonemeParams { // Second phoneme params are ignored here
                            color = "cvvc",
                            shift = 0,
                            alt = 0
                        }
                    }
                },
                new NoteParams { lyric = "さ", hint = "", tone = "F4", phonemes = SamePhonemeParams(1, 0, 0, "vcv") },
                new NoteParams { lyric = "た", hint = "", tone = "C4",
                    phonemes = new PhonemeParams[] {
                        new PhonemeParams {
                            color = "",
                            shift = 0,
                            alt = 0
                        },
                        new PhonemeParams {
                            color = "cvvc",
                            shift = 0,
                            alt = 0
                        }
                    }
                },
                new NoteParams { lyric = "な", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "vcv") }
            },
            ["- あ", "a か_VCV_D4", "a さ_VCV_D4", "a た", "a な_VCV_D4"]);
        }
        [Fact]
        public void VcvIntegrationTest() {
            RunPhonemizeTest("ja_vcv_integration", new NoteParams[] {
                new NoteParams { lyric = "あ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "か", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "cvvc") },
                new NoteParams { lyric = "さ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "た", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "vcv") },
                new NoteParams { lyric = "な", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "cv") },
                new NoteParams { lyric = "R", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") }
            },
            ["- あ", "か_CVVC_C4", "a さ", "a た_VCV_D4", "な_CV_C4", "a R"]);
        }
        [Fact]
        public void VcvGlottalTest() {
            RunPhonemizeTest("ja_vcv_integration", new NoteParams[] {
                new NoteParams { lyric = "あ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "あ・", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") },
                new NoteParams { lyric = "か", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "cvvc") },
                new NoteParams { lyric = "あ・", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "cvvc") },
                new NoteParams { lyric = "た", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "vcv") },
                new NoteParams { lyric = "あ・", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "vcv") },
                new NoteParams { lyric = "R", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "") }
            },
            ["- あ", "a あ・", "か_CVVC_C4", "a ・_CVVC_C4", "・ あ_CVVC_C4", "a た_VCV_D4", "a あ・_VCV_D4", "a R"]);
        }

        // X-SAMPA
        [Fact]
        public void CvvcXsampaTest() {
            RunPhonemizeTest("ja_presamp", new NoteParams[] {
                new NoteParams { lyric = "あ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "波") },
                new NoteParams { lyric = "あ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "波") },
                new NoteParams { lyric = "っ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "波") },
                new NoteParams { lyric = "ひゃ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "波") },
                new NoteParams { lyric = "さ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "波") },
                new NoteParams { lyric = "ちゃ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "波") },
                new NoteParams { lyric = "にゃ", hint = "", tone = "C4", phonemes = SamePhonemeParams(1, 0, 0, "波") }
            },
            ["- あ波_D4", "a あ波_D4", "a C波_D4", "ひゃ波_D4", "a s波_D4", "さ波_D4", "a tS波_D4", "ちゃ波_D4", "a J波_D4", "にゃ波_D4"]);
        }
    }
}

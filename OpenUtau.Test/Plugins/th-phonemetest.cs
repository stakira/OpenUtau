// Made And Checked By DELTA SYNTH & Gemini AI
// Original Concept By OpenUtau Contributors
// Version: 1.0

using OpenUtau.Api;
using OpenUtau.Plugin.Builtin;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Plugins {
    public class ThaiVCCVTest : PhonemizerTestBase {
        public ThaiVCCVTest(ITestOutputHelper output) : base(output) { }
        
        protected override Phonemizer CreatePhonemizer() {
            // เรียกใช้งานคลาส Phonemizer ภาษาไทยที่คุณเดลต้าสร้างไว้
            return new ThaiVCCV_CVVCPhonemizerDelta();
        }

        [Theory]
        // ทดสอบการประมวลผลคำพื้นฐาน โดยอ้างอิงชุดคำว่า "ทด" และ "สอบ"
        // หมายเหตุ: ชุด Alias ท้ายสุดอาจต้องปรับให้ตรงกับกฏ Oto.ini ของคุณเดลต้าอีกครั้ง
        [InlineData("th_vccv_delta",
            new string[] { "ทด", "สอบ" },
            new string[] { "-tho", "od-", "sQ", "Qb-" })] 
        public void BasicPhonemizingTest(string singerName, string[] lyrics, string[] aliases) {
            SameAltsTonesColorsTest(singerName, lyrics, aliases, "", "C4", "");
        }

        [Fact]
        public void ToneShiftTest() {
            // ทดสอบการเปลี่ยนระดับเสียง (Tone Shift) ในช่วงตัวโน้ตเดียวกัน
            RunPhonemizeTest("th_vccv_delta", new NoteParams[] {
                new NoteParams {
                    lyric = "ดี",
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
            }, new string[] { "-di", "i-_H" }); // จำลองสถานการณ์ที่เสียงหางมีการขยับคีย์ขึ้น
        }

        [Theory]
        // ทดสอบระบบ Hint (การพิมพ์คำอ่านกำกับ) ว่าทำงานได้ถูกต้องแม้เนื้อร้องจะว่างเปล่าหรือเป็นคำที่อ่านไม่ออก
        [InlineData("จันทร์", "", new string[] { "-ja", "an-" })]
        [InlineData("จันทร์", "j a n", new string[] { "-ja", "an-" })]
        [InlineData("asdfjkl", "j a n", new string[] { "-ja", "an-" })]
        [InlineData("", "j a n", new string[] { "-ja", "an-" })]
        public void HintTest(string lyric, string hint, string[] aliases) {
            RunPhonemizeTest("th_vccv_delta", new NoteParams[] { new NoteParams { lyric = lyric, hint = hint, tone = "C4", phonemes = SamePhonemeParams(4, 0, 0, "") } }, aliases);
        }
    }
}

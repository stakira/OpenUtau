using OpenUtau.Api;
using OpenUtau.Plugin.Builtin;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Plugins {
    public class ThaiVCCVTest : PhonemizerTestBase {
        public ThaiVCCVTest(ITestOutputHelper output) : base(output) { }
        
        protected override Phonemizer CreatePhonemizer() {
            // เรียกใช้ Phonemizer ภาษาไทยที่เราเพิ่งเขียนไป (v8.7)
            return new ThaiVCCVPhonemizer();
        }

        // =========================================================
        // 1. ทดสอบการแปลงเสียงพื้นฐาน (Basic Phonemizing)
        // =========================================================
        [Theory]
        [InlineData("th_vccv",
            new string[] { "ทด", "สอบ" },
            // โน้ต 1 "ทด" (th o d): เริ่มด้วย - th, สระ th o, ตัวสะกด o d (ไม่มีคลาย d - เพราะมีคำต่อ)
            // โน้ต 2 "สอบ" (s Q b): พยัญชนะ s Q, ตัวสะกด Q b, จบคำด้วย b -
            new string[] { "- th", "th o", "o d", "s Q", "Q b", "b -" })]
        public void BasicPhonemizingTest(string singerName, string[] lyrics, string[] aliases) {
            SameAltsTonesColorsTest(singerName, lyrics, aliases, "", "C4", "");
        }

        // =========================================================
        // 2. ทดสอบไม้ยมกและโน้ตเอื้อน (Maiyamok & Extension Test)
        // =========================================================
        [Theory]
        [InlineData("th_vccv",
            new string[] { "อา", "+" },
            // โน้ต "อา" ลากเสียง + ต้องไม่มีเสียงเริ่มใหม่ ได้แค่ "- a" และปิดด้วย "a -"
            new string[] { "- a", "a -" })]
        [InlineData("th_vccv",
            new string[] { "รัก", "ๆ" },
            // โน้ต "ๆ" ต้องย้อนไปก๊อปปี้คำว่า "รัก" (r a k) มาร้องซ้ำ
            new string[] { "- r", "r a", "a k", "r a", "a k", "k -" })]
        public void ExtensionAndMaiyamokTest(string singerName, string[] lyrics, string[] aliases) {
            SameAltsTonesColorsTest(singerName, lyrics, aliases, "", "C4", "");
        }

        // =========================================================
        // 3. ทดสอบการทำงานของ Hint และกฎคำพิเศษ (Hint & Dictionary Test)
        // =========================================================
        [Theory]
        // เทสคำเดี่ยวปกติ
        [InlineData("ทด", "", new string[] { "- th", "th o", "o d", "d -" })]
        
        // เทสการใช้ Phonetic Hint บังคับคำอ่าน (เช่น พิมพ์ "ทด" แต่บังคับให้อ่าน "ทัด")
        [InlineData("ทด", "th a d", new string[] { "- th", "th a", "a d", "d -" })]
        
        // เทสกฎอักษรนำ (ห และ อ ต้องถูกละเว้น)
        [InlineData("อยาก", "", new string[] { "- y", "y a", "a k", "k -" })]
        [InlineData("หมอน", "", new string[] { "- m", "m Q", "Q n", "n -" })]
        
        // เทสคำพิเศษ และการล้างวรรณยุกต์ (เศร้า -> s 8)
        [InlineData("เศร้า", "", new string[] { "- s", "s 8", "8 -" })]
        public void HintAndSpecialWordsTest(string lyric, string hint, string[] aliases) {
            RunPhonemizeTest("th_vccv", new NoteParams[] { 
                new NoteParams { 
                    lyric = lyric, 
                    hint = hint, 
                    tone = "C4", 
                    phonemes = SamePhonemeParams(aliases.Length, 0, 0, "") 
                } 
            }, aliases);
        }
        
        // =========================================================
        // 4. ทดสอบ Tone Shift (ทดสอบการขยับ Pitch)
        // =========================================================
        [Fact]
        public void ToneShiftTest() {
            RunPhonemizeTest("th_vccv", new NoteParams[] {
                new NoteParams {
                    lyric = "อา",
                    hint = "",
                    tone = "C4",
                    phonemes = new PhonemeParams[] {
                        new PhonemeParams { alt = 0, shift = 0, color = "" },
                        new PhonemeParams { alt = 0, shift = 12, color = "" }, // ทดสอบขยับขึ้น 1 Octave
                    }
                }
            }, new string[] { "- a", "a -" });
        }
    }
}

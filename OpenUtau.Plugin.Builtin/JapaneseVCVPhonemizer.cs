using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    // Made And Checked By DELTA SYNTH & Gemini AI, OpenUtau Contributors
    [Phonemizer("Japanese VCV Phonemizer (Delta Edition v2.2)", "JA VCV Delta", language: "JA")]
    public class JapaneseVCVPhonemizer : Phonemizer {
        
        /// <summary>
        /// ตารางอ้างอิงสำหรับการแปลงฮิรางานะเป็นสระเสียงท้าย
        /// </summary>
        static readonly string[] vowels = new string[] {
            "a=ぁ,あ,か,が,さ,ざ,た,だ,な,は,ば,ぱ,ま,ゃ,や,ら,わ,ァ,ア,カ,ガ,サ,ザ,タ,ダ,ナ,ハ,バ,パ,マ,ャ,ヤ,ラ,ワ,a",
            "e=ぇ,え,け,げ,せ,ぜ,て,で,ね,へ,べ,ぺ,め,れ,ゑ,ェ,エ,ケ,ゲ,セ,ゼ,テ,デ,ネ,ヘ,ベ,ペ,メ,レ,ヱ,e",
            "i=ぃ,い,き,ぎ,し,じ,ち,ぢ,に,ひ,び,ぴ,み,り,ゐ,ィ,イ,キ,ギ,シ,ジ,チ,ヂ,ニ,ヒ,ビ,ピ,ミ,リ,ヰ,i",
            "o=ぉ,お,こ,ご,そ,ぞ,と,ど,の,ほ,ぼ,ぽ,も,ょ,よ,ろ,を,ォ,オ,コ,ゴ,ソ,ゾ,ト,ド,ノ,ホ,ボ,ポ,モ,ョ,ヨ,ロ,ヲ,o",
            "n=ん,n",
            "u=ぅ,う,く,ぐ,す,ず,つ,づ,ぬ,ふ,ぶ,ぷ,む,ゅ,ゆ,る,ゥ,ウ,ク,グ,ス,ズ,ツ,ヅ,ヌ,フ,ブ,プ,ム,ュ,ユ,ル,ヴ,u",
            "N=ン,ng",
        };

        static readonly Dictionary<string, string> vowelLookup;

        static JapaneseVCVPhonemizer() {
            // แปลงข้อมูลจาก String ให้อยู่ในรูปแบบ Dictionary เพื่อประสิทธิภาพในการค้นหาที่รวดเร็วและเสถียรที่สุด
            vowelLookup = vowels.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
        }

        private USinger singer;

        public override void SetSinger(USinger singer) => this.singer = singer;

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            var note = notes[0];
            int totalDuration = note.duration; // ดึงค่าความยาวของโน้ตปัจจุบัน (Tick)
            
            // 1. จัดการลบ Suffix อื่นๆ ออกจากเนื้อร้อง เพื่อความสะอาดและป้องกันปัญหาการค้นหา Alias
            var currentLyric = CleanLyric(note.lyric.Normalize()); 

            if (!string.IsNullOrEmpty(note.phoneticHint)) {
                if (CheckOtoUntilHit(new string[] { note.phoneticHint.Normalize() }, note, out var ph)) {
                    return new Result { phonemes = new Phoneme[] { new Phoneme { phoneme = ph.Alias, position = 0 } } };
                }
            }

            // 2. ตรวจสอบและหั่นหน่วยเสียง (Diphthong / Vowel Clusters)
            string[] splitLyrics = SplitSyllables(currentLyric);
            List<Phoneme> outputPhonemes = new List<Phoneme>();
            
            string currentVowelContext = ""; // เก็บสระอ้างอิงสำหรับเชื่อมพยางค์ถัดไป

            if (prevNeighbour != null) {
                var prevLyric = CleanLyric(prevNeighbour.Value.lyric.Normalize());
                if (!string.IsNullOrEmpty(prevNeighbour.Value.phoneticHint)) {
                    prevLyric = prevNeighbour.Value.phoneticHint.Normalize();
                }
                var unicode = ToUnicodeElements(prevLyric);
                if (vowelLookup.TryGetValue(unicode.LastOrDefault() ?? string.Empty, out var vow)) {
                    currentVowelContext = vow;
                }
            }

            // 3. ประมวลผลแต่ละพยางค์ที่ถูกหั่นออกมา พร้อมคำนวณมาตราส่วน 1:2
            for (int i = 0; i < splitLyrics.Length; i++) {
                string partLyric = splitLyrics[i];
                string[] tests;

                // 4. แปลงสระโดดที่ซ้ำกับสระก่อนหน้าให้เป็นเครื่องหมาย "+" อัตโนมัติสำหรับการเอื้อน
                if (IsMatchingContinuationVowel(partLyric, currentVowelContext)) {
                    partLyric = "+";
                }

                if (partLyric == "+") {
                    tests = new string[] { partLyric };
                } else if (!string.IsNullOrEmpty(currentVowelContext)) {
                    tests = new string[] { $"{currentVowelContext} {partLyric}", $"* {partLyric}", partLyric, $"- {partLyric}" };
                } else {
                    tests = new string[] { $"- {partLyric}", partLyric };
                }

                // 5. คำนวณตำแหน่ง (Position) เพื่อให้พยัญชนะมีพื้นที่กว้างขึ้นตามอัตราส่วน 1/2 กับสระเสมอ
                int tickPosition = 0;
                
                // หากโน้ตถูกหั่นเป็นหลายพยางค์ หรืออยู่ในโน้ตที่แคบมากๆ จะบังคับใช้อัตราส่วนนี้ทันที
                if (splitLyrics.Length > 1) {
                    if (i == 0) {
                        tickPosition = 0; // ส่วนที่ 1 (พยัญชนะ/คำตั้งต้น) เริ่มที่จุดเริ่มต้น
                    } else {
                        // ส่วนที่ 2 (สระ/คำเอื้อน) ให้เริ่มที่ 1/3 ของโน้ต 
                        // ส่งผลให้พยัญชนะมีพื้นที่ 1 ส่วน และสระมีพื้นที่ 2 ส่วนเสมอ แม้โน้ตจะสั้นแค่ไหนก็ตาม
                        tickPosition = totalDuration / 3; 
                    }
                }

                if (CheckOtoUntilHit(tests, note, out var oto)) {
                    outputPhonemes.Add(new Phoneme { 
                        phoneme = oto.Alias,
                        position = tickPosition
                    });
                    
                    // อัปเดตสระอ้างอิงสำหรับพยางค์ย่อยถัดไป (ถ้ามี)
                    var partUnicode = ToUnicodeElements(partLyric);
                    if (vowelLookup.TryGetValue(partUnicode.LastOrDefault() ?? string.Empty, out var nextVow)) {
                        currentVowelContext = nextVow;
                    }
                } else {
                    outputPhonemes.Add(new Phoneme { 
                        phoneme = partLyric,
                        position = tickPosition
                    });
                }
            }

            // 6. ระบบเติมเสียงท้าย (End Breaths / Tail Notes "V -" หรือ "V R") แบบอัตโนมัติ
            if (nextNeighbour == null || string.IsNullOrEmpty(nextNeighbour.Value.lyric)) {
                if (!string.IsNullOrEmpty(currentVowelContext)) {
                    string[] tailTests = new string[] { $"{currentVowelContext} -", $"{currentVowelContext} R" };
                    if (CheckOtoUntilHit(tailTests, note, out var tailOto)) {
                        outputPhonemes.Add(new Phoneme { 
                            phoneme = tailOto.Alias, 
                            // หน่วงตำแหน่งของเสียงท้ายให้อยู่ตรงปลายสุดของโน้ต เพื่อความเป็นธรรมชาติและไม่แย่งพื้นที่เนื้อร้องหลัก
                            position = totalDuration 
                        });
                    }
                }
            }

            return new Result {
                phonemes = outputPhonemes.ToArray()
            };
        }

        /// <summary>
        /// ฟังก์ชันสำหรับลบ Suffix ที่ไม่จำเป็นออกจากเนื้อร้อง
        /// </summary>
        private string CleanLyric(string lyric) {
            // ตัดเครื่องหมายอักขระพิเศษที่มักติดมากับ Suffix ออก
            return Regex.Replace(lyric, @"[_A-Za-z0-9↑↓]+$", "").Trim();
        }

        /// <summary>
        /// ฟังก์ชันตรวจสอบว่าเนื้อร้องเป็นสระที่ตรงกับพยางค์ก่อนหน้าหรือไม่ เพื่อแปลงเป็น "+"
        /// </summary>
        private bool IsMatchingContinuationVowel(string lyric, string prevVowel) {
            if (string.IsNullOrEmpty(prevVowel)) return false;
            if (vowelLookup.TryGetValue(lyric, out string thisVowel)) {
                // หากเป็นสระตัวเดียวกัน (เช่น ร้อง "あ" ต่อจากเสียงที่ลงท้ายด้วย "a")
                return thisVowel == prevVowel && (lyric.Length == 1 || Regex.IsMatch(lyric, @"^[aiueoあいうえおぁぃぅぇぉ]+$"));
            }
            return false;
        }

        /// <summary>
        /// ฟังก์ชันสำหรับหั่นหน่วยเสียงคำควบกล้ำหรือสระประสม (Diphthongs / Clusters) อย่างแม่นยำ
        /// </summary>
        private string[] SplitSyllables(string lyric) {
            // รายการคำเฉพาะที่ต้องการให้แยกร่างอัตโนมัติ
            var exactSplits = new Dictionary<string, string[]> {
                {"てい", new[]{"て", "い"}},
                {"でぃ", new[]{"で", "ぃ"}},
                {"すい", new[]{"す", "い"}},
                {"さん", new[]{"さ", "ん"}},
                {"こう", new[]{"こ", "う"}},
                {"とう", new[]{"と", "う"}},
                {"おい", new[]{"お", "い"}},
                {"たい", new[]{"た", "い"}},
                {"ない", new[]{"な", "い"}}
            };

            if (exactSplits.ContainsKey(lyric)) {
                return exactSplits[lyric];
            }

            // อัลกอริทึมเสริม: หากคำยาวกว่า 1 ตัวอักษรและลงท้ายด้วย ん, い, う ให้ลองตัดอัตโนมัติ
            if (lyric.Length > 1) {
                char lastChar = lyric.Last();
                if (lastChar == 'ん' || lastChar == 'い' || lastChar == 'う' || lastChar == 'ぃ' || lastChar == 'ぅ') {
                    return new string[] { lyric.Substring(0, lyric.Length - 1), lastChar.ToString() };
                }
            }

            return new string[] { lyric };
        }

        private bool CheckOtoUntilHit(string[] input, Note note, out UOto oto) {
            oto = default;
            var attr = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
            string color = attr.voiceColor ?? "";

            var otos = new List<UOto>();
            foreach (string test in input) {
                if (singer.TryGetMappedOto(test + attr.alternate, note.tone + attr.toneShift, color, out var otoAlt)) {
                    otos.Add(otoAlt);
                } else if (singer.TryGetMappedOto(test, note.tone + attr.toneShift, color, out var otoCandidacy)) {
                    otos.Add(otoCandidacy);
                }
            }

            if (otos.Count > 0) {
                if (otos.Any(o => (o.Color ?? string.Empty) == color)) {
                    oto = otos.Find(o => (o.Color ?? string.Empty) == color);
                    return true;
                } else {
                    oto = otos.First();
                    return true;
                }
            }
            return false;
        }
    }
}

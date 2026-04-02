using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Melanchall.DryWetMidi.Interaction;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Thai VCCV_CVVC Delta Synth", "TH VCCV Delta", "DELTA SYNTH", language: "TH")]
    public class ThaiVCCV_CVVCPhonemizerDelta : Phonemizer {

        static readonly string[] vowels = new string[] {
            "a", "i", "u", "e", "o", "@", "Q", "3", "6", "1", "ia", "ua", "I", "8"
        };

        static readonly string[] diphthongs = new string[] {
            "r", "l", "w", "y"
        };

        static readonly string[] consonants = new string[] {
            "b", "ch", "d", "f", "g", "h", "j", "k", "kh", "l", "m", "n", "p", "ph", "r", "s", "t", "th", "w", "y", "-"
        };

        static readonly string[] endingConsonants = new string[] {
            "b", "ch", "d", "f", "g", "h", "j", "k", "kh", "l", "m", "n", "p", "ph", "r", "s", "t", "th", "w", "y"
        };

        private readonly Dictionary<string, string> VowelMapping = new Dictionary<string, string> {
            {"เcือะ", "6"}, {"เcือx", "6"}, {"แcะ", "@"}, {"แcx", "@"}, {"เcอะ", "3"}, {"เcอ", "3"}, {"ไc", "I"}, {"ใc", "I"}, {"เcาะ", "Q"}, {"cอx", "Q"},
            {"cืx", "1"}, {"cึx", "1"}, {"cือ", "1"}, {"cะ", "a"}, {"cัx", "a"}, {"cาx", "a"}, {"เcา", "8"}, {"เcะ", "e"}, {"เcx", "e"}, {"cิx", "i"}, {"cีx", "i"},
            {"เcียะ", "ia"}, {"เcียx", "ia"}, {"โcะ", "o"}, {"โcx", "o"}, {"cุx", "u"}, {"cูx", "u"}, {"cัวะ", "ua"}, {"cัว", "ua"}, {"cำ", "am"}, {"เcิx", "3"}, {"เcิ", "3"}
        };

        private readonly Dictionary<char, string> CMapping = new Dictionary<char, string> {
            {'-', "-"}, {'อ', "-"},
            {'ก', "k"}, {'ข', "kh"}, {'ค', "kh"}, {'ฆ', "kh"}, {'ฅ', "kh"}, {'ฃ', "kh"},
            {'จ', "j"}, {'ฉ', "ch"}, {'ช', "ch"}, {'ฌ', "ch"},
            {'ฎ', "d"}, {'ด', "d"},
            {'ต', "t"}, {'ฏ', "t"},
            {'ถ', "th"}, {'ฐ', "th"}, {'ฑ', "th"}, {'ธ', "th"}, {'ท', "th"},
            {'บ', "b"}, {'ป', "p"}, {'พ', "ph"}, {'ผ', "ph"}, {'ภ', "ph"}, {'ฟ', "f"}, {'ฝ', "f"},
            {'ห', "h"}, {'ฮ', "h"},
            {'ม', "m"}, {'น', "n"}, {'ณ', "n"}, {'ร', "r"}, {'ล', "l"}, {'ฤ', "r"},
            {'ส', "s"}, {'ศ', "s"}, {'ษ', "s"}, {'ซ', "s"},
            {'ง', "g"}, {'ย', "y"}, {'ญ', "y"}, {'ว', "w"}, {'ฬ', "r"}
        };

        private readonly Dictionary<char, string> XMapping = new Dictionary<char, string> {
            {'ก', "k"}, {'ข', "k"}, {'ค', "k"}, {'ฆ', "k"},
            {'บ', "b"}, {'ป', "b"}, {'พ', "b"}, {'ภ', "b"},
            {'ฟ', "f"}, {'ด', "d"}, {'จ', "d"}, {'ช', "ch"}, {'ฎ', "d"}, {'ฏ', "d"}, {'ฐ', "d"}, {'ฑ', "d"}, {'ฒ', "d"}, {'ต', "d"}, {'ถ', "d"}, {'ท', "d"}, {'ธ', "d"},
            {'ส', "s"}, {'ซ', "s"}, {'ศ', "s"}, {'ษ', "s"},
            {'น', "n"}, {'ณ', "n"}, {'ญ', "n"}, {'ร', "n"}, {'ล', "l"}, {'ฬ', "l"},
            {'ม', "m"}, {'ง', "g"}, {'ย', "y"}, {'ว', "w"}
        };

        private USinger singer;
        public override void SetSinger(USinger singer) => this.singer = singer;

        private bool checkOtoUntilHit(string[] input, Note note, out UOto oto) {
            oto = default;
            var attr = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
            foreach (string test in input) {
                if (singer.TryGetMappedOto(test, note.tone + attr.toneShift, attr.voiceColor, out var otoCandidacy)) {
                    oto = otoCandidacy;
                    return true;
                }
            }
            return false;
        }

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            var note = notes[0];
            var currentLyric = note.lyric.Normalize();
            if (!string.IsNullOrEmpty(note.phoneticHint)) currentLyric = note.phoneticHint.Normalize();

            var phonemes = new List<Phoneme>();
            List<string> tests = new List<string>();

            string prevTemp = prevNeighbour?.lyric ?? "";
            var prevTh = ParseInput(prevTemp);
            var noteTh = ParseInput(currentLyric);

            // --- แก้ไข Logic อ อ่าง กลางประโยค ---
            bool isMiddleGlottal = (noteTh.Consonant == "-" && prevNeighbour != null);

            if (noteTh.Consonant != null && noteTh.Vowel != null && !isMiddleGlottal) {
                // กรณีปกติ (มีพยัญชนะต้น หรือ เป็น อ อ่าง ต้นประโยค)
                string baseCv = (noteTh.Dipthong != null) ? noteTh.Consonant + noteTh.Dipthong + noteTh.Vowel : noteTh.Consonant + noteTh.Vowel;
                if (checkOtoUntilHit(new string[] { baseCv }, note, out var tempOto)) tests.Add(tempOto.Alias);
            } 
            else if (noteTh.Vowel != null) {
                // กรณีไม่มีพยัญชนะต้น หรือ อ อ่าง กลางประโยค (เน้นเชื่อมเสียง)
                if (prevTh.EndingConsonant != null && checkOtoUntilHit(new string[] { prevTh.EndingConsonant + noteTh.Vowel }, note, out var tempOto)) tests.Add(tempOto.Alias);
                else if (prevTh.Vowel != null && checkOtoUntilHit(new string[] { prevTh.Vowel + noteTh.Vowel }, note, out tempOto)) tests.Add(tempOto.Alias);
                else if (checkOtoUntilHit(new string[] { noteTh.Vowel }, note, out tempOto)) tests.Add(tempOto.Alias);
            }

            if (noteTh.EndingConsonant != null && noteTh.Vowel != null) {
                if (checkOtoUntilHit(new string[] { noteTh.Vowel + noteTh.EndingConsonant }, note, out var tempOto)) tests.Add(tempOto.Alias);
            }

            if (prevNeighbour == null && tests.Count >= 1) {
                if (checkOtoUntilHit(new string[] { "-" + tests[0] }, note, out var tempOto)) tests[0] = tempOto.Alias;
            }

            if (nextNeighbour == null && tests.Count >= 1) {
                string tail = (noteTh.EndingConsonant == null) ? noteTh.Vowel + "-" : tests.Last() + "-";
                if (checkOtoUntilHit(new string[] { tail }, note, out var tempOto)) {
                    if (noteTh.EndingConsonant == null) tests.Add(tempOto.Alias);
                    else tests[tests.Count - 1] = tempOto.Alias;
                }
            }

            if (tests.Count == 0) tests.Add(currentLyric);

            if (checkOtoUntilHit(tests.ToArray(), note, out var mainOto)) {
                var noteDuration = notes.Sum(n => n.duration);
                for (int i = 0; i < tests.Count; i++) {
                    int position = 0;
                    if (i > 0) position = (int)(noteDuration * 0.6); 
                    phonemes.Add(new Phoneme { phoneme = tests[i], position = position });
                }
            }

            return new Result { phonemes = phonemes.ToArray() };
        }

        // --- ส่วน ParseInput และ WordToPhonemes เหมือนเดิม ---
        (string Consonant, string Dipthong, string Vowel, string EndingConsonant) ParseInput(string input) {
            if (string.IsNullOrEmpty(input)) return (null, null, null, null);
            input = WordToPhonemes(input);
            string consonant = null; string dipthong = null; string vowel = null; string endingConsonant = null;
            foreach (var con in consonants) { if (input.StartsWith(con)) { consonant = con; break; } }
            int idx = consonant?.Length ?? 0;
            foreach (var dip in diphthongs) { if (input.Substring(Math.Min(idx, input.Length)).StartsWith(dip)) { dipthong = dip; break; } }
            idx += dipthong?.Length ?? 0;
            foreach (var vow in vowels) { if (input.Substring(Math.Min(idx, input.Length)).StartsWith(vow)) { vowel = vow; break; } }
            idx += vowel?.Length ?? 0;
            if (idx < input.Length) endingConsonant = input.Substring(idx);
            return (consonant, dipthong, vowel, endingConsonant);
        }

        public string WordToPhonemes(string input) {
            input = input.Replace(" ", "");
            input = Regex.Replace(input, ".์", "");
            input = Regex.Replace(input, "[่้๊๋็]", "");
            input = input.Replace("ทร", "ซ");
            input = input.Replace("ฤทธิ์", "ริด").Replace("ฤา", "รือ").Replace("ฤ", "รึ");
            input = Regex.Replace(input, "([ก-ฮ])รร(?![ก-ฮ])", "$1ัน");
            input = Regex.Replace(input, "([ก-ฮ])รร([ก-ฮ])", "$1ั$2");

            var splitMatch = Regex.Match(input, "^([เแโไใ])([ก-ฮ])([ก-ฮ])$");
            if (splitMatch.Success) {
                string v = splitMatch.Groups[1].Value; string c1 = splitMatch.Groups[2].Value; string c2 = splitMatch.Groups[3].Value;
                if (!Regex.IsMatch(c1 + c2, "^(ก[รลว]|ข[รลว]|ค[รลว]|ต[รล]|ป[รล]|พ[รลว]|ฟ[รล]|บ[รล]|ด[ร]|ผล|ทร|ศร|สร|ห[ก-ฮ]|อย)")) {
                    string vConv = (v == "เ") ? "e" : (v == "แ") ? "@" : (v == "โ") ? "o" : "I";
                    return ConvertC(c1) + "a" + ConvertC(c2) + vConv;
                }
            }

            foreach (var mapping in VowelMapping) {
                string pattern = "^" + mapping.Key.Replace("c", "(ก[รลว]|ข[รลว]|ค[รลว]|ต[รลว]|ป[รล]|พ[รลว]|ฟ[รล]|บ[รล]|ด[ร]|ผล|ทร|ศร|สร|ห[ก-ฮ]|อย|[ก-ฮ-])").Replace("x", "([ก-ฮ]?)") + "$";
                var match = Regex.Match(input, pattern);
                if (match.Success) {
                    string c = match.Groups[1].Value; string x = match.Groups.Count > 2 ? match.Groups[2].Value : string.Empty;
                    if (c.Length == 2 && c.EndsWith("ว") && string.IsNullOrEmpty(x)) { x = "ว"; c = c.Substring(0, 1); }
                    if (c.Length >= 2 && (c.StartsWith("ห") || c.StartsWith("อ"))) { c = (c == "อย") ? "ย" : c.Substring(1); }
                    string cFinal = ConvertC(c); string xFinal = ConvertX(x);
                    if (mapping.Value == "a" && input.Contains("ั") && x == "ว") return cFinal + "ua";
                    if (mapping.Value == "@" && x == "ว") return cFinal + "@w";
                    return cFinal + mapping.Value + xFinal;
                }
            }
            if (input.Length == 3 && !Regex.IsMatch(input, "[ะาิีึืุูเแโไใโั]")) {
                if (input == "สมร") return "samQn";
                return ConvertC(input[0].ToString()) + "a" + ConvertC(input[1].ToString()) + "Q" + ConvertX(input[2].ToString());
            }
            return input;
        }

        private string ConvertC(string input) {
            if (string.IsNullOrEmpty(input)) return "";
            string res = ""; foreach (char ch in input) { if (CMapping.ContainsKey(ch)) res += CMapping[ch]; }
            return res == "--" ? "-" : res;
        }

        private string ConvertX(string input) {
            if (string.IsNullOrEmpty(input)) return "";
            return XMapping.ContainsKey(input[0]) ? XMapping[input[0]] : "";
        }
    }
}

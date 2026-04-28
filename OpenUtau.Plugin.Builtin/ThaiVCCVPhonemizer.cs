using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Melanchall.DryWetMidi.Interaction;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Thai VCCV Phonemizer v.8.2", "TH VCCV", "DELTA SYNTH & Gemini and PRINTmov", language: "TH")]
    public class ThaiVCCVPhonemizer : Phonemizer {

        private Dictionary<string, string> userDict = new Dictionary<string, string>();
        private HashSet<string> thaiDict = new HashSet<string>();
        private bool dictLoaded = false;

        readonly string[] vowels = new string[] {
            "a", "i", "u", "e", "o", "@", "Q", "3", "6", "1", "ia", "ua", "I", "8"
        };

        readonly string[] diphthongs = new string[] {
            "r", "l", "w"
        };

        readonly string[] consonants = new string[] {
            "b", "ch", "d", "f", "g", "h", "j", "k", "kh", "l", "m", "n", "p", "ph", "r", "s", "t", "th", "w", "y"
        };

        readonly string[] endingConsonants = new string[] {
            "b", "ch", "d", "f", "g", "h", "j", "k", "kh", "l", "m", "n", "p", "ph", "r", "s", "t", "th", "w", "y"
        };

        private readonly Dictionary<string, string> VowelMapping = new Dictionary<string, string> {
            {"เcือะ", "6"}, {"เcือ", "6"}, {"เcือx", "6"}, 
            {"แcะ", "@"}, {"แc", "@"}, {"แcx", "@"}, 
            {"เcอะ", "3"}, {"เcอ", "3"}, {"เcอx", "3"}, 
            {"ไc", "I"}, {"ใc", "I"},
            {"เcาะ", "Q"}, {"cอ", "Q"}, {"cอx", "Q"},
            {"cือ", "1"}, {"cื", "1"}, {"cืx", "1"}, 
            {"cึ", "1"}, {"cึx", "1"}, 
            {"cะ", "a"}, {"cา", "a"}, {"cาx", "a"}, {"cัx", "a"}, {"cรร", "a"}, {"cรรx", "a"},
            {"เcา", "8"},
            {"เcะ", "e"}, {"เc", "e"}, {"เcx", "e"},
            {"cิ", "i"}, {"cิx", "i"}, 
            {"cี", "i"}, {"cีx", "i"},
            {"เcียะ", "ia"}, {"เcีย", "ia"}, {"เcียx", "ia"}, 
            {"โcะ", "o"}, {"โc", "o"}, {"โcx", "o"}, 
            {"cุ", "u"}, {"cุx", "u"}, 
            {"cู", "u"}, {"cูx", "u"}, 
            {"cัวะ", "ua"}, {"cัว", "ua"}, {"cัวx", "ua"}, 
            {"cำ", "a"}, 
            {"เcิ", "3"}, {"เcิรx", "3"}, {"เcิx", "3"}
        };

        private readonly Dictionary<char, string> CMapping = new Dictionary<char, string> {
            {'ก', "k"}, {'ข', "kh"}, {'ค', "kh"}, {'ฆ', "kh"}, {'ฅ', "kh"}, {'ฃ', "kh"},
            {'จ', "j"}, {'ฉ', "ch"}, {'ช', "ch"}, {'ฌ', "ch"},
            {'ฎ', "d"}, {'ด', "d"},
            {'ต', "t"}, {'ฏ', "t"},
            {'ถ', "th"}, {'ฐ', "th"}, {'ฑ', "th"}, {'ฒ', "th"}, {'ธ', "th"}, {'ท', "th"},
            {'บ', "b"}, {'ป', "p"}, {'พ', "ph"}, {'ผ', "ph"}, {'ภ', "ph"}, {'ฟ', "f"}, {'ฝ', "f"},
            {'ห', "h"}, {'ฮ', "h"},
            {'ม', "m"}, {'น', "n"}, {'ณ', "n"}, {'ร', "r"}, {'ล', "l"}, {'ฤ', "r"},
            {'ส', "s"}, {'ศ', "s"}, {'ษ', "s"}, {'ซ', "s"},
            {'ง', "g"}, {'ย', "y"}, {'ญ', "y"}, {'ว', "w"}, {'ฬ', "r"}
        };

        private readonly Dictionary<char, string> XMapping = new Dictionary<char, string> {
            {'ง', "g"},
            {'ม', "m"},
            {'ย', "y"},
            {'ว', "w"},
            {'น', "n"}, {'ณ', "n"}, {'ญ', "n"}, {'ร', "n"}, {'ล', "n"}, {'ฬ', "n"},
            {'ก', "k"}, {'ข', "k"}, {'ค', "k"}, {'ฆ', "k"},
            {'บ', "b"}, {'ป', "b"}, {'พ', "b"}, {'ฟ', "b"}, {'ภ', "b"},
            {'ด', "d"}, {'จ', "d"}, {'ช', "d"}, {'ซ', "d"}, {'ฎ', "d"}, {'ฏ', "d"}, 
            {'ฐ', "d"}, {'ฑ', "d"}, {'ฒ', "d"}, {'ต', "d"}, {'ถ', "d"}, {'ท', "d"}, 
            {'ธ', "d"}, {'ศ', "d"}, {'ษ', "d"}, {'ส', "d"}
        };

        private string NormalizeLyric(string text) {
            if (string.IsNullOrEmpty(text)) return text;
            string result = text.Normalize();
            
            result = result.Replace("ก็", "ก้อ").Replace("บ่", "บ่อ");
            result = result.Replace("ฤทธิ์", "ริด").Replace("อังกฤษ", "อังกิด").Replace("สมมติ", "สมมุด");
            result = result.Replace("ฤๅ", "รือ").Replace("ฤา", "รือ").Replace("ฤ", "รึ");
            result = result.Replace("ฦๅ", "ลือ").Replace("ฦา", "ลือ").Replace("ฦ", "ลึ");
            
            result = Regex.Replace(result, "[ก-ฮ][ิุ]?์", "");
            result = Regex.Replace(result, "[ก-ฮ]์", "");
            result = Regex.Replace(result, "[่้๊๋็]", "");
            
            return result;
        }

        private USinger singer;
        public override void SetSinger(USinger singer) {
            this.singer = singer;
            if (!dictLoaded) {
                LoadDictionary();
            }
        }

        private void LoadDictionary() {
            if (singer == null || string.IsNullOrEmpty(singer.Location)) return;
            string dictPath = Path.Combine(singer.Location, "words_th.txt");
            if (File.Exists(dictPath)) {
                try {
                    string[] lines = File.ReadAllLines(dictPath);
                    foreach (string line in lines) {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("[")) continue;
                        string[] parts = line.Split('=');
                        if (parts.Length == 2) {
                            string key = NormalizeLyric(parts[0].Trim());
                            userDict[key] = parts[1].Trim();
                        } else {
                            thaiDict.Add(NormalizeLyric(line.Trim()));
                        }
                    }
                    Log.Information($"Loaded {userDict.Count} mapping words and {thaiDict.Count} dict words from words_th.txt");
                } catch (Exception e) {
                    Log.Error(e, "Failed to load words_th.txt");
                }
            }
            dictLoaded = true;
        }

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

        private string GetNextSyllable(string text) {
            string[] validClusters = { 
                "กร", "กล", "กว", "ขร", "ขล", "ขว", "คร", "คล", "คว", 
                "ตร", "ปร", "ปล", "พร", "พล", "ผล", 
                "จร", "ซร", "ศร", "สร", "ทร",
                "บร", "บล", "ฟร", "ฟล", "ดร" 
            };

            if (text.Length == 2 && Regex.IsMatch(text, @"^[ก-ฮ][ก-ฮ]$")) {
                return text; 
            }

            if (text.Length > 2) {
                if (Regex.IsMatch(text.Substring(0, 1), @"^[ก-ฮ]") && 
                    Regex.IsMatch(text.Substring(1), @"^[ก-ฮ][ัิีึืุูเแโใไะาำ]")) {
                    string prefix2 = text.Substring(0, 2);
                    if (!validClusters.Contains(prefix2) && !prefix2.StartsWith("ห") && !prefix2.StartsWith("อ")
                        && !prefix2.EndsWith("อ") && !prefix2.EndsWith("ว")) {
                        return text.Substring(0, 1) + "ะ";
                    }
                } else if (Regex.IsMatch(text.Substring(0, 1), @"^[ก-ฮ]") &&
                           Regex.IsMatch(text.Substring(1), @"^[ก-ฮ]$|^[ก-ฮ][ก-ฮ]")) {
                    string prefix2 = text.Substring(0, 2);
                    if (!validClusters.Contains(prefix2) && !prefix2.StartsWith("ห") && !prefix2.StartsWith("อ")
                        && !prefix2.EndsWith("อ") && !prefix2.EndsWith("ว")) {
                        return text.Substring(0, 1) + "ะ";
                    }
                }
            }

            string greedyPattern = @"^([เแโใไ]?[หอ]?[ก-ฮ][รลว]?รร[ก-ฮ]?|[เแโใไ]?[หอ]?[ก-ฮ][รลว]?[ัิีึืุู]?[ะาำอยว]*[ก-ฮ]?|อ[ยว]?[ัิีึืุู]?[ะาำอยว]*[ก-ฮ]?)";
            Match m = Regex.Match(text, greedyPattern);
            if (m.Success && m.Length > 0) return m.Value;

            return text.Substring(0, 1);
        }

        private List<string> SplitIntoSyllables(string word) {
            if (userDict.TryGetValue(word, out string mapped)) {
                if (mapped.Contains(",") || mapped.Contains(" ")) {
                    return mapped.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                }
            }
            
            if (!Regex.IsMatch(word, "[ก-ฮ]")) {
                return new List<string> { word };
            }
            
            List<string> syllables = new List<string>();
            int i = 0;
            while (i < word.Length) {
                string syl = GetNextSyllable(word.Substring(i));
                syllables.Add(syl);
                if (syl.EndsWith("ะ") && word.Substring(i).StartsWith(syl.Substring(0, 1)) && !word.Substring(i).StartsWith(syl)) {
                    i += syl.Length - 1;
                } else {
                    i += syl.Length;
                }
            }
            return syllables;
        }

        private string GetSyllable(string lyric, int plusCount) {
            string cleanLyric = lyric;
            if (cleanLyric.EndsWith("-") && cleanLyric.Length > 1) cleanLyric = cleanLyric.Substring(0, cleanLyric.Length - 1);
            else if (cleanLyric.ToLower().EndsWith("r") && cleanLyric.Length > 1) cleanLyric = cleanLyric.Substring(0, cleanLyric.Length - 1);
            
            List<string> syllables = SplitIntoSyllables(cleanLyric);
            
            for (int i = 1; i < syllables.Count; i++) {
                if (syllables[i] == "ๆ") syllables[i] = syllables[i - 1];
            }
            
            if (plusCount < syllables.Count) return syllables[plusCount];
            return syllables.LastOrDefault() ?? lyric;
        }

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            var note = notes[0];
            
            int currentPlusCount = 0;
            string baseLyric = NormalizeLyric(note.lyric);
            if (!string.IsNullOrEmpty(note.phoneticHint)) baseLyric = NormalizeLyric(note.phoneticHint);

            if (baseLyric == "ๆ" && prevNeighbours.Length > 0) {
                baseLyric = NormalizeLyric(prevNeighbours[prevNeighbours.Length - 1].lyric);
            }

            if (baseLyric == "+") {
                currentPlusCount = 1;
                int index = prevNeighbours.Length - 1;
                while (index >= 0 && NormalizeLyric(prevNeighbours[index].lyric) == "+") {
                    currentPlusCount++;
                    index--;
                }
                if (index >= 0) {
                    baseLyric = NormalizeLyric(prevNeighbours[index].lyric);
                    if (!string.IsNullOrEmpty(prevNeighbours[index].phoneticHint)) {
                        baseLyric = NormalizeLyric(prevNeighbours[index].phoneticHint);
                    }
                }
            }

            bool isRest = baseLyric == "-" || baseLyric.ToLower() == "r";
            bool forceClose = false;
            
            if (!isRest && baseLyric != "+") {
                if (baseLyric.EndsWith("-") && baseLyric.Length > 1) {
                    forceClose = true;
                } else if (baseLyric.ToLower().EndsWith("r") && baseLyric.Length > 1) {
                    forceClose = true;
                }
            }

            string currentLyric = baseLyric;
            if (!isRest && baseLyric != "+" && baseLyric != "") {
                List<string> syllables = SplitIntoSyllables(baseLyric.EndsWith("-") ? baseLyric.Substring(0, baseLyric.Length - 1) : (baseLyric.ToLower().EndsWith("r") ? baseLyric.Substring(0, baseLyric.Length - 1) : baseLyric));
                currentLyric = GetSyllable(baseLyric, currentPlusCount);
                
                if (forceClose) {
                    if (currentPlusCount != syllables.Count - 1) {
                        forceClose = false;
                    }
                }
            }

            var phonemes = new List<Phoneme>();
            List<string> tests = new List<string>();

            string prevTemp = "";
            if (prevNeighbour != null) {
                int p_plusCount = 0;
                string p_baseLyric = NormalizeLyric(prevNeighbour.Value.lyric);
                if (!string.IsNullOrEmpty(prevNeighbour.Value.phoneticHint)) p_baseLyric = NormalizeLyric(prevNeighbour.Value.phoneticHint);
                
                if (p_baseLyric == "ๆ" && prevNeighbours.Length > 1) {
                    p_baseLyric = NormalizeLyric(prevNeighbours[prevNeighbours.Length - 2].lyric);
                }
                
                if (p_baseLyric == "+") {
                    p_plusCount = 1;
                    int index = prevNeighbours.Length - 2; 
                    while (index >= 0 && NormalizeLyric(prevNeighbours[index].lyric) == "+") {
                        p_plusCount++;
                        index--;
                    }
                    if (index >= 0) {
                        p_baseLyric = NormalizeLyric(prevNeighbours[index].lyric);
                        if (!string.IsNullOrEmpty(prevNeighbours[index].phoneticHint)) {
                            p_baseLyric = NormalizeLyric(prevNeighbours[index].phoneticHint);
                        }
                    }
                }
                if (p_baseLyric != "-" && p_baseLyric.ToLower() != "r" && p_baseLyric != "+") {
                    prevTemp = GetSyllable(p_baseLyric, p_plusCount);
                } else {
                    prevTemp = p_baseLyric;
                }
            }
            var prevTh = ParseInput(prevTemp);

            if (isRest) {
                if (prevNeighbour != null) {
                    string endSound = prevTh.EndingConsonant ?? prevTh.Vowel;
                    if (endSound != null) {
                        if (checkOtoUntilHit(new string[] { endSound + " -", endSound + "-", endSound + " R", endSound + "R" }, note, out var tempOto)) {
                            tests.Add(tempOto.Alias);
                        }
                    }
                }
                if (tests.Count == 0 && checkOtoUntilHit(new string[] { "-", "R" }, note, out var fallbackOto)) {
                    tests.Add(fallbackOto.Alias);
                }
            } else {
                var noteTh = ParseInput(currentLyric);

                if (noteTh.Consonant == null && noteTh.Vowel != null && prevNeighbour != null) {
                    string p_lyric = NormalizeLyric(prevNeighbour.Value.lyric);
                    if (p_lyric != "-" && p_lyric.ToLower() != "r") {
                        if (prevTh.EndingConsonant != null) {
                            noteTh.Consonant = prevTh.EndingConsonant; 
                        }
                    }
                }

                if (noteTh.Consonant != null && noteTh.Dipthong == null && noteTh.Vowel != null) {
                    if (checkOtoUntilHit(new string[] { noteTh.Consonant + " " + noteTh.Vowel, noteTh.Consonant + noteTh.Vowel }, note, out var tempOto)) {
                        tests.Add(tempOto.Alias);
                    }
                } else if (noteTh.Consonant != null && noteTh.Dipthong != null && noteTh.Vowel != null) {
                    if (checkOtoUntilHit(new string[] { noteTh.Consonant + " " + noteTh.Dipthong + noteTh.Vowel, noteTh.Consonant + noteTh.Dipthong + " " + noteTh.Vowel, noteTh.Consonant + noteTh.Dipthong + noteTh.Vowel }, note, out var tempOto)) {
                        tests.Add(tempOto.Alias);
                    } else {
                        if (checkOtoUntilHit(new string[] { noteTh.Consonant + " " + noteTh.Dipthong, noteTh.Consonant + noteTh.Dipthong }, note, out tempOto)) {
                            tests.Add(tempOto.Alias);
                        } else if (checkOtoUntilHit(new string[] { noteTh.Consonant }, note, out tempOto)) {
                            tests.Add(tempOto.Alias);
                        }
                        
                        if (checkOtoUntilHit(new string[] { noteTh.Dipthong + " " + noteTh.Vowel, noteTh.Dipthong + noteTh.Vowel }, note, out tempOto)) {
                            tests.Add(tempOto.Alias);
                        }
                    }
                } else if (noteTh.Consonant == null && noteTh.Vowel != null) {
                    if (prevNeighbour != null && prevTh.Vowel != null && checkOtoUntilHit(new string[] { prevTh.Vowel + " " + noteTh.Vowel, prevTh.Vowel + noteTh.Vowel }, note, out var tempOto)) {
                        tests.Add(tempOto.Alias);
                    } else {
                        if (checkOtoUntilHit(new string[] { "- " + noteTh.Vowel, "-" + noteTh.Vowel, noteTh.Vowel }, note, out var fallbackOto)) {
                            tests.Add(fallbackOto.Alias);
                        }
                    }
                }

                if (noteTh.EndingConsonant != null && noteTh.Vowel != null) {
                    if (checkOtoUntilHit(new string[] { noteTh.Vowel + " " + noteTh.EndingConsonant, noteTh.Vowel + noteTh.EndingConsonant }, note, out var tempOto)) {
                        tests.Add(tempOto.Alias);
                    }
                } else if (nextNeighbour != null && noteTh.Vowel != null) {
                    string nextTemp = "";
                    string n_lyric = NormalizeLyric(nextNeighbour.Value.lyric);
                    if (n_lyric == "+") {
                        nextTemp = GetSyllable(baseLyric, currentPlusCount + 1);
                    } else {
                        nextTemp = GetSyllable(n_lyric, 0);
                    }
                    var nextTh = ParseInput(nextTemp);
                    if (checkOtoUntilHit(new string[] { noteTh.Vowel + " " + nextTh.Consonant, noteTh.Vowel + nextTh.Consonant }, note, out var tempOto)) {
                        tests.Add(tempOto.Alias);
                    }
                }

                if (noteTh.Consonant != null && noteTh.Vowel == null) {
                    if (prevTh.Vowel != null) {
                        if (checkOtoUntilHit(new string[] { prevTh.Vowel + " " + noteTh.EndingConsonant, prevTh.Vowel + noteTh.EndingConsonant }, note, out var tempOto)) {
                            tests.Add(tempOto.Alias);
                        } else if (checkOtoUntilHit(new string[] { prevTh.Vowel + " " + noteTh.Consonant, prevTh.Vowel + noteTh.Consonant }, note, out tempOto)) {
                            tests.Add(tempOto.Alias);
                        }
                    }
                    if (tests.Count == 0) {
                        if (checkOtoUntilHit(new string[] { noteTh.Consonant }, note, out var tempOto)) {
                            tests.Add(tempOto.Alias);
                        }
                    }
                    if (tests.Count == 0) {
                        if (checkOtoUntilHit(new string[] { noteTh.Consonant + "Q" }, note, out var tempOto)) {
                            tests.Add(tempOto.Alias);
                        }
                    }
                }

                bool isAfterRest = prevNeighbour == null || NormalizeLyric(prevNeighbour.Value.lyric) == "-" || NormalizeLyric(prevNeighbour.Value.lyric).ToLower() == "r";
                if (isAfterRest && tests.Count >= 1) {
                    string startChar = noteTh.Consonant ?? noteTh.Vowel;
                    string firstAlias = tests[0];
                    if (startChar != null) {
                        if (checkOtoUntilHit(new string[] { "- " + startChar, "-" + startChar, "- " + firstAlias, "-" + firstAlias }, note, out var tempOto)) {
                            if (tempOto.Alias == "- " + startChar || tempOto.Alias == "-" + startChar) {
                                if (startChar != firstAlias) {
                                    tests.Insert(0, tempOto.Alias);
                                } else {
                                    tests[0] = tempOto.Alias;
                                }
                            } else {
                                tests[0] = tempOto.Alias;
                            }
                        }
                    }
                }

                bool isBeforeRest = nextNeighbour == null || NormalizeLyric(nextNeighbour.Value.lyric) == "-" || NormalizeLyric(nextNeighbour.Value.lyric).ToLower() == "r";
                if ((isBeforeRest || forceClose) && tests.Count >= 1) {
                    string endSound = noteTh.EndingConsonant ?? noteTh.Vowel;
                    if (endSound != null) {
                        if (checkOtoUntilHit(new string[] { endSound + " -", endSound + "-", endSound + " R", endSound + "R" }, note, out var tempOto)) {
                            tests.Add(tempOto.Alias);
                        } else if (checkOtoUntilHit(new string[] { tests[tests.Count - 1] + " -", tests[tests.Count - 1] + "-" }, note, out tempOto)) {
                            tests[tests.Count - 1] = tempOto.Alias;
                        }
                    }
                }

                if (tests.Count <= 0) {
                    if (checkOtoUntilHit(new string[] { currentLyric }, note, out var tempOto)) {
                        tests.Add(currentLyric);
                    }
                }
            }

            if (checkOtoUntilHit(tests.ToArray(), note, out var oto)) {
                var noteDuration = notes.Sum(n => n.duration);
                bool firstHasVowel = tests.Count > 0 && vowels.Any(v => tests[0].Contains(v));

                for (int i = 0; i < tests.Count; i++) {
                    int position = 0;
                    int vcPosition = noteDuration - 120;

                    if (nextNeighbour != null && tests[i].Contains(" ")) {
                        string nextTemp = "";
                        string n_lyric = NormalizeLyric(nextNeighbour.Value.lyric);
                        if (n_lyric == "+") {
                            nextTemp = GetSyllable(baseLyric, currentPlusCount + 1);
                        } else {
                            nextTemp = GetSyllable(n_lyric, 0);
                        }
                        var nextTh = ParseInput(nextTemp);
                        
                        var nextCheck = nextTh.Vowel;
                        if (nextTh.Consonant != null) {
                            nextCheck = nextTh.Consonant + nextTh.Vowel;
                        }
                        if (nextTh.Dipthong != null) {
                            nextCheck = nextTh.Consonant + nextTh.Dipthong + nextTh.Vowel;
                        }
                        var nextAttr = nextNeighbour.Value.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
                        if (singer.TryGetMappedOto(nextCheck, nextNeighbour.Value.tone + nextAttr.toneShift, nextAttr.voiceColor, out var nextOto)) {
                            if (oto.Overlap > 0) {
                                vcPosition = noteDuration - MsToTick(nextOto.Overlap) - MsToTick(nextOto.Preutter);
                            }
                        }
                    }

                    // ปรับแต่งขยายพื้นที่ของพยัญชนะต้นให้กว้างขึ้น เพื่อเสียงที่ชัดเจนเต็มคำ
                    if (i > 0) {
                        if (!firstHasVowel && i == 1) {
                            position = Math.Min((int)(noteDuration * 0.15), 90);
                        } else if ((tests[i].EndsWith("-") || tests[i].EndsWith("R") || tests[i].Contains(" -")) && tests.Count > 1) {
                            position = Math.Max((int)(noteDuration * 0.85), noteDuration - 60);
                        } else {
                            position = Math.Max((int)(noteDuration * 0.75), vcPosition);
                            
                            if (tests.Count > 2 && i == tests.Count - 2 && (tests[tests.Count - 1].EndsWith("-") || tests[tests.Count - 1].EndsWith("R") || tests[tests.Count - 1].Contains(" -"))) {
                                position = Math.Max((int)(noteDuration * 0.65), vcPosition - 60);
                            }
                        }
                    }

                    phonemes.Add(new Phoneme { phoneme = tests[i], position = position });
                }
            }

            return new Result {
                phonemes = phonemes.ToArray()
            };
        }

        (string Consonant, string Dipthong, string Vowel, string EndingConsonant) ParseInput(string input) {
            input = WordToPhonemes(input);

            if (input == null) {
                return (null, null, null, null);
            }

            input = input.Replace(" ", "");

            string consonant = null;
            string diphthong = null;
            string vowel = null;
            string endingConsonant = null;

            foreach (var con in consonants) {
                if (input.StartsWith(con)) {
                    if (consonant == null || consonant.Length < con.Length) {
                        consonant = con;
                    }
                }
            }

            int startIdx = consonant?.Length ?? 0;
            foreach (var dip in diphthongs) {
                if (input.Substring(startIdx).StartsWith(dip)) {
                    if (diphthong == null || diphthong.Length < dip.Length) {
                        diphthong = dip;
                    }
                }
            }

            startIdx += diphthong?.Length ?? 0;
            foreach (var vow in vowels) {
                if (input.Substring(startIdx).StartsWith(vow)) {
                    if (vowel == null || vowel.Length < vow.Length) {
                        vowel = vow;
                    }
                }
            }

            foreach (var con in endingConsonants) {
                if (input.EndsWith(con)) {
                    if (endingConsonant == null || endingConsonant.Length < con.Length) {
                        endingConsonant = con;
                    }
                }
            }

            return (consonant, diphthong, vowel, endingConsonant);
        }

        public string WordToPhonemes(string input) {
            string originalInput = input;
            input = input.Replace(" ", "");
            
            if (userDict.ContainsKey(input)) {
                return userDict[input];
            }

            // คำศัพท์พิเศษที่ดักเก็บไว้
            switch (input) {
                case "ควัน": return "kh ua n";
                case "อยาก": return "y a k";
            }

            if (!Regex.IsMatch(input, "[ก-ฮ]")) {
                return input;
            }

            foreach (var mapping in VowelMapping) {
                string pattern = "^" + mapping.Key
                    .Replace("c", "([ก-ฮ][ลรว]?|อ[ย]?|ห[ก-ฮ]?)")
                    .Replace("x", "([ก-ฮ]?)") + "$";

                var match = Regex.Match(input, pattern);
                if (match.Success) {
                    string c = match.Groups[1].Value;
                    string x = match.Groups.Count > 2 ? match.Groups[2].Value : string.Empty;

                    if (mapping.Key == "cรรx" && x == "") {
                        x = "น"; 
                    } else if (mapping.Key.EndsWith("x") && x == "") {
                        string[] validInitials = {
                            "กร", "กล", "กว", "ขร", "ขล", "ขว", "คร", "คล", "คว",
                            "ตร", "ปร", "ปล", "พร", "พล", "ผล",
                            "จร", "ซร", "ศร", "สร", "ทร",
                            "บร", "บล", "ฟร", "ฟล", "ดร"
                        };
                        
                        if (!validInitials.Contains(c) && c.Length > 1 && !c.StartsWith("ห") && !c.StartsWith("อ")) {
                            x = c.Substring(c.Length - 1); 
                            c = c.Substring(0, c.Length - 1); 
                        }
                    }

                    string cConverted = ConvertC(c, originalInput);
                    string xConverted = ConvertX(x);
                    
                    if (mapping.Key == "cำ") {
                        return cConverted + "a" + "m";
                    }
                    if (mapping.Value == "a" && input.Contains("ั") && x == "ว") {
                        return cConverted + "ua";
                    }
                    if (mapping.Value == "e" && x == "ย") {
                        return cConverted + "3" + xConverted;
                    }
                    return cConverted + mapping.Value + xConverted;
                }
            }
            
            Match mRR = Regex.Match(input, "^([หอ]?[ก-ฮ][รลว]?)รร([ก-ฮ]?)$");
            if (mRR.Success) {
                string c = mRR.Groups[1].Value;
                string x = mRR.Groups[2].Value;
                if (string.IsNullOrEmpty(x)) x = "น";
                return ConvertC(c, originalInput) + "a" + ConvertX(x);
            }

            Match mUa = Regex.Match(input, "^([หอ]?[ก-ฮ][รลว]?)ว([ก-ฮ])$");
            if (mUa.Success) {
                string c = mUa.Groups[1].Value;
                string x = mUa.Groups[2].Value;
                return ConvertC(c, originalInput) + "ua" + ConvertX(x);
            }

            Match mO = Regex.Match(input, "^([หอ]?[ก-ฮ][รลว]?)([ก-ฮ])$");
            if (mO.Success) {
                string c = mO.Groups[1].Value;
                string x = mO.Groups[2].Value;
                return ConvertC(c, originalInput) + "o" + ConvertX(x);
            }
            
            if (input.Length == 1) {
                return ConvertC(input, originalInput);
            }
            
            return input;
        }

        private string ConvertC(string input, string originalLyric = "") {
            if (string.IsNullOrEmpty(input)) return input;

            if (input == "อ") return "";

            if (input.Length >= 2 && (input.StartsWith("ห") || input.StartsWith("อ"))) {
                input = input.Substring(1);
            }

            if (input == "ทร") {
                if (originalLyric == "ทรา" || originalLyric == "นิทรา" || originalLyric == "จันทรา" || originalLyric.Contains("ทริ") || originalLyric.Contains("ทรี")) {
                } else {
                    return "s";
                }
            } else if (input == "สร" || input == "ศร" || input == "ซร") {
                return "s";
            } else if (input == "จร") {
                return "j";
            }

            char firstChar = input[0];
            char? secondChar = input.Length > 1 ? input[1] : (char?)null;

            if (CMapping.ContainsKey(firstChar)) {
                string firstCharConverted = CMapping[firstChar];
                if (secondChar != null && CMapping.ContainsKey((char)secondChar)) {
                    return firstCharConverted + CMapping[(char)secondChar];
                }
                return firstCharConverted;
            }
            return input;
        }

        private string ConvertX(string input) {
            if (string.IsNullOrEmpty(input)) return input;
            
            input = Regex.Replace(input, "[ิุ]$", "");
            
            if (input == "ตร" || input == "ทร" || input == "รถ" || input == "ชร") return "d"; 
            if (input == "รม" || input == "หม") return "m"; 
            if (input == "รท") return "d"; 
            if (input == "กร") return "k"; 

            char firstChar = input[0];
            if (XMapping.ContainsKey(firstChar)) {
                return XMapping[firstChar];
            }
            return input;
        }

    }
}

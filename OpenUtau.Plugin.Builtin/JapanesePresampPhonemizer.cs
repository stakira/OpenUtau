using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Classic;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Japanese presamp Phonemizer", "JA VCV & CVVC", "Maiko", language:"JA")]
    public class JapanesePresampPhonemizer : Phonemizer {

        // JP CV, VCV, CVVCを含むすべての日本語VBをサポートする予定です。
        // 基本的な仕様はpresampに準拠します。
        // 歌詞に書かれた[強]などの表情は非対応。VoiceColorでやってもらいます。
        // 喉切り[・]はpresamp.iniに書いてなくても動くようなんとかします。

        // Phoneme data held by the voice bank
        private List<string> PlainVowels = new List<string>();
        private List<string> PlainConsonants = new List<string>();
        static readonly string[] plainVowels = new string[] {"あ","い","う","え","お","を","ん","ン"};
        static readonly string[] plainConsonants = new string[]{"k","ky","g","gy",
                                                           "s","sh","z","j","t","ch","ty","ts",
                                                           "d","dy","n","ny","h","hy","f","b",
                                                           "by","p","py","m","my","y","r","4",
                                                           "ry","w","v","ng","l","・"
        };

        // in case voicebank is missing certain symbols
        static readonly string[] substitution = new string[] {  
            "ty,ch,ts=t", "j,dy=d", "gy=g", "ky=k", "py=p", "ny=n", "ry=r", "hy,f=h", "by,v=b", "dz=z", "l=r", "ly=l"
        };

        static readonly Dictionary<string, string> substituteLookup;

        static JapanesePresampPhonemizer() {
            substituteLookup = substitution.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[0].Split(',').Select(orig => (orig, parts[1]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
        }

        private USinger singer;
        private Presamp presamp;
        public override void SetSinger(USinger singer) {
            if (this.singer == singer) {
                return;
            }
            this.singer = singer;
            if (this.singer == null) {
                return;
            }

            presamp = new Presamp(singer.Location, singer.TextFileEncoding);
            if (presamp.PhonemeList.Count > 0) {
                PlainVowels.Clear();
                PlainConsonants.Clear();
                foreach (var pair in presamp.PhonemeList) {
                    if (!pair.Value.HasConsonant) {
                        PlainVowels.Add(pair.Key);
                    }
                    if (!pair.Value.HasVowel) {
                        PlainConsonants.Add(pair.Key);
                    }
                }
            } else {
                PlainVowels = plainVowels.ToList();
                PlainConsonants = plainConsonants.ToList();
            }
        }

        // JP CVVC をもとに改造
        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            var note = notes[0];
            var currentUnicode = ToUnicodeElements(note.lyric);
            var currentLyric = note.lyric;
            // replace (exact match)
            foreach (var pair in presamp.Replace) {
                if(pair.Key == currentLyric) {
                    currentLyric = pair.Value;
                }
            }
            string currentAlias = presamp.ParseAlias(currentLyric)[1];
            var initial = $"- {currentLyric}";
            var cfLyric = $"* {currentLyric}";


            if (prevNeighbour == null) {
                // Use "- V" or "- CV" if present in voicebank
                string[] tests = new string[] { initial, currentLyric };
                if (checkOtoUntilHit(tests, note, out var oto)){
                    currentLyric = oto.Alias;
                }
            } else {
                var prevUnicode = ToUnicodeElements(prevNeighbour?.lyric);
                var prevLyric = string.Join("", prevUnicode);
                string prevAlias = presamp.ParseAlias(prevLyric)[1];
                foreach (var pair in presamp.Replace) {
                    if (pair.Key == prevAlias) {
                        prevAlias = pair.Value;
                    }
                }

                // 喉切り prev is VC -> [・ あ][・あ][あ・][- あ][あ]
                if (prevLyric == "・" || Regex.IsMatch(prevLyric, "[aiueonN] ・$")) {
                    var vowelUpper = Regex.Match(currentLyric, "[あいうえおんン]").Value ?? currentLyric;
                    string[] tests = new string[] { $"・ {vowelUpper}", $"・{vowelUpper}", $"{vowelUpper}・", $"- {vowelUpper}・", $"- {vowelUpper}", initial, currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        currentLyric = oto.Alias;
                    }
                } else if (presamp.PhonemeList.TryGetValue(prevAlias, out PresampPhoneme prevPhoneme)) {

                    if (currentLyric.Contains("・")) { // 喉切り

                        if (Regex.IsMatch(currentLyric, "[aiueonN] ・$")) { // current is VC
                            string[] tests = new string[] { currentLyric };
                            if (checkOtoUntilHit(tests, note, out var oto)) {
                                currentLyric = oto.Alias;
                            }
                        } else if (currentLyric == "・" && prevPhoneme.HasVowel) { // current is VC
                            var vc = $"{prevPhoneme.Vowel} {currentLyric}";
                            string[] tests = new string[] { vc, currentLyric };
                            if (checkOtoUntilHit(tests, note, out var oto)) {
                                currentLyric = oto.Alias;
                            }
                        } else if (prevPhoneme.HasVowel) { // current is VCV or CV
                            var vcv = $"{prevPhoneme.Vowel} {currentLyric}";
                            var vowelUpper = Regex.Match(currentLyric, "[あいうえおんン]").Value ?? currentLyric;
                            var vcv2 = $"{prevPhoneme.Vowel} {vowelUpper}・";
                            var vcv3 = $"{prevPhoneme.Vowel} ・{vowelUpper}";
                            string[] tests = new string[] { vcv, vcv2, vcv3, $"・ {vowelUpper}", $"・{vowelUpper}", $"{vowelUpper}・", $"- {vowelUpper}・", $"- {vowelUpper}", initial, currentLyric };
                            if (checkOtoUntilHit(tests, note, out var oto)) {
                                currentLyric = oto.Alias;
                            }
                        } else { // current is CV
                            var vowelUpper = Regex.Match(currentLyric, "[あいうえおんン]").Value ?? currentLyric;
                            string[] tests = new string[] { $"・ {vowelUpper}", $"・{vowelUpper}", $"{vowelUpper}・", $"- {vowelUpper}・", $"- {vowelUpper}", initial, currentLyric };
                            if (checkOtoUntilHit(tests, note, out var oto)) {
                                currentLyric = oto.Alias;
                            }
                        }
                    } else if (presamp.PhonemeList.TryGetValue(currentAlias, out PresampPhoneme currentPhoneme) && currentPhoneme.IsPriority) {
                        // Priority: not VCV
                        string[] tests = new string[] { currentLyric, initial };
                        if (checkOtoUntilHit(tests, note, out var oto)) {
                            currentLyric = oto.Alias;
                        }
                    } else if (prevPhoneme.HasVowel) {
                        // try VCV
                        string prevVow = prevPhoneme.Vowel;
                        var vcv = $"{prevVow} {currentLyric}";
                        string[] tests = new string[] { vcv, cfLyric, currentLyric };
                        if (checkOtoUntilHit(tests, note, out var oto)) {
                            currentLyric = oto.Alias;
                        }
                    } else {
                        // try CV
                        string[] tests = new string[] { currentLyric, initial };
                        if (checkOtoUntilHit(tests, note, out var oto)) {
                            currentLyric = oto.Alias;
                        }
                    }
                } else {
                    // try "- CV" 
                    string[] tests = new string[] { initial, currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        currentLyric = oto.Alias;
                    }
                }
            }

            if (nextNeighbour != null) {
                var nextUnicode = ToUnicodeElements(nextNeighbour?.lyric);
                var nextLyric = string.Join("", nextUnicode);
                string nextAlias = presamp.ParseAlias(nextLyric)[1];
                foreach (var pair in presamp.Replace) {
                    if (pair.Key == nextAlias) {
                        nextAlias = pair.Value;
                    }
                }
                string vcPhoneme;


                if (nextLyric.Contains(" ")) {
                    return MakeSimpleResult(currentLyric);
                }

                // Without current vowel, VC cannot be created.
                if (!presamp.PhonemeList.TryGetValue(currentAlias, out PresampPhoneme currentPhoneme) || !currentPhoneme.HasVowel) {
                    return MakeSimpleResult(currentLyric);
                }
                var vowel = currentPhoneme.Vowel;

                // 喉切り
                if (nextLyric.Contains("・")) {
                    if (nextLyric == "・") { // next is VC
                        return MakeSimpleResult(currentLyric);
                    } else {
                        // next is VCV
                        var vowelUpper = Regex.Match(nextLyric, "[あいうえおんン]").Value;
                        if (vowelUpper == null) return MakeSimpleResult(currentLyric);

                        string[] tests = new string[] { $"{vowel} {vowelUpper}・", $"{vowel} ・{vowelUpper}" };
                        if (checkOtoUntilHit(tests, (Note)nextNeighbour, out var oto1)) {
                            var attr = nextNeighbour.Value.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
                            string nextcolor = attr.voiceColor ?? "";
                            if (oto1.Alias.Contains(" ") && oto1.Color == nextcolor) {
                                return MakeSimpleResult(currentLyric);
                            }
                        }
                        // next is CV (VC needed)
                        string[] tests2 = new string[] { $"{vowel} ・" };
                        if (checkOtoUntilHit(tests2, note, out oto1)) {
                            vcPhoneme = oto1.Alias;
                        } else {
                            return MakeSimpleResult(currentLyric);
                        }
                    }
                } else {
                    
                    // Without next consonant, VC cannot be created.
                    if (!presamp.PhonemeList.TryGetValue(nextAlias, out PresampPhoneme nextPhoneme) || !nextPhoneme.HasConsonant) {
                        return MakeSimpleResult(currentLyric);
                    }
                    var consonant = nextPhoneme.Consonant;

                    // If the next lyrics are a priority, use VC.
                    // If the next note has a VCV, use it.
                    if (!nextPhoneme.IsPriority) {
                        var nextVCV = $"{vowel} {nextAlias}";
                        string[] tests = new string[] { nextVCV, nextAlias };
                        if (checkOtoUntilHit(tests, (Note)nextNeighbour, out var oto1)) {
                            if (oto1.Alias.Contains(" ")) {
                                return MakeSimpleResult(currentLyric);
                            }
                        }
                    }

                    // Insert VC before next neighbor
                    vcPhoneme = $"{vowel} {consonant}";
                    var vcPhonemes = new string[] { vcPhoneme, "" };
                    // find potential substitute symbol
                    if (substituteLookup.TryGetValue(consonant ?? string.Empty, out var con)) {
                        vcPhonemes[1] = $"{vowel} {con}";
                    }
                    if (checkOtoUntilHit(vcPhonemes, note, out var oto)) {
                        vcPhoneme = oto.Alias;
                    } else {
                        return MakeSimpleResult(currentLyric);
                    }
                }

                int totalDuration = notes.Sum(n => n.duration);
                int vcLength = 120;
                var nextAttr = nextNeighbour.Value.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
                if (singer.TryGetMappedOto(nextLyric, nextNeighbour.Value.tone + nextAttr.toneShift, nextAttr.voiceColor, out var nextOto)) {
                    // If overlap is a negative value, vcLength is longer than Preutter
                    if (nextOto.Overlap < 0) {
                        vcLength = MsToTick(nextOto.Preutter - nextOto.Overlap);
                    } else {
                        vcLength = MsToTick(nextOto.Preutter);
                    }
                }
                vcLength = Convert.ToInt32(Math.Min(totalDuration / 2, vcLength * (nextAttr.consonantStretchRatio ?? 1)));

                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme() {
                            phoneme = currentLyric
                        },
                        new Phoneme() {
                            phoneme = vcPhoneme,
                            position = totalDuration - vcLength
                        }
                    },
                };
            }

            // No next neighbor
            return MakeSimpleResult(currentLyric);
        }

        // make it quicker to check multiple oto occurrences at once rather than spamming if else if
        private bool checkOtoUntilHit(string[] input, Note note, out UOto oto) {
            oto = default;

            var attr0 = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
            var attr1 = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 1) ?? default;

            var otos = new List<UOto>();
            foreach (string test in input) {
                UOto otoCandidacy;
                if (singer.TryGetMappedOto(test, note.tone + attr0.toneShift, attr0.voiceColor, out otoCandidacy)) {
                    otos.Add(otoCandidacy);
                }
            }

            if (otos.Count > 0) {
                if (attr0.voiceColor == null) {
                    oto = otos[0];
                } else if (otos.Any(oto => oto.Alias.Contains(attr0.voiceColor))) {
                    oto = otos.Find(oto => oto.Alias.Contains(attr0.voiceColor));
                } else {
                    oto = otos[0];
                }
                return true;
            }

            return false;
        }

    }
}

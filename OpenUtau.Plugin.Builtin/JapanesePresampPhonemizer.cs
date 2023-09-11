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

        // CV, VCV, CVVCを含むすべての日本語VBをサポートする予定です。Will support all Japanese VBs including CV, VCV, CVVC
        // 基本的な仕様はpresampに準拠します。Basic behavior conforms to presamp
        // 歌詞に書かれた"強"などの表情は非対応。VoiceColorでやってもらいます。Append suffixes such as "強" written in the lyrics are not supported
        // 喉切り"・"はpresamp.iniに書いてなくても動くようなんとかします。I'll try to make it work even if "・" are not written in presamp.ini
        // Supporting: [VOWEL][CONSONANT][PRIORITY][REPLACE][ALIAS(VCPAD,VCVPAD)]
        // Partial supporting: [NUM][APPEND][PITCH] -> Using to exclude useless characters in lyrics

        // in case voicebank is missing certain symbols
        static readonly string[] substitution = new string[] {
            "ty,ch,ts=t", "j,dy=d", "gy=g", "ky=k", "py=p", "ny=n", "ry=r", "my=m", "hy,f=h", "by,v=b", "dz=z", "l=r", "ly=l"
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

            presamp = new Presamp();
            presamp.ReadPresampIni(singer.Location, singer.TextFileEncoding);
        }


        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            var note = notes[0];
            var currentLyric = note.lyric.Normalize(); // Normalize(): measures for Unicode
            if (!string.IsNullOrEmpty(note.phoneticHint)) {
                currentLyric = note.phoneticHint.Normalize();
            } else {
                // replace (exact match)
                foreach (var pair in presamp.Replace) {
                    if (pair.Key == currentLyric) {
                        currentLyric = pair.Value;
                    }
                }
            }
            string currentAlias = presamp.ParseAlias(currentLyric)[1]; // exclude useless characters
            var vcvpad = presamp.AliasRules.VCVPAD;
            var vcpad = presamp.AliasRules.VCPAD;
            var initial = $"-{vcvpad}{currentLyric}";
            var cfLyric = $"*{vcpad}{currentLyric}";

            var vowelUpper = Regex.Match(currentLyric, "[あいうえおんン]").Value ?? currentLyric;
            var glottalCVtests = new List<string> { $"・{vcpad}{vowelUpper}", $"・{vowelUpper}", $"{vowelUpper}・", $"-{vcvpad}{vowelUpper}・", $"-{vcvpad}{vowelUpper}", initial, currentLyric };

            // Convert 1st phoneme
            if (!string.IsNullOrEmpty(note.phoneticHint)) { // not convert
                var tests = new List<string> { currentLyric };
                if (checkOtoUntilHit(tests, note, out var oto)) {
                    currentLyric = oto.Alias;
                }
            } else if (prevNeighbour == null) { // beginning of phrase
                if (currentLyric.Contains("・")) {
                    if (checkOtoUntilHit(glottalCVtests, note, out var oto1)) {
                        currentLyric = oto1.Alias;
                    }
                } else {
                    var tests = new List<string> { initial, currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        currentLyric = oto.Alias;
                    }
                }
            } else { // middle of phrase
                var prevLyric = prevNeighbour.Value.lyric.Normalize();
                if (!string.IsNullOrEmpty(prevNeighbour.Value.phoneticHint)) { // current phoneme is converted even if prev has hint
                    prevLyric = prevNeighbour.Value.phoneticHint.Normalize();
                } else {
                    foreach (var pair in presamp.Replace) {
                        if (pair.Key == prevLyric) {
                            prevLyric = pair.Value;
                        }
                    }
                }
                string prevAlias = presamp.ParseAlias(prevLyric)[1]; // exclude useless characters
                if (prevAlias.Contains("・")) {
                    prevAlias = prevAlias.Replace("・", "");
                }

                string vcGlottalStop = "[aiueonN]" + vcpad + "・$"; // [a ・]
                if (prevLyric == "・" || Regex.IsMatch(prevLyric, vcGlottalStop)) {
                    // 喉切り(Glottal stop) prev is VC -> current is Glottal stop CV[・ あ][・あ][あ・][- あ][あ]
                    if (checkOtoUntilHit(glottalCVtests, note, out var oto)) {
                        currentLyric = oto.Alias;
                    }
                } else if (presamp.PhonemeList.TryGetValue(prevAlias, out PresampPhoneme prevPhoneme)) {

                    if (currentLyric.Contains("・")) {
                        // Glottal stop
                        var tests = new List<string>();
                        UOto oto;

                        if (Regex.IsMatch(currentLyric, vcGlottalStop)) { // current is VC = 2nd phoneme is not needed
                            tests = new List<string> { currentLyric };
                            if (checkOtoUntilHit(tests, note, out oto)) {
                                return MakeSimpleResult(oto.Alias);
                            }
                        } else if (currentLyric == "・" && prevPhoneme.HasVowel) { // current is VC = 2nd phoneme is not needed
                            var vc = $"{prevPhoneme.Vowel}{vcpad}{currentLyric}";
                            tests = new List<string> { vc, currentLyric };
                            if (checkOtoUntilHit(tests, note, out oto)) {
                                return MakeSimpleResult(oto.Alias);
                            }
                        } else if (prevPhoneme.HasVowel) { // current is VCV
                            tests.Add($"{prevPhoneme.Vowel}{vcvpad}{currentLyric}");
                            tests.Add($"{prevPhoneme.Vowel}{vcvpad}{vowelUpper}・");
                            tests.Add($"{prevPhoneme.Vowel}{vcvpad}・{vowelUpper}");
                        }
                        tests.AddRange(glottalCVtests); // current is CV
                        if (checkOtoUntilHit(tests, note, out oto)) { // check VCV and CV
                            currentLyric = oto.Alias;
                        }
                    } else if (presamp.PhonemeList.TryGetValue(currentLyric, out PresampPhoneme currentPhoneme) && currentPhoneme.IsPriority) {
                        // Priority: not VCV, VC (almost C)
                        var tests = new List<string> { currentLyric, initial };
                        if (checkOtoUntilHit(tests, note, out var oto)) {
                            currentLyric = oto.Alias;
                        }
                    } else if (prevPhoneme.HasVowel) {
                        string prevVow = prevPhoneme.Vowel;

                        if (currentLyric == "っ" && nextNeighbour != null) { // っ = current is VC
                            var nextLyric = nextNeighbour.Value.lyric.Normalize();
                            if (!string.IsNullOrEmpty(nextNeighbour.Value.phoneticHint)) {
                                nextLyric = nextNeighbour.Value.phoneticHint.Normalize();
                            } else {
                                foreach (var pair in presamp.Replace) {
                                    if (pair.Key == nextLyric) {
                                        nextLyric = pair.Value;
                                    }
                                }
                            }
                            string nextAlias = presamp.ParseAlias(nextLyric)[1];

                            var axtu1 = $"{prevVow}{vcvpad}{currentLyric}"; // a っ
                            var axtu2 = $"{prevVow}{vcpad}{currentLyric}"; // a っ
                            var tests2 = new List<string> { axtu1, axtu2, currentLyric };
                            if (presamp.PhonemeList.TryGetValue(nextAlias, out PresampPhoneme nextPhoneme) && nextPhoneme.HasConsonant) {
                                tests2.Insert(2, $"{prevVow}{vcpad}{nextPhoneme.Consonant}"); // VC
                            }
                            if (checkOtoUntilHit(tests2, note, out var oto2)) {
                                return MakeSimpleResult(oto2.Alias);
                            }
                        } else { // try VCV, VC
                            var vcv = $"{prevVow}{vcvpad}{currentLyric}";
                            var vc = $"{prevVow}{vcpad}{currentLyric}";
                            var tests = new List<string> { vcv, vc, cfLyric, currentLyric };
                            if (checkOtoUntilHit(tests, note, out var oto)) {
                                currentLyric = oto.Alias;
                            }
                        }
                    } else {
                        // try CV
                        var tests = new List<string> { currentLyric, initial };
                        if (checkOtoUntilHit(tests, note, out var oto)) {
                            currentLyric = oto.Alias;
                        }
                    }
                } else {
                    // try "- CV" (prev is R, breath, etc.)
                    var tests = new List<string> { initial, currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        currentLyric = oto.Alias;
                    }
                }
            }

            // Insert 2nd phoneme (when next doesn't have hint)
            if (nextNeighbour != null && string.IsNullOrEmpty(nextNeighbour.Value.phoneticHint)) {
                int totalDuration = notes.Sum(n => n.duration);
                if (TickToMs(totalDuration) < 100) {
                    return MakeSimpleResult(currentLyric);
                }

                var nextLyric = nextNeighbour.Value.lyric.Normalize();
                foreach (var pair in presamp.Replace) {
                    if (pair.Key == nextLyric) {
                        nextLyric = pair.Value;
                    }
                }
                string nextAlias = presamp.ParseAlias(nextLyric)[1]; // exclude useless characters
                string vcPhoneme;

                // Without current vowel, VC cannot be created
                if (!presamp.PhonemeList.TryGetValue(currentAlias, out PresampPhoneme currentPhoneme) || !currentPhoneme.HasVowel) {
                    return MakeSimpleResult(currentLyric);
                }
                var vowel = currentPhoneme.Vowel;

                if (Regex.IsMatch(nextLyric, "[aiueonN]" + vcvpad) || Regex.IsMatch(nextLyric, "[aiueonN]" + vcpad)) {
                    // next is VCV or VC (VC is not needed)
                    return MakeSimpleResult(currentLyric);
                } else {
                    if (nextLyric.Contains("・")) { // Glottal stop
                        if (nextLyric == "・") { // next is VC (VC is not needed)
                            return MakeSimpleResult(currentLyric);
                        } else {
                            vowelUpper = Regex.Match(nextLyric, "[あいうえおんン]").Value;
                            if (vowelUpper == null) {
                                return MakeSimpleResult(currentLyric);
                            }
                            // next is VCV (VC is not needed)
                            var tests = new List<string>{ $"{vowel}{vcvpad}{vowelUpper}・", $"{vowel}{vcvpad}・{vowelUpper}" };
                            if (checkOtoUntilHit(tests, (Note)nextNeighbour, out var oto1) && oto1.Alias.Contains(vcvpad)) {
                                return MakeSimpleResult(currentLyric);
                            }
                            // next is CV (VC is needed)
                            tests = new List<string> { $"{vowel}{vcpad}・" };
                            if (checkOtoUntilHitVc(tests, note, out oto1)) {
                                vcPhoneme = oto1.Alias;
                            } else {
                                return MakeSimpleResult(currentLyric);
                            }
                        }
                    } else {

                        // Without next consonant, VC cannot be created
                        if (!presamp.PhonemeList.TryGetValue(nextAlias, out PresampPhoneme nextPhoneme) || !nextPhoneme.HasConsonant) {
                            return MakeSimpleResult(currentLyric);
                        }
                        var consonant = nextPhoneme.Consonant;

                        // If next is not priority and is convertable VC, VC is not needed
                        // If next is VCV, VC is not needed
                        if (!nextPhoneme.IsPriority) {
                            var nextVCV = $"{vowel}{vcvpad}{nextAlias}";
                            var nextVC = $"{vowel}{vcpad}{nextAlias}";
                            var tests = new List<string>{ nextVCV, nextVC, nextAlias };
                            if (checkOtoUntilHit(tests, nextNeighbour.Value, out var oto1)
                                && (Regex.IsMatch(oto1.Alias, "[aiueonN]" + vcvpad) || Regex.IsMatch(oto1.Alias, "[aiueonN]" + vcpad))) {
                                return MakeSimpleResult(currentLyric);
                            }
                        }

                        // Insert VC
                        vcPhoneme = $"{vowel}{vcpad}{consonant}";
                        var vcPhonemes = new List<string> { vcPhoneme };
                        // find potential substitute symbol
                        if (substituteLookup.TryGetValue(consonant ?? string.Empty, out var con)) {
                            vcPhonemes.Add($"{vowel}{vcpad}{con}");
                        }
                        if (checkOtoUntilHitVc(vcPhonemes, note, out var oto)) {
                            vcPhoneme = oto.Alias;
                        } else {
                            return MakeSimpleResult(currentLyric);
                        }
                    }
                }
                if(!string.IsNullOrEmpty(vcPhoneme)) {
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
                    // Minimam is 30 tick, maximum is half of note
                    vcLength = Convert.ToInt32(Math.Min(totalDuration / 2, Math.Max(30, vcLength * (nextAttr.consonantStretchRatio ?? 1))));
                    
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
            }

            // No next neighbor
            return MakeSimpleResult(currentLyric);
        }

        // make it quicker to check multiple oto occurrences at once rather than spamming if else if
        private bool checkOtoUntilHit(List<string> input, Note note, out UOto oto) {
            oto = default;
            var attr = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;

            var otos = new List<UOto>();
            foreach (string test in input) {
                if (singer.TryGetMappedOto(test + attr.alternate, note.tone + attr.toneShift, attr.voiceColor, out var otoAlt)) {
                    otos.Add(otoAlt);
                } else if (singer.TryGetMappedOto(test, note.tone + attr.toneShift, attr.voiceColor, out var otoCandidacy)) {
                    otos.Add(otoCandidacy);
                }
            }

            string color = attr.voiceColor ?? "";
            if (otos.Count > 0) {
                if (otos.Any(oto => (oto.Color ?? string.Empty) == color)) {
                    oto = otos.Find(oto => (oto.Color ?? string.Empty) == color);
                    return true;
                } else {
                    oto = otos.First();
                    return true;
                }
            }
            return false;
        }

        // checking VCs
        // when VC does not exist, it will not be inserted
        // TODO: fix duplicate voice color fallback bug (for now, this is better than nothing)
        private bool checkOtoUntilHitVc(List<string> input, Note note, out UOto oto) {
            oto = default;
            var attr = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;

            var otos = new List<UOto>();
            foreach (string test in input) {
                if (singer.TryGetMappedOto(test + attr.alternate, note.tone + attr.toneShift, attr.voiceColor, out var otoAlt)) {
                    otos.Add(otoAlt);
                } else if (singer.TryGetMappedOto(test, note.tone + attr.toneShift, attr.voiceColor, out var otoCandidacy)) {
                    otos.Add(otoCandidacy);
                }
            }

            string color = attr.voiceColor ?? "";
            if (otos.Count > 0) {
                if (otos.Any(oto => (oto.Color ?? string.Empty) == color)) {
                    oto = otos.Find(oto => (oto.Color ?? string.Empty) == color);
                    return true;
                } else {
                    return false;
                }
            }
            return false;
        }
    }
}

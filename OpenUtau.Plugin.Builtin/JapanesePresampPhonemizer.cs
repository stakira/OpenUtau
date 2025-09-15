﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Classic;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Japanese presamp Phonemizer", "JA VCV & CVVC", "Maiko", language: "JA")]
    public class JapanesePresampPhonemizer : Phonemizer {

        // CV, VCV, CVVCを含むすべての日本語VBをサポートする予定です。Will support all Japanese VBs including CV, VCV, CVVC
        // 基本的な仕様はpresampに準拠します。Basic behavior conforms to presamp
        // 歌詞に書かれた"強"などの表情は非対応。VoiceColorでやってもらいます。Append suffixes such as "強" written in the lyrics are not supported
        // 喉切り"・"はpresamp.iniに書いてなくても動くようなんとかします。I'll try to make it work even if "・" are not written in presamp.ini
        // Supporting: [VOWEL][CONSONANT][PRIORITY][REPLACE][ALIAS(VCPAD,VCVPAD)]
        // Partial supporting: [NUM][APPEND][PITCH] -> Using to exclude useless characters in lyrics

        private USinger singer;
        private Presamp presamp;
        private UProject project;
        private UTrack track;

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

        public override void SetUp(Note[][] groups, UProject project, UTrack track) {
            this.project = project;
            this.track = track;
        }

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
            var result = new List<Phoneme>();
            bool preCFlag = false;

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
                preCFlag = true;
                if (currentLyric.Contains("・")) {
                    var tests = new List<string> { $"-{vcvpad}{vowelUpper}・", $"・{vcpad}{vowelUpper}", $"{vowelUpper}・", $"・{vowelUpper}", $"-{vcvpad}{vowelUpper}", initial, currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto1)) {
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
                } else if (prevLyric.Contains("っ")) {
                    // try CV
                    var tests = new List<string> { currentLyric, initial };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
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
                    preCFlag = true;
                    var tests = new List<string> { initial, currentLyric };
                    if (checkOtoUntilHit(tests, note, out var oto)) {
                        currentLyric = oto.Alias;
                    }
                }
            }
            result.Add(new Phoneme() { phoneme = currentLyric, index = 0 });

            // Insert "- C"
            if (string.IsNullOrEmpty(note.phoneticHint)
                && preCFlag
                && !currentLyric.Contains(vcvpad)
                && presamp.PhonemeList.TryGetValue(currentAlias, out PresampPhoneme phoneme)
                && phoneme.HasConsonant
                && !presamp.Priorities.Contains(phoneme.Consonant)) {
                if (checkOtoUntilHit(new List<string> { $"-{vcvpad}{phoneme.Consonant}" }, note, 2, out var cOto, out var color)
                    && checkOtoUntilHit(new List<string> { currentLyric }, note, out var oto)) {
                    int endTick = notes[^1].position + notes[^1].duration;
                    var attr = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
                    var cLength = Math.Max(30, -MsToTickAt(-oto.Preutter, endTick) * (attr.consonantStretchRatio ?? 1));

                    if (prevNeighbour != null) {
                        cLength = Math.Min(prevNeighbour.Value.duration / 2, cLength);
                    } else if (prev != null) {
                        cLength = Math.Min(note.position - prev.Value.position - prev.Value.duration, cLength);
                    }

                    result.Insert(0, new Phoneme() {
                        phoneme = cOto.Alias,
                        position = Convert.ToInt32(- cLength),
                        index = 2,
                        expressions = new List<PhonemeExpression>()
                    });
                    if (color != null) {
                        result[0].expressions.Add(new PhonemeExpression() { abbr = Core.Format.Ustx.CLR, value = (int)color });
                    }
                }
            }

            // Insert 2nd phoneme (when next doesn't have hint)
            if (nextNeighbour != null && string.IsNullOrEmpty(nextNeighbour.Value.phoneticHint)) {
                int totalDuration = notes.Sum(n => n.duration);
                if (TickToMs(totalDuration) < 100 && presamp.MustVC == false) {
                    return new Result { phonemes = result.ToArray() };
                }

                var nextLyric = nextNeighbour.Value.lyric.Normalize();
                foreach (var pair in presamp.Replace) {
                    if (pair.Key == nextLyric) {
                        nextLyric = pair.Value;
                    }
                }
                string nextAlias = presamp.ParseAlias(nextLyric)[1]; // exclude useless characters
                string vcPhoneme;
                int? vcColorIndex;

                // Without current vowel, VC cannot be created
                if (!presamp.PhonemeList.TryGetValue(currentAlias, out PresampPhoneme currentPhoneme) || !currentPhoneme.HasVowel) {
                    return new Result { phonemes = result.ToArray() };
                }
                var vowel = currentPhoneme.Vowel;

                if (Regex.IsMatch(nextLyric, "[aiueonN]" + vcvpad) || Regex.IsMatch(nextLyric, "[aiueonN]" + vcpad)) {
                    // next is VCV or VC (VC is not needed)
                    return new Result { phonemes = result.ToArray() };
                } else {
                    if (nextLyric.Contains("・")) { // Glottal stop
                        if (nextLyric == "・") { // next is VC (VC is not needed)
                            return new Result { phonemes = result.ToArray() };
                        } else {
                            vowelUpper = Regex.Match(nextLyric, "[あいうえおんン]").Value;
                            if (vowelUpper == null) {
                                return new Result { phonemes = result.ToArray() };
                            }
                            // next is VCV (VC is not needed)
                            var tests = new List<string> { $"{vowel}{vcvpad}{vowelUpper}・", $"{vowel}{vcvpad}・{vowelUpper}" };
                            if (checkOtoUntilHit(tests, (Note)nextNeighbour, out var oto1) && oto1.Alias.Contains(vcvpad)) {
                                return new Result { phonemes = result.ToArray() };
                            }
                            // next is CV (VC is needed)
                            tests = new List<string> { $"{vowel}{vcpad}・" };
                            if (checkOtoUntilHit(tests, note, 1, out oto1, out var color)) {
                                vcPhoneme = oto1.Alias;
                                vcColorIndex = color;
                            } else {
                                return new Result { phonemes = result.ToArray() };
                            }
                        }
                    } else {

                        // Without next consonant, VC cannot be created
                        if (!presamp.PhonemeList.TryGetValue(nextAlias, out PresampPhoneme nextPhoneme) || !nextPhoneme.HasConsonant) {
                            return new Result { phonemes = result.ToArray() };
                        }
                        var consonant = nextPhoneme.Consonant;

                        // If next is not priority and is convertable VC, VC is not needed
                        // If next is VCV, VC is not needed
                        if (!nextPhoneme.IsPriority) {
                            var nextVCV = $"{vowel}{vcvpad}{nextAlias}";
                            var nextVC = $"{vowel}{vcpad}{nextAlias}";
                            var tests = new List<string> { nextVCV, nextVC, nextAlias };
                            if (checkOtoUntilHit(tests, nextNeighbour.Value, out var oto1)
                                && (Regex.IsMatch(oto1.Alias, "[aiueonN]" + vcvpad) || Regex.IsMatch(oto1.Alias, "[aiueonN]" + vcpad))) {
                                return new Result { phonemes = result.ToArray() };
                            }
                        }

                        // Insert VC
                        vcPhoneme = $"{vowel}{vcpad}{consonant}";
                        var vcPhonemes = new List<string> { vcPhoneme };
                        // find potential substitute symbol
                        if (substituteLookup.TryGetValue(consonant ?? string.Empty, out var con)) {
                            vcPhonemes.Add($"{vowel}{vcpad}{con}");
                        }
                        if (checkOtoUntilHit(vcPhonemes, note, 1, out var oto, out var color)) {
                            vcPhoneme = oto.Alias;
                            vcColorIndex = color;
                        } else {
                            return new Result { phonemes = result.ToArray() };
                        }
                    }
                }
                if (!string.IsNullOrEmpty(vcPhoneme)) {
                    int vcLength = 120;
                    int endTick = notes[^1].position + notes[^1].duration;
                    var nextAttr = nextNeighbour.Value.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
                    if (singer.TryGetMappedOto(nextLyric, nextNeighbour.Value.tone + nextAttr.toneShift, nextAttr.voiceColor, out var nextOto)) {
                        // If overlap is a negative value, vcLength is longer than Preutter
                        if (nextOto.Overlap < 0) {
                            vcLength = -MsToTickAt(-(nextOto.Preutter - nextOto.Overlap), endTick);
                        } else {
                            vcLength = -MsToTickAt(-nextOto.Preutter, endTick);
                        }
                    }
                    // Minimam is 30 tick, maximum is half of note
                    vcLength = Convert.ToInt32(Math.Min(totalDuration / 2, Math.Max(30, vcLength * (nextAttr.consonantStretchRatio ?? 1))));

                    result.Add(new Phoneme() {
                        phoneme = vcPhoneme,
                        position = totalDuration - vcLength,
                        index = 1,
                        expressions = new List<PhonemeExpression>()
                    });
                    if (vcColorIndex != null) {
                        result.First(p => p.index == 1).expressions.Add(new PhonemeExpression() { abbr = Core.Format.Ustx.CLR, value = (int)vcColorIndex });
                    }
                }
            }

            return new Result { phonemes = result.ToArray() };
        }

        // make it quicker to check multiple oto occurrences at once rather than spamming if else if
        private bool checkOtoUntilHit(List<string> input, Note note, out UOto oto) {
            oto = default;
            var attr = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
            // track.TryGetExpression(project, Core.Format.Ustx.CLR, out var trackExp);
            // string color = attr.voiceColor ?? trackExp.descriptor.options[(int)trackExp.value];
            string color = attr.voiceColor ?? string.Empty;

            var otos = new List<UOto>();
            foreach (string test in input) {
                if (singer.TryGetMappedOto(test + attr.alternate, note.tone + attr.toneShift, color, out var otoAlt)) {
                    otos.Add(otoAlt);
                } else if (singer.TryGetMappedOto(test, note.tone + attr.toneShift, color, out var otoCandidacy)) {
                    otos.Add(otoCandidacy);
                }
            }

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
        private bool checkOtoUntilHit(List<string> input, Note note, int index, out UOto oto, out int? colorIndex) {
            oto = default;
            colorIndex = null;
            var attr = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == index) ?? default;
            var attr0 = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
            string color = attr.voiceColor ?? attr0.voiceColor ?? string.Empty;

            var otos = new List<UOto>();
            foreach (string test in input) {
                if (singer.TryGetMappedOto(test + attr.alternate, note.tone + attr.toneShift, color, out var otoAlt)) {
                    otos.Add(otoAlt);
                } else if (singer.TryGetMappedOto(test, note.tone + attr.toneShift, color, out var otoCandidacy)) {
                    otos.Add(otoCandidacy);
                }
            }

            if (otos.Count > 0) {
                if (otos.Any(oto => (oto.Color ?? string.Empty) == color)) {
                    oto = otos.Find(oto => (oto.Color ?? string.Empty) == color);
                    if (track.VoiceColorExp.options.Contains(color)) {
                        colorIndex = Array.IndexOf(track.VoiceColorExp.options, color);
                    }
                    return true;
                } else if (index != 1 && index != 2) {
                    oto = otos.First();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Convert ms to tick at a given reference tick position
        /// </summary>
        /// <param name="offsetMs">Duration in ms</param>
        /// <param name="refTick">Reference tick position</param>
        /// <returns>Duration in ticks</returns>
        public int MsToTickAt(double offsetMs, int refTick) {
            return timeAxis.TicksBetweenMsPos(
                timeAxis.TickPosToMsPos(refTick),
                timeAxis.TickPosToMsPos(refTick) + offsetMs);
        }
    }
}

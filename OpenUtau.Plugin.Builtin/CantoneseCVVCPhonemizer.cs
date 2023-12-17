using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core.G2p;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    /// <summary>
    /// Cantonese CVVC phonemizer.
    /// It works similarly to the Chinese CVVC phonemizer, including presamp.ini requirement.
    /// The big difference is that it converts hanzi to jyutping instead of pinyin.
    /// </summary>
    [Phonemizer("Cantonese CVVC Phonemizer", "ZH-YUE CVVC", "Lotte V", language: "ZH-YUE")]
    public class CantoneseCVVCPhonemizer : Phonemizer {
        private Dictionary<string, string> vowels = new Dictionary<string, string>();
        private Dictionary<string, string> consonants = new Dictionary<string, string>();
        private USinger singer;
        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            var lyric = notes[0].lyric;
            string consonant = consonants.TryGetValue(lyric, out consonant) ? consonant : lyric;
            string prevVowel = "-";
            if (prevNeighbour != null) {
                var prevLyric = prevNeighbour.Value.lyric;
                if (vowels.TryGetValue(prevLyric, out var vowel)) {
                    prevVowel = vowel;
                }
            };
            var attr0 = notes[0].phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
            var attr1 = notes[0].phonemeAttributes?.FirstOrDefault(attr => attr.index == 1) ?? default;
            var attr2 = notes[0].phonemeAttributes?.FirstOrDefault(attr => attr.index == 2) ?? default;
            if (lyric == "-" || lyric.ToLowerInvariant() == "r") {
                if (singer.TryGetMappedOto($"{prevVowel} R", notes[0].tone + attr0.toneShift, attr0.voiceColor, out var oto1)) {
                    return MakeSimpleResult(oto1.Alias);
                }
                return MakeSimpleResult($"{prevVowel} R");
            }
            string currVowel = vowels.TryGetValue(lyric, out currVowel) ? currVowel : lyric;
            int totalDuration = notes.Sum(n => n.duration); // totalDuration of current note

            if (singer.TryGetMappedOto($"{prevVowel} {lyric}", notes[0].tone + attr0.toneShift, attr0.voiceColor, out var oto)) {
                if (nextNeighbour == null && singer.TryGetMappedOto($"{currVowel} R", notes[0].tone + attr1.toneShift, attr1.voiceColor, out var oto1)) {
                    // automatically add ending if present
                    return new Result {
                        phonemes = new Phoneme[] {
                                new Phoneme() {
                                    phoneme = oto.Alias,
                                },
                                new Phoneme() {
                                    phoneme = oto1.Alias,
                                    position = totalDuration - (totalDuration / 6),
                                },
                            },
                    };
                }
                return MakeSimpleResult(oto.Alias);
            }
            int vcLen = 120;
            if (singer.TryGetMappedOto(lyric, notes[0].tone + attr1.toneShift, attr1.voiceColor, out var cvOto)) {
                vcLen = MsToTick(cvOto.Preutter);
                if (cvOto.Overlap == 0 && vcLen < 120) {
                    vcLen = Math.Min(120, vcLen * 2); // explosive consonant with short preutter.
                }
                if (cvOto.Overlap < 0) {
                    vcLen = MsToTick(cvOto.Preutter - cvOto.Overlap);
                }
            }

            if (singer.TryGetMappedOto(lyric, notes[0].tone + attr0.toneShift, attr0.voiceColor, out var cvOtoSimple)) {
                lyric = cvOtoSimple.Alias;
            }

            var vcPhoneme = $"{prevVowel} {consonant}";
            if (prevNeighbour != null) {
                if (singer.TryGetMappedOto(vcPhoneme, prevNeighbour.Value.tone + attr0.toneShift, attr0.voiceColor, out oto)) {
                    vcPhoneme = oto.Alias;
                }
                // prevDuration calculated on basis of previous note length
                int prevDuration = prevNeighbour.Value.duration;
                // vcLength depends on the Vel of the current base note
                vcLen = Convert.ToInt32(Math.Min(prevDuration / 1.5, Math.Max(30, vcLen * (attr1.consonantStretchRatio ?? 1))));
            } else {
                if (singer.TryGetMappedOto(vcPhoneme, notes[0].tone + attr0.toneShift, attr0.voiceColor, out oto)) {
                    vcPhoneme = oto.Alias;
                }
                // no previous note, so length can be minimum velocity regardless of oto
                vcLen = Convert.ToInt32(Math.Min(vcLen * 2, Math.Max(30, vcLen * (attr1.consonantStretchRatio ?? 1))));
            }

            if (nextNeighbour == null) { // automatically add ending if present
                if (singer.TryGetMappedOto($"{prevVowel} {lyric}", notes[0].tone + attr0.toneShift, attr0.voiceColor, out var oto0)) {
                    if (singer.TryGetMappedOto($"{currVowel} R", notes[0].tone + attr1.toneShift, attr1.voiceColor, out var otoEnd)) {
                        // automatically add ending if present
                        return new Result {
                            phonemes = new Phoneme[] {
                                new Phoneme() {
                                    phoneme = oto0.Alias,
                                },
                                new Phoneme() {
                                    phoneme = otoEnd.Alias,
                                    position = totalDuration - (totalDuration / 6),
                                },
                            },
                        };
                    }
                } else {
                    // use vc if present
                    if (prevNeighbour == null && singer.TryGetMappedOto(vcPhoneme, notes[0].tone + attr0.toneShift, attr0.voiceColor, out var vcOto1)) {
                        vcPhoneme = vcOto1.Alias;
                        // automatically add ending if present
                        if (singer.TryGetMappedOto($"{currVowel} R", notes[0].tone + attr2.toneShift, attr2.voiceColor, out var otoEnd)) {
                            return new Result {
                                phonemes = new Phoneme[] {
                                    new Phoneme() {
                                        phoneme = vcPhoneme,
                                        position = -vcLen,
                                },
                                    new Phoneme() {
                                        phoneme = cvOto?.Alias ?? lyric,
                                },
                                    new Phoneme() {
                                        phoneme = otoEnd.Alias,
                                        position = totalDuration - (totalDuration / 6),
                                },
                            },
                            };
                        }
                    } else if (prevNeighbour != null && singer.TryGetMappedOto(vcPhoneme, prevNeighbour.Value.tone + attr0.toneShift, attr0.voiceColor, out var vcOto2)) {
                        vcPhoneme = vcOto2.Alias;
                        // automatically add ending if present
                        if (singer.TryGetMappedOto($"{currVowel} R", notes[0].tone + attr2.toneShift, attr2.voiceColor, out var otoEnd)) {
                            return new Result {
                                phonemes = new Phoneme[] {
                                    new Phoneme() {
                                        phoneme = vcPhoneme,
                                        position = -vcLen,
                                },
                                    new Phoneme() {
                                        phoneme = cvOto?.Alias ?? lyric,
                                },
                                    new Phoneme() {
                                        phoneme = otoEnd.Alias,
                                        position = totalDuration - (totalDuration / 6),
                                },
                            },
                            };
                        }
                    } // just base note and ending
                    if (singer.TryGetMappedOto($"{currVowel} R", notes[0].tone + attr1.toneShift, attr1.voiceColor, out var otoEnd1)) {
                        return new Result {
                            phonemes = new Phoneme[] {
                                new Phoneme() {
                                    phoneme = cvOtoSimple?.Alias ?? lyric,
                                },
                                new Phoneme() {
                                    phoneme = otoEnd1.Alias,
                                    position = totalDuration - (totalDuration / 6),
                                },
                            },
                        };
                    }
                }
            }

            if (singer.TryGetMappedOto(vcPhoneme, notes[0].tone + attr0.toneShift, attr0.voiceColor, out oto)) {
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme() {
                            phoneme = vcPhoneme,
                            position = -vcLen,
                        },
                        new Phoneme() {
                            phoneme = cvOto?.Alias ?? lyric,
                        },
                    },
                };
            }
            return MakeSimpleResult(cvOtoSimple?.Alias ?? lyric);
        }

        public override void SetSinger(USinger singer) {
            if (this.singer == singer) {
                return;
            }
            this.singer = singer;
            vowels.Clear();
            consonants.Clear();
            if (this.singer == null) {
                return;
            }
            try {
                string file = Path.Combine(singer.Location, "presamp.ini");
                using (var reader = new StreamReader(file, singer.TextFileEncoding)) {
                    var blocks = Ini.ReadBlocks(reader, file, @"\[\w+\]");
                    var vowelLines = blocks.Find(block => block.header == "[VOWEL]").lines;
                    foreach (var iniLine in vowelLines) {
                        var parts = iniLine.line.Split('=');
                        if (parts.Length >= 3) {
                            string vowelLower = parts[0];
                            string vowelUpper = parts[1];
                            string[] sounds = parts[2].Split(',');
                            foreach (var sound in sounds) {
                                vowels[sound] = vowelLower;
                            }
                        }
                    }
                    var consonantLines = blocks.Find(block => block.header == "[CONSONANT]").lines;
                    foreach (var iniLine in consonantLines) {
                        var parts = iniLine.line.Split('=');
                        if (parts.Length >= 3) {
                            string consonant = parts[0];
                            string[] sounds = parts[1].Split(',');
                            foreach (var sound in sounds) {
                                consonants[sound] = consonant;
                            }
                        }
                    }
                    var priority = blocks.Find(block => block.header == "PRIORITY");
                    var replace = blocks.Find(block => block.header == "REPLACE");
                    var alias = blocks.Find(block => block.header == "ALIAS");
                }
            } catch (Exception e) {
                Log.Error(e, "failed to load presamp.ini");
            }
        }

        /// <summary>
        /// Converts hanzi notes to jyutping phonemes.
        /// </summary>
        /// <param name="groups"></param>
        public override void SetUp(Note[][] groups) {
            JyutpingConversion.RomanizeNotes(groups);
        }

        /// <summary>
        /// Converts hanzi to jyutping, based on G2P.
        /// </summary>
        public class JyutpingConversion {
            public static Note[] ChangeLyric(Note[] group, string lyric) {
                var oldNote = group[0];
                group[0] = new Note {
                    lyric = lyric,
                    phoneticHint = oldNote.phoneticHint,
                    tone = oldNote.tone,
                    position = oldNote.position,
                    duration = oldNote.duration,
                    phonemeAttributes = oldNote.phonemeAttributes,
                };
                return group;
            }

            public static string[] Romanize(IEnumerable<string> lyrics) {
                var lyricsArray = lyrics.ToArray();
                var hanziLyrics = lyricsArray
                    .Where(ZhG2p.CantoneseInstance.IsHanzi)
                    .ToList();
                var jyutpingResult = ZhG2p.CantoneseInstance.Convert(hanziLyrics, false, false).ToLower().Split();
                if (jyutpingResult == null) {
                    return lyricsArray;
                }
                var jyutpingIndex = 0;
                for (int i = 0; i < lyricsArray.Length; i++) {
                    if (lyricsArray[i].Length == 1 && ZhG2p.CantoneseInstance.IsHanzi(lyricsArray[i])) {
                        lyricsArray[i] = jyutpingResult[jyutpingIndex];
                        jyutpingIndex++;
                    }
                }
                return lyricsArray;
            }

            public static void RomanizeNotes(Note[][] groups) {
                var ResultLyrics = Romanize(groups.Select(group => group[0].lyric));
                Enumerable.Zip(groups, ResultLyrics, ChangeLyric).Last();
            }

            public void SetUp(Note[][] groups) {
                RomanizeNotes(groups);
            }
        }
    }
}

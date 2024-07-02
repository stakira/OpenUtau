using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Melanchall.DryWetMidi.Interaction;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Thai VCCV Phonemizer", "TH VCCV", "PRINTmov", language: "TH")]
    public class ThaiVCCVPhonemizer : Phonemizer {
        static readonly string[] vowels = new string[] {
            "a", "i", "u", "e", "o", "@", "Q", "3", "6", "1", "ia", "ua", "I", "8"
        };

        static readonly string[] diphthongs = new string[] {
            "r", "l", "w"
        };

        static readonly string[] consonants = new string[] {
            "b", "ch", "d", "f", "g", "h", "j", "k", "kh", "l", "m", "n", "p", "ph", "r", "s", "t", "th", "w", "y"
        };

        static readonly string[] endingConsonants = new string[] {
            "b", "ch", "d", "f", "g", "h", "j", "k", "kh", "l", "m", "n", "p", "ph", "r", "s", "t", "th", "w", "y"
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
            if (!string.IsNullOrEmpty(note.phoneticHint)) {
                currentLyric = note.phoneticHint.Normalize();
            }

            var phonemes = new List<Phoneme>();

            List<string> tests = new List<string>();

            string prevTemp = "";
            if (prevNeighbour != null) {
                prevTemp = prevNeighbour.Value.lyric;
            }
            var prevTh = ParseInput(prevTemp);

            var noteTh = ParseInput(currentLyric);

            if (noteTh.Consonant != null && noteTh.Dipthong == null && noteTh.Vowel != null) {
                if (checkOtoUntilHit(new string[] { noteTh.Consonant + noteTh.Vowel }, note, out var tempOto)) {
                    tests.Add(tempOto.Alias);
                }
            } else if (noteTh.Consonant != null && noteTh.Dipthong != null && noteTh.Vowel != null) {
                if (checkOtoUntilHit(new string[] { noteTh.Consonant + noteTh.Dipthong + noteTh.Vowel }, note, out var tempOto)) {
                    tests.Add(tempOto.Alias);
                } else {
                    if (checkOtoUntilHit(new string[] { noteTh.Consonant + noteTh.Dipthong }, note, out tempOto)) {
                        tests.Add(tempOto.Alias);
                    }
                    if (checkOtoUntilHit(new string[] { noteTh.Dipthong + noteTh.Vowel }, note, out tempOto)) {
                        tests.Add(tempOto.Alias);
                    }
                }
            }

            if (noteTh.Consonant == null && noteTh.Vowel != null) {
                if (prevTh.EndingConsonant != null && checkOtoUntilHit(new string[] { prevTh.EndingConsonant + noteTh.Vowel }, note, out var tempOto)) {
                    tests.Add(tempOto.Alias);
                } else if (prevTh.Vowel != null && checkOtoUntilHit(new string[] { prevTh.Vowel + noteTh.Vowel }, note, out tempOto)) {
                    tests.Add(tempOto.Alias);
                } else if (checkOtoUntilHit(new string[] { noteTh.Vowel }, note, out tempOto)) {
                    tests.Add(tempOto.Alias);
                }
            }

            if (noteTh.EndingConsonant != null && noteTh.Vowel != null) {
                if (checkOtoUntilHit(new string[] { noteTh.Vowel + noteTh.EndingConsonant }, note, out var tempOto)) {
                    tests.Add(tempOto.Alias);
                }
            } else if (nextNeighbour != null && noteTh.Vowel != null) {
                var nextTh = ParseInput(nextNeighbour.Value.lyric);
                if (checkOtoUntilHit(new string[] { noteTh.Vowel + " " + nextTh.Consonant }, note, out var tempOto)) {
                    tests.Add(tempOto.Alias);
                }
            }

            if (prevNeighbour == null && tests.Count >= 1) {
                if (checkOtoUntilHit(new string[] { "-" + tests[0] }, note, out var tempOto)) {
                    tests[0] = (tempOto.Alias);
                }
            }

            if (nextNeighbour == null && tests.Count >= 1) {
                if (noteTh.EndingConsonant == null) {
                    if (checkOtoUntilHit(new string[] { noteTh.Vowel + "-" }, note, out var tempOto)) {
                        tests.Add(tempOto.Alias);
                    }
                } else {
                    if (checkOtoUntilHit(new string[] { tests[tests.Count - 1] + "-" }, note, out var tempOto)) {
                        tests[tests.Count - 1] = (tempOto.Alias);
                    }
                }
            }

            if (tests.Count <= 0) {
                if (checkOtoUntilHit(new string[] { currentLyric }, note, out var tempOto)) {
                    tests.Add(currentLyric);
                }
            }

            if (checkOtoUntilHit(tests.ToArray(), note, out var oto)) {

                var noteDuration = notes.Sum(n => n.duration);

                for (int i = 0; i < tests.ToArray().Length; i++) {

                    int position = 0;
                    int vcPosition = noteDuration - 120;

                    if (nextNeighbour != null && tests[i].Contains(" "))
                    {
                        var nextLyric = nextNeighbour.Value.lyric.Normalize();
                        if (!string.IsNullOrEmpty(nextNeighbour.Value.phoneticHint)) {
                            nextLyric = nextNeighbour.Value.phoneticHint.Normalize();
                        }
                        var nextTh = ParseInput(nextLyric);
                        var nextCheck = nextTh.Vowel;
                        if (nextTh.Consonant != null) {
                            nextCheck = nextTh.Consonant + nextTh.Vowel;
                        }
                        if(nextTh.Dipthong != null) {
                            nextCheck = nextTh.Consonant + nextTh.Dipthong + nextTh.Vowel;
                        }
                        var nextAttr = nextNeighbour.Value.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
                        if (singer.TryGetMappedOto(nextCheck, nextNeighbour.Value.tone + nextAttr.toneShift, nextAttr.voiceColor, out var nextOto)) {
                            if (oto.Overlap > 0) {
                                vcPosition = noteDuration - MsToTick(nextOto.Overlap) - MsToTick(nextOto.Preutter);
                            }
                        }
                    }
                    

                    if (noteTh.Dipthong == null || tests.Count <= 2) {
                        if (i == 1) {
                            position = Math.Max((int)(noteDuration * 0.75), vcPosition);
                        }
                    } else {
                        if (i == 1) {
                            position = Math.Min((int)(noteDuration * 0.1), 60);
                        } else if (i == 2) {
                            position = Math.Max((int)(noteDuration * 0.75), vcPosition);
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
            string consonant = null;
            string dipthong = null;
            string vowel = null;
            string endingConsonant = null;

            if (input == null) {
                return (null, null, null, null);
            }

            if (input.Length > 3) {
                foreach (var dip in diphthongs) {
                    if (input[1].ToString().Equals(dip) || input[2].ToString().Equals(dip)) {
                        dipthong = dip;
                    }
                }
            }

            foreach (var con in consonants) {
                if (input.StartsWith(con)) {
                    if (consonant == null || consonant.Length < con.Length) {
                        consonant = con;
                    }
                }
                if (input.EndsWith(con)) {
                    if (endingConsonant == null || endingConsonant.Length < con.Length) {
                        endingConsonant = con;
                    }
                }
            }

            foreach (var vow in vowels) {
                if (input.Contains(vow)) {
                    if (vowel == null || vowel.Length < vow.Length) {
                        vowel = vow;
                    }
                }
            }

            return (consonant, dipthong, vowel, endingConsonant);
        }
    }
}

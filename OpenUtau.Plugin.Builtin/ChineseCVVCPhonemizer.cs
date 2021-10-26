using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Chinese CVVC Phonemizer (WIP)", "ZH CVVC")]
    public class ChineseCVVCPhonemizer : Phonemizer {
        private Dictionary<string, string> vowels = new Dictionary<string, string>();
        private Dictionary<string, string> consonants = new Dictionary<string, string>();
        private USinger singer;

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour) {
            var phonemes = GetPhonemes(notes[0]);
            if (phonemes == null && notes[0].lyric != "-") {
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme() {
                            phoneme = notes[0].lyric,
                        },
                    },
                };
            }
            var prevPhonemes = prevNeighbour == null ? null : GetPhonemes(prevNeighbour.Value);
            string before = prevPhonemes == null ? "-" : prevPhonemes.Last();
            int totalDuration = notes.Sum(n => n.duration);
            if (notes[0].lyric == "-") {
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme() {
                            phoneme = $"{before} R",
                        },
                    },
                };
            }
            if (before == "-") {
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme() {
                            phoneme = $"{before} {notes[0].lyric}",
                        },
                    },
                };
            }
            if (phonemes.Length == 1) {
                if (singer.TryGetMappedOto($"{before} {notes[0].lyric}", notes[0].tone, out var _)) {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme() {
                                phoneme = $"{before} {notes[0].lyric}",
                            },
                        },
                    };
                }
                if (singer.TryGetMappedOto($"{before} {phonemes[0]}", notes[0].tone, out var _)) {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme() {
                                phoneme = $"{before} {phonemes[0]}",
                            },
                        },
                    };
                }
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme() {
                            phoneme = notes[0].lyric,
                        },
                    },
                };
            }
            int vcLen = 120;
            if (singer.TryGetMappedOto(notes[0].lyric, notes[0].tone, out var oto)) {
                vcLen = MsToTick(oto.Preutter);
                if (oto.Overlap == 0 && vcLen < 120) {
                    vcLen = Math.Min(120, vcLen * 2); // explosive consonant with short preutter.
                }
            }
            return new Result {
                phonemes = new Phoneme[] {
                    new Phoneme() {
                        phoneme = $"{before} {phonemes[0]}",
                        position = -vcLen,
                    },
                    new Phoneme() {
                        phoneme = $"{notes[0].lyric}",
                    },
                },
            };
        }

        private string[] GetPhonemes(Note note) {
            if (!string.IsNullOrEmpty(note.phoneticHint)) {
                return note.phoneticHint.Split();
            }
            if (vowels.TryGetValue(note.lyric, out var vowel)) {
                if (note.lyric == vowel) {
                    return new string[] { vowel };
                }
            } else {
                return null;
            }
            if (consonants.TryGetValue(note.lyric, out var consonant)) {
                return new string[] { consonant, vowel };
            } else {
                return new string[] { vowel };
            }
        }

        public override void SetSinger(USinger singer) {
            if (this.singer == singer) {
                return;
            }
            this.singer = singer;
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
    }
}

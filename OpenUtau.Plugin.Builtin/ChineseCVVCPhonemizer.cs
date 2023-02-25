using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Chinese CVVC Phonemizer", "ZH CVVC", language: "ZH")]
    public class ChineseCVVCPhonemizer : BaseChinesePhonemizer {
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
            if (lyric == "-" || lyric.ToLowerInvariant() == "r") {
                if (singer.TryGetMappedOto($"{prevVowel} R", notes[0].tone + attr0.toneShift, attr0.voiceColor, out var oto1)) {
                    return MakeSimpleResult(oto1.Alias);
                }
                return MakeSimpleResult($"{prevVowel} R");
            }
            int totalDuration = notes.Sum(n => n.duration);
            if (singer.TryGetMappedOto($"{prevVowel} {lyric}", notes[0].tone + attr0.toneShift, attr0.voiceColor, out var oto)) {
                return MakeSimpleResult(oto.Alias);
            }
            int vcLen = 120;
            if (singer.TryGetMappedOto(lyric, notes[0].tone + attr0.toneShift, attr0.voiceColor, out var cvOto)) {
                vcLen = MsToTick(cvOto.Preutter);
                if (cvOto.Overlap == 0 && vcLen < 120) {
                    vcLen = Math.Min(120, vcLen * 2); // explosive consonant with short preutter.
                }
            }
            if (singer.TryGetMappedOto($"{prevVowel} {consonant}", notes[0].tone + attr0.toneShift, attr0.voiceColor, out oto)) {
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme() {
                            phoneme = oto.Alias,
                            position = -vcLen,
                        },
                        new Phoneme() {
                            phoneme = cvOto?.Alias ?? lyric,
                        },
                    },
                };
            }
            return MakeSimpleResult(cvOto?.Alias ?? lyric);
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
    }
}

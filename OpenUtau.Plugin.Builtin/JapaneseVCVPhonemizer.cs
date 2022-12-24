using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using WanaKanaNet;
using static OpenUtau.Api.Phonemizer;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Japanese VCV Phonemizer", "JA VCV", language: "JA")]
    public class JapaneseVCVPhonemizer : Phonemizer {
     
        private USinger singer;

        // Simply stores the singer in a field.
        public override void SetSinger(USinger singer) => this.singer = singer;

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            var note = notes[0];
            if (!string.IsNullOrEmpty(note.phoneticHint)) {
                // If a hint is present, returns the hint.
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme {
                            phoneme = note.phoneticHint,
                        }
                    },
                };
            }
            // The alias for no previous neighbour note. For example, "- な" for "な".
            var phoneme = $"- {note.lyric}";
            if (prevNeighbour != null) { 
                // If there is a previous neighbour note, first get its hint or lyric.
                var lyric = prevNeighbour?.phoneticHint ?? prevNeighbour?.lyric;
                // Get the trailing vowel from wanakana
                var vowel = WanaKana.ToRomaji(lyric);
                // Get the last symbol in the string
                char vow = vowel[vowel.Length- 1];
                // Now replace "- な" initially set to "a な".
                phoneme = $"{vow} {note.lyric}";
                
            }
            // Get color
            string color = string.Empty;
            int toneShift = 0;
            if (note.phonemeAttributes != null) {
                var attr = note.phonemeAttributes.FirstOrDefault(attr => attr.index == 0);
                color = attr.voiceColor;
                toneShift = attr.toneShift;
            }
            if (singer.TryGetMappedOto(phoneme, note.tone + toneShift, color, out var oto)) {
                phoneme = oto.Alias;
            } else if (singer.TryGetMappedOto(note.lyric, note.tone + toneShift, color, out oto)) {
                phoneme = oto.Alias;
            } else {
                phoneme = note.lyric;
            }
            return new Result {
                phonemes = new Phoneme[] {
                    new Phoneme {
                        phoneme = phoneme,
                    }
                },
            };
        }
    }
}

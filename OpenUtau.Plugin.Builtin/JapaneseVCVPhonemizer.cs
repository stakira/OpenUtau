using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Japanese VCV Phonemizer", "JA VCV", language: "JA")]
    public class JapaneseVCVPhonemizer : Phonemizer {
        /// <summary>
        /// The lookup table to convert a hiragana to its tail vowel.
        /// </summary>
        static readonly string[] vowels = new string[] {
            "a=ぁ,あ,か,が,さ,ざ,た,だ,な,は,ば,ぱ,ま,ゃ,や,ら,わ,ァ,ア,カ,ガ,サ,ザ,タ,ダ,ナ,ハ,バ,パ,マ,ャ,ヤ,ラ,ワ,a",
            "e=ぇ,え,け,げ,せ,ぜ,て,で,ね,へ,べ,ぺ,め,れ,ゑ,ェ,エ,ケ,ゲ,セ,ゼ,テ,デ,ネ,ヘ,ベ,ペ,メ,レ,ヱ,e",
            "i=ぃ,い,き,ぎ,し,じ,ち,ぢ,に,ひ,び,ぴ,み,り,ゐ,ィ,イ,キ,ギ,シ,ジ,チ,ヂ,ニ,ヒ,ビ,ピ,ミ,リ,ヰ,i",
            "o=ぉ,お,こ,ご,そ,ぞ,と,ど,の,ほ,ぼ,ぽ,も,ょ,よ,ろ,を,ォ,オ,コ,ゴ,ソ,ゾ,ト,ド,ノ,ホ,ボ,ポ,モ,ョ,ヨ,ロ,ヲ,o",
            "n=ん,n",
            "u=ぅ,う,く,ぐ,す,ず,つ,づ,ぬ,ふ,ぶ,ぷ,む,ゅ,ゆ,る,ゥ,ウ,ク,グ,ス,ズ,ツ,ヅ,ヌ,フ,ブ,プ,ム,ュ,ユ,ル,ヴ,u",
            "N=ン,ng",
        };

        static readonly Dictionary<string, string> vowelLookup;

        static JapaneseVCVPhonemizer() {
            // Converts the lookup table from raw strings to a dictionary for better performance.
            vowelLookup = vowels.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
        }

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
                // Get the last unicode element of the hint or lyric. For example, "ゃ" from "きゃ" or "- きゃ".
                var unicode = ToUnicodeElements(lyric);
                // Look up the trailing vowel. For example "a" for "ゃ".
                if (vowelLookup.TryGetValue(unicode.LastOrDefault() ?? string.Empty, out var vow)) {
                    // Now replace "- な" initially set to "a な".
                    phoneme = $"{vow} {note.lyric}";
                }
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

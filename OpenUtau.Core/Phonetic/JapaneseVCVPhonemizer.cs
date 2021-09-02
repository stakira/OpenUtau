using System.Collections.Generic;
using System.Linq;

namespace OpenUtau.Core {
    public class JapaneseVCVPhonemizer : Phonemizer {
        static readonly string[] vowels = new string[] {
            "a=ぁ,あ,か,が,さ,ざ,た,だ,な,は,ば,ぱ,ま,ゃ,や,ら,わ,ァ,ア,カ,ガ,サ,ザ,タ,ダ,ナ,ハ,バ,パ,マ,ャ,ヤ,ラ,ワ",
            "e=ぇ,え,け,げ,せ,ぜ,て,で,ね,へ,べ,ぺ,め,れ,ゑ,ェ,エ,ケ,ゲ,セ,ゼ,テ,デ,ネ,ヘ,ベ,ペ,メ,レ,ヱ",
            "i=ぃ,い,き,ぎ,し,じ,ち,ぢ,に,ひ,び,ぴ,み,り,ゐ,ィ,イ,キ,ギ,シ,ジ,チ,ヂ,ニ,ヒ,ビ,ピ,ミ,リ,ヰ",
            "o=ぉ,お,こ,ご,そ,ぞ,と,ど,の,ほ,ぼ,ぽ,も,ょ,よ,ろ,を,ォ,オ,コ,ゴ,ソ,ゾ,ト,ド,ノ,ホ,ボ,ポ,モ,ョ,ヨ,ロ,ヲ",
            "n=ん",
            "u=ぅ,う,く,ぐ,す,ず,つ,づ,ぬ,ふ,ぶ,ぷ,む,ゅ,ゆ,る,ゥ,ウ,ク,グ,ス,ズ,ツ,ヅ,ヌ,フ,ブ,プ,ム,ュ,ユ,ル,ヴ",
            "N=ン",
        };

        static readonly Dictionary<string, string> vowelLookup;

        static JapaneseVCVPhonemizer() {
            vowelLookup = vowels.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
        }

        private Ustx.USinger singer;

        public override string Name => "Japanese VCV Phonemizer";
        public override string Tag => "JP VCV";
        public override void SetSinger(Ustx.USinger singer) => this.singer = singer;
        public override Phoneme[] Process(Note note, Note? prev, Note? next) {
            var phoneme = $"- {note.lyric}";
            if (prev != null && !string.IsNullOrEmpty(prev?.lyric)) {
                var lyric = prev?.lyric;
                var unicode = ToUnicodeElements(lyric);
                if (vowelLookup.TryGetValue(unicode.Last(), out var vow)) {
                    phoneme = $"{vow} {note.lyric}";
                }
            }
            phoneme = TryMapPhoneme(phoneme, note.tone, singer);
            if (singer.FindOto(phoneme) == null) {
                phoneme = note.lyric;
            }
            return new Phoneme[] {
                new Phoneme {
                    phoneme = phoneme,
                    duration = note.duration,
                }
            };
        }
    }
}

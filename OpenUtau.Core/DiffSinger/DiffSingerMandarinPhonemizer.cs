using System;
using System.IO;
using K4os.Hash.xxHash;
using TinyPinyin;

using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Vogen;
using System.Linq;

namespace OpenUtau.Core.DiffSinger {
    [Phonemizer("DiffSinger Chinese Mandarin Phonemizer", "DIFFS ZH", language: "ZH")]
    public class DiffsingerMandarinPhonemizer : VogenMandarinPhonemizer {
        DiffSingerSinger singer;

        public override void SetSinger(USinger singer) {
            this.singer = singer as DiffSingerSinger;//#TODO：为什么转不进去
        }

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevs) {
            /*if (!partResult.TryGetValue(notes[0].position, out var phonemes)) {
                throw new Exception("Part result not found");
            }
            return new Result {
                phonemes = phonemes
                    .Select((tu) => new Phoneme() {
                        phoneme = tu.Item1,
                        position = tu.Item2,
                    })
                    .ToArray(),
            };*/
            string lyric = notes[0].lyric;
            //汉字转拼音
            if (lyric.Length > 0 && PinyinHelper.IsChinese(lyric[0])) {
                lyric = PinyinHelper.GetPinyin(lyric).ToLowerInvariant();
            }
            var phones = singer.phoneDict[lyric];
            if (phones.Item1 == "") {//仅韵母
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme {phoneme = phones.Item2,}
                    },
                };
            } else {
                //使用vogen的辅音时间
                Result VogenResult = base.Process(notes, prev, next, prevNeighbour, nextNeighbour, prevs);
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme {phoneme = phones.Item1, position = VogenResult.phonemes[0].position},
                        new Phoneme {phoneme = phones.Item2, position = 0}
                    },
                };
            }
        }
    }
}

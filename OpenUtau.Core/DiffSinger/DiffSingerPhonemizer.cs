using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using K4os.Hash.xxHash;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using TinyPinyin;

namespace OpenUtau.Core.DiffSinger {
    [Phonemizer("DiffSinger Phonemizer", "DIFFS")]
    public class DiffSingerPhonemizer : Phonemizer {
        DiffSingerSinger singer;

        public DiffSingerPhonemizer() {
            
        }

        public override void SetSinger(USinger singer) {
            this.singer = singer as DiffSingerSinger;//#TODO：为什么转不进去
        }

        static ulong HashNoteGroups(Note[][] notes) {
            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {
                    foreach (var ns in notes) {
                        foreach (var n in ns) {
                            writer.Write(n.lyric);
                            writer.Write(n.position);
                            writer.Write(n.duration);
                            writer.Write(n.tone);
                        }
                    }
                    return XXH64.DigestOf(stream.ToArray());
                }
            }
        }

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevs) {
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
                //计算辅音时间，暂时按照120tick或上一音符长度的一半中的较小者#TODO
                int prevDuration = 240;
                if (prevNeighbour != null) {
                    prevDuration = prevNeighbour.Value.duration;
                }
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme {phoneme = phones.Item1, position = -Math.Min(120,prevDuration)},
                        new Phoneme {phoneme = phones.Item2, position = 0}
                    },
                };
            }
        }

        public override void CleanUp() {
            //partResult.Clear();
        }
    }
}

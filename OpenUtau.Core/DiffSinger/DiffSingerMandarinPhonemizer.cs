using System;
using System.IO;
using TinyPinyin;

using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Vogen;
using System.Collections.Generic;
using Serilog;

namespace OpenUtau.Core.DiffSinger {
    [Phonemizer("DiffSinger Chinese Mandarin Phonemizer", "DIFFS ZH", language: "ZH")]
    public class DiffsingerMandarinPhonemizer : VogenMandarinPhonemizer {
        DiffSingerSinger singer;
        Dictionary<string, Tuple<string, string>> phoneDict = new Dictionary<string, Tuple<string, string>>();

        //初始化
        public override void SetSinger(USinger singer) {
            if (this.singer == singer) {
                return;
            }
            this.singer = singer as DiffSingerSinger;
            if (this.singer == null) {
                return;
            }
            //导入拼音转音素字典
            try {
                phoneDict.Clear();
            HashSet<string> phonemesSet = new HashSet<string> { "SP", "AP" };
            string path = Path.Combine(singer.Location, "dsdict.txt");
            phoneDict.Add("AP", new Tuple<string, string>("", "AP"));
            phoneDict.Add("SP", new Tuple<string, string>("", "SP"));
                foreach (string line in File.ReadLines(path, singer.TextFileEncoding)) {
                    string[] elements = line.Split("\t");
                    elements[1] = elements[1].Trim();
                    if (elements[1].Contains(" ")) {//声母+韵母
                        string[] phones = elements[1].Split(" ");
                        phoneDict.Add(elements[0].Trim(), new Tuple<string, string>(phones[0], phones[1]));
                        phonemesSet.Add(phones[0]);
                        phonemesSet.Add(phones[1]);
                    } else {//仅韵母
                        phoneDict.Add(elements[0].Trim(), new Tuple<string, string>("", elements[1]));
                        phonemesSet.Add(elements[1]);
                    } 
                }
            }
            catch (Exception e) {
                Log.Error(e, "failed to load dsdict.txt");
            }
        }

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevs) {
            float frameMs = 1000f*512/44100;
            //TODO:变速曲可能会产生错误结果
            int frameTick = MsToTick(frameMs);
            string lyric = notes[0].lyric;
            //汉字转拼音
            if (lyric.Length > 0 && PinyinHelper.IsChinese(lyric[0])) {
                lyric = PinyinHelper.GetPinyin(lyric).ToLowerInvariant();
            }
            var phones = phoneDict[lyric];
            if (phones.Item1 == "") {//仅韵母
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme {phoneme = phones.Item2,}
                    },
                };
            } else {
                //使用vogen的辅音时间
                Result VogenResult = base.Process(notes, prev, next, prevNeighbour, nextNeighbour, prevs);
                //辅音长度至少为1帧
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme {phoneme = phones.Item1, 
                            position = Math.Min(VogenResult.phonemes[0].position,-frameTick)},
                        new Phoneme {phoneme = phones.Item2, position = 0}
                    },
                };
            }
        }
    }
}

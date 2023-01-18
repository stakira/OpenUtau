using System;
using System.Collections.Generic;
using Microsoft.ML.OnnxRuntime;
using System.IO;
using OpenUtau.Api;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using System.Reflection;
using System.Text;
using System.Linq;

namespace OpenUtau.Core.DiffSinger {
    //音素时长模型配置文件
    [Serializable]
    public class DsRhythmizerConfig {
        public string name = "rhythmizer";
        public string model = "model.onnx";
        public string phonemes = "phonemes.txt";
    }

    class DsRhythmizer {
        public string name;
        public string Location;
        public DsRhythmizerConfig config;
        public InferenceSession session;
        public Dictionary<string, string[]> phoneDict;

        //加载字典
        public static Dictionary<string, string[]> LoadPhoneDict(string path, Encoding TextFileEncoding) {
            var phoneDict = new Dictionary<string, string[]>();
            if (File.Exists(path)) {
                foreach (string line in File.ReadLines(path, TextFileEncoding)) {
                    string[] elements = line.Split("\t");
                    phoneDict.Add(elements[0].Trim(), elements[1].Trim().Split(" "));
                }
            }
            return phoneDict;
        }

        //通过名称获取音素时长模型
        public DsRhythmizer(string name) {
            byte[] model;
            this.name = name;
            Location = Path.Combine(PathManager.Inst.DependencyPath, name);
            config = Core.Yaml.DefaultDeserializer.Deserialize<DsRhythmizerConfig>(
                File.ReadAllText(Path.Combine(Location, "vocoder.yaml"),
                    System.Text.Encoding.UTF8));
            phoneDict = LoadPhoneDict(Path.Combine("dsdict.txt"), Encoding.UTF8);
            model = File.ReadAllBytes(Path.Combine(Location, config.model));
            session = Onnx.getInferenceSession(model);
        }
    }

    class DsPhoneme {
        public string phoneme;//音素名称
        public int midi;//音高
        public float midi_dur;//音符总时长
    }

    [Phonemizer("DiffSinger Rhythmizer Phonemizer", "DIFFS RHY", language: "ZH")]
    public class DiffSingerRhythmizerPhonemizer : Phonemizer {
        DiffSingerSinger singer;
        DsRhythmizer rhythmizer;
        Dictionary<string, string[]> phoneDict;
        private Dictionary<int, List<Tuple<string, int>>> partResult = new Dictionary<int, List<Tuple<string, int>>>();

        public override void SetSinger(USinger singer) {
            if (this.singer == singer) {
                return;
            }
            this.singer = singer as DiffSingerSinger;
            if (this.singer == null) {
                return;
            }
            //加载音素时长模型
            if (rhythmizer == null || rhythmizer.name != this.singer.dsConfig.rhythmizer) {
                rhythmizer = new DsRhythmizer(this.singer.dsConfig.rhythmizer);
            }
            //导入拼音转音素字典，目前仅从时长模型包中导入字典
            phoneDict = rhythmizer.phoneDict;
        }

        //一次性生成全曲音素时长
        public override void SetUp(Note[][] groups) {
            float padding = 0.5f;//段首辅音预留时长
            var phonemes = new List<string> { "AP" };//音素名称列表
            var midi = new List<int> { 0 };//音素音高列表
            var midi_dur = new List<float> { padding };//音素所属的音符时长列表
            //开头的
            if (groups.Length == 0) {
                return;
            }
            var phrase = new List<Note>() { groups[0][0] };
            for (int i = 1; i < groups.Length; ++i) {
                if (groups[i - 1][0].position + groups[i - 1][0].duration == groups[i][0].position) {
                    phrase.Add(groups[i][0]);
                } else {
                    phrase.Clear();
                    phrase.Add(groups[i][0]);
                }
            }
            if (phrase.Count > 0) {
            }
        }

        //OpenUtau Vogen的实现是错误的，连音符会被视为空白时间
        //当上一音符包含连音符且总时长过短时，会使下一音符的辅音过长

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevs) {
            if (!partResult.TryGetValue(notes[0].position, out var phonemes)) {
                throw new Exception("Part result not found");
            }
            return new Result {
                phonemes = phonemes
                    .Select((tu) => new Phoneme() {
                        phoneme = tu.Item1,
                        position = tu.Item2,
                    })
                    .ToArray(),
            };
        }

        public override void CleanUp() {
            partResult.Clear();
        }
    }
}

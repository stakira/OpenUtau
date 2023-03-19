using System;
using System.Collections.Generic;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.IO;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
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

    //音源中的声明文件
    [Serializable]
    public class DsRhythmizerYaml {
        public string rhythmizer = DsRhythmizer.DefaultRhythmizer;
    }

    class DsRhythmizer {
        public string name;
        public string Location;
        public DsRhythmizerConfig config;
        public InferenceSession session;
        public Dictionary<string, string[]> phoneDict;
        public List<string> phonemes = new List<string>();
        //默认包名
        public static string DefaultRhythmizer = "rhythmizer_zh_opencpop_strict";

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
            this.name = name;
            Location = Path.Combine(PathManager.Inst.DependencyPath, name);
            config = Core.Yaml.DefaultDeserializer.Deserialize<DsRhythmizerConfig>(
                File.ReadAllText(Path.Combine(Location, "vocoder.yaml"),
                    System.Text.Encoding.UTF8));
            phoneDict = LoadPhoneDict(Path.Combine(Location,"dsdict.txt"), Encoding.UTF8);
            //导入音素列表
            string phonemesPath = Path.Combine(Location, config.phonemes);
            phonemes = File.ReadLines(phonemesPath, Encoding.UTF8).ToList();
            session = new InferenceSession(Path.Combine(Location, config.model));
        }
    }

    [Phonemizer("DiffSinger Rhythmizer Phonemizer", "DIFFS RHY", language: "ZH")]
    public class DiffSingerRhythmizerPhonemizer : Phonemizer {
        USinger singer;
        DsRhythmizer rhythmizer;
        Dictionary<string, string[]> phoneDict;
        private Dictionary<int, List<Tuple<string, int>>> partResult = new Dictionary<int, List<Tuple<string, int>>>();

        public override void SetSinger(USinger singer) {
            if (this.singer == singer) {
                return;
            }
            this.singer = singer;
            if (this.singer == null) {
                return;
            }
            //加载音素时长模型
            try {
                string rhythmizerName;
                string rhythmizerYamlPath = Path.Combine(singer.Location, "dsrhythmizer.yaml");
                if (File.Exists(rhythmizerYamlPath)) {
                    rhythmizerName = Core.Yaml.DefaultDeserializer.Deserialize<DsRhythmizerYaml>(
                        File.ReadAllText(rhythmizerYamlPath, singer.TextFileEncoding)).rhythmizer;
                } else {
                    rhythmizerName = DsRhythmizer.DefaultRhythmizer;
                }
                if (rhythmizer == null || rhythmizer.name != rhythmizerName) {
                    rhythmizer = new DsRhythmizer(rhythmizerName);
                }
                //导入拼音转音素字典，仅从时长模型包中导入字典
                phoneDict = rhythmizer.phoneDict;
            } catch (Exception ex) {
                return;
            }
        }

        //只要音符改动一点，就会将全曲传入SetUp函数
        //groups为Note的二维数组，其中的每个Note[]表示一个歌词音符及其后面的连音符
        //需要分段调用模型生成全曲音素时长，以防止蝴蝶效应
        public override void SetUp(Note[][] groups) {
            if (groups.Length == 0) {
                return;
            }
            //汉字转拼音
            BaseChinesePhonemizer.RomanizeNotes(groups);
            //将全曲拆分为句子
            var phrase = new List<Note[]> { groups[0] };
            for (int i = 1; i < groups.Length; ++i) {
                //如果上下音符相互衔接，则不分句
                if (groups[i - 1][^1].position + groups[i - 1][^1].duration == groups[i][0].position) {
                    phrase.Add(groups[i]);
                } else {
                    //如果断开了，则处理当前句子，并开启下一句
                    ProcessPart(phrase.ToArray());
                    phrase.Clear();
                    phrase.Add(groups[i]);
                }
            }
            if (phrase.Count > 0) {
                ProcessPart(phrase.ToArray());
            }
        }

        //以句子为单位进行处理，调用模型
        //对于连音符，合并为一个音符输入模型，以防止过短的连音符使辅音缩短
        void ProcessPart(Note[][] phrase) {
            float padding = 0.5f;//段首辅音预留时长
            var phonemes = new List<string> { "SP" };//音素名称列表
            var midi = new List<long> { 0 };//音素音高列表
            var midi_dur = new List<float> { padding };//音素所属的音符时长列表
            var is_slur = new List<bool> { false };//是否为连音符
            List<double> ph_dur;//模型输出的音素时长
            var notePhIndex = new List<int>{ 1 };//每个音符的第一个音素在音素列表上对应的位置
            var phAlignPoints = new List<Tuple<int, double>>();//音素对齐的位置，s，绝对时间
            double offsetMs = timeAxis.TickPosToMsPos(phrase[0][0].position);

            //将音符列表转化为音素列表
            foreach (int groupIndex in Enumerable.Range(0,phrase.Length)) {
                string[] notePhonemes;
                Note[] group = phrase[groupIndex];
                if (group[0].phoneticHint is null) {
                    var lyric = group[0].lyric;
                    
                    if (phoneDict.ContainsKey(lyric)) {
                        notePhonemes = phoneDict[lyric];
                    } else {
                        notePhonemes = new string[] { lyric };
                    }
                } else {
                    notePhonemes = group[0].phoneticHint.Split(" ");
                }
                is_slur.AddRange(Enumerable.Repeat(false, notePhonemes.Length));
                phAlignPoints.Add(new Tuple<int,double>(
                    phonemes.Count + (notePhonemes.Length > 1 ? 1 : 0),//TODO
                    timeAxis.TickPosToMsPos(group[0].position) / 1000
                    ));
                phonemes.AddRange(notePhonemes);
                midi.AddRange(Enumerable.Repeat((long)group[0].tone, notePhonemes.Length));
                notePhIndex.Add(phonemes.Count);

                midi_dur.AddRange(Enumerable.Repeat((float)timeAxis.MsBetweenTickPos(
                    group[0].position, group[^1].position + group[^1].duration) / 1000, notePhonemes.Length));
            }
            var lastNote = phrase[^1][^1];
            phAlignPoints.Add(new Tuple<int, double>(
                phonemes.Count,
                timeAxis.TickPosToMsPos(lastNote.position + lastNote.duration) / 1000));

            //调用Diffsinger音素时长模型
            //ph_dur = session.run(['ph_dur'], {'tokens': tokens, 'midi': midi, 'midi_dur': midi_dur, 'is_slur': is_slur})[0]
            //错误音素皆视为空白SP，输入音素时长模型
            long defaultToken = rhythmizer.phonemes.IndexOf("SP");
            var tokens = phonemes
                .Select(x => (long)(rhythmizer.phonemes.IndexOf(x)))
                .Select(x => x < 0 ? defaultToken : x)
                .ToList();
            var inputs = new List<NamedOnnxValue>();
            inputs.Add(NamedOnnxValue.CreateFromTensor("tokens",
                new DenseTensor<long>(tokens.ToArray(), new int[] { tokens.Count }, false)
                .Reshape(new int[] { 1, tokens.Count })));
            inputs.Add(NamedOnnxValue.CreateFromTensor("midi",
                new DenseTensor<long>(midi.ToArray(), new int[] { midi.Count }, false)
                .Reshape(new int[] { 1, midi.Count })));
            inputs.Add(NamedOnnxValue.CreateFromTensor("midi_dur",
                new DenseTensor<float>(midi_dur.ToArray(), new int[] { midi_dur.Count }, false)
                .Reshape(new int[] { 1, midi_dur.Count })));
            inputs.Add(NamedOnnxValue.CreateFromTensor("is_slur",
                new DenseTensor<bool>(is_slur.ToArray(), new int[] { is_slur.Count }, false)
                .Reshape(new int[] { 1, is_slur.Count })));
            var outputs = rhythmizer.session.Run(inputs);
            ph_dur = outputs.First().AsTensor<float>().Select(x => (double)x).ToList();
            //对齐，将时长序列转化为位置序列，单位s
            var positions = new List<double>();
            List<double> alignGroup = ph_dur.GetRange(0, phAlignPoints[0].Item1);
            //开头辅音不缩放
            positions.AddRange(stretch(alignGroup, 1, phAlignPoints[0].Item2));
            //其他音素每一段线性缩放//pairwise(alignGroups)
            foreach(var pair in phAlignPoints.Zip(phAlignPoints.Skip(1), (a, b) => Tuple.Create(a, b))){
                var currAlignPoint = pair.Item1;
                var nextAlignPoint = pair.Item2;
                alignGroup = ph_dur.GetRange(currAlignPoint.Item1, nextAlignPoint.Item1 - currAlignPoint.Item1);
                double ratio = (nextAlignPoint.Item2 - currAlignPoint.Item2)/alignGroup.Sum();
                positions.AddRange(stretch(alignGroup, ratio, nextAlignPoint.Item2));

            }
            //将位置序列转化为tick，填入结果列表
            int index = 1;
            foreach(int groupIndex in Enumerable.Range(0, phrase.Length)) {
                Note[] group = phrase[groupIndex];
                var noteResult = new List<Tuple<string, int>>();
                if (group[0].lyric.StartsWith("+")) {
                    continue;
                }
                double notePos = timeAxis.TickPosToMsPos(group[0].position);//音符起点位置，单位ms
                for(int phIndex = notePhIndex[groupIndex]; phIndex < notePhIndex[groupIndex + 1]; ++phIndex) {
                    noteResult.Add(Tuple.Create(phonemes[phIndex], timeAxis.TicksBetweenMsPos(
                       notePos, positions[phIndex] * 1000)));
                }
                partResult[group[0].position] = noteResult;
            }
        }

        //计算累加
        public static IEnumerable<double> CumulativeSum(IEnumerable<double> sequence, double start = 0) {
            double sum = start;
            foreach (var item in sequence) {
                sum += item;
                yield return sum;
            }
        }
        
        //缩放音素时长序列
        public List<double> stretch(IList<double> source, double ratio, double endPos) {
            //source：音素时长序列，单位s
            //ratio：缩放比例
            //endPos：目标终点时刻，单位s
            //输出：缩放后的音素位置，单位s
            double startPos = endPos - source.Sum() * ratio;
            var result = CumulativeSum(source.Select(x => x * ratio).Prepend(0),startPos).ToList();
            result.RemoveAt(result.Count - 1);
            return result;
        }

        //OpenUtau Vogen的实现是错误的，连音符会被视为空白时间，使句子断开
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

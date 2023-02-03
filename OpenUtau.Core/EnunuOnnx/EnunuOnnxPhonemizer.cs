using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OpenUtau.Core.Ustx;
using OpenUtau.Api;
using OpenUtau.Core.EnunuOnnx.nnmnkwii.io.hts;
using Microsoft.ML.OnnxRuntime;
using OpenUtau.Core.EnunuOnnx.nnmnkwii.frontend;
using Microsoft.ML.OnnxRuntime.Tensors;
using Serilog;

//This phonemizer is a pure C# implemention of the ENUNU phonemizer,
//which aims at providing all ML-based synthesizer developers with a useable phonemizer,
//This phonemizer uses onnxruntime to run ML models. No Python needed. 

namespace OpenUtau.Core.EnunuOnnx {
    [Phonemizer("Enunu Onnx Phonemizer", "ENUNU X")]
    public class EnunuOnnxPhonemizer : Phonemizer {
        readonly string PhonemizerType = "ENUNU X";

        //singer-related informations used by this phonemizer
        //basic informations
        protected USinger singer;
        EnunuConfig enuconfig;

        //information used by HTS writer
        Dictionary<string, string[]> phoneDict = new Dictionary<string, string[]>();
        string[] vowels = new string[] { "a", "i", "u", "e", "o", "A", "I", "U", "E", "O", "N", "ae", "AE" };
        string[] breaks = new string[] { "br", "cl" };
        string[] pauses = new string[] { "pau" };
        string[] silences = new string[] { "sil" };

        //model and information used by model
        InferenceSession timelagModel;
        InferenceSession? durationModel;
        Dictionary<int, Tuple<string, List<Regex>>> binaryDict = new Dictionary<int, Tuple<string, List<Regex>>>();
        Dictionary<int, Tuple<string, Regex>> numericDict = new Dictionary<int, Tuple<string, Regex>>();
        int[] pitchIndices = new int[] { };
        Scaler durationInScaler = new Scaler();
        Scaler durationOutScaler = new Scaler();
        Scaler timelagInScaler = new Scaler();

        //information used by openutau phonemizer
        protected IG2p g2p;

        //result caching
        private Dictionary<int, List<Tuple<string, int>>> partResult = new Dictionary<int, List<Tuple<string, int>>>();

        int paddingMs = 500;//段首辅音预留时长

        public override void SetSinger(USinger singer) {
            this.singer = singer;
            if (singer == null) {
                return;
            }
            //Load enuconfig
            string rootPath;
            if (File.Exists(Path.Join(singer.Location, "enunux", "enuconfig.yaml"))) {
                rootPath = Path.Combine(singer.Location, "enunux");
            } else {
                rootPath = singer.Location;
            }
            var configPath = Path.Join(rootPath, "enuconfig.yaml");
            var configTxt = File.ReadAllText(configPath);
            RawEnunuConfig config = Yaml.DefaultDeserializer.Deserialize<RawEnunuConfig>(configTxt);
            enuconfig = config.Convert();
            //Load Dictionary
            LoadDict(Path.Join(rootPath, enuconfig.tablePath), singer.TextFileEncoding);
            //Load question set
            LoadQuestionSet(Path.Join(rootPath, enuconfig.questionPath), singer.TextFileEncoding);
            //Load timing models
            var timelagModelPath = Path.Join(rootPath, enuconfig.modelDir, "timelag");
            timelagModelPath = Path.Join(timelagModelPath, enuconfig.timelag.checkpoint);//TODO
            if (timelagModelPath.EndsWith(".pth")) {
                timelagModelPath = timelagModelPath[..^4] + ".onnx";
            }
            this.timelagModel = new InferenceSession(timelagModelPath);
            var durationModelPath = Path.Join(rootPath, enuconfig.modelDir, "duration");
            durationModelPath = Path.Join(durationModelPath, enuconfig.duration.checkpoint);
            if (durationModelPath.EndsWith(".pth")) {
                durationModelPath = durationModelPath[..^4] + ".onnx";
            }
            this.durationModel = new InferenceSession(durationModelPath);
            //Load scalers
            var timelagInScalerPath = Path.Join(rootPath, enuconfig.statsDir, "in_timelag_scaler.json");
            this.timelagInScaler = Scaler.load(timelagInScalerPath, singer.TextFileEncoding);
            var timelagOutScalerPath = Path.Join(rootPath, enuconfig.statsDir, "out_timelag_scaler.json");
            this.timelagInScaler = Scaler.load(timelagOutScalerPath, singer.TextFileEncoding);
            var durationInScalerPath = Path.Join(rootPath, enuconfig.statsDir, "in_duration_scaler.json");
            this.durationInScaler = Scaler.load(durationInScalerPath, singer.TextFileEncoding);
            var durationOutScalerPath = Path.Join(rootPath, enuconfig.statsDir, "out_duration_scaler.json");
            this.durationOutScaler = Scaler.load(durationOutScalerPath, singer.TextFileEncoding);
            //Load enunux.yaml
            this.g2p = LoadG2p(rootPath);
        }

        protected IG2p LoadG2p(string rootPath) {
            var g2ps = new List<IG2p>();

            // Load dictionary from singer folder.
            string file = Path.Combine(rootPath, "enunux.yaml");
            if (File.Exists(file)) {
                try {
                    g2ps.Add(G2pDictionary.NewBuilder().Load(File.ReadAllText(file)).Build());
                } catch (Exception e) {
                    Log.Error(e, $"Failed to load {file}");
                }
            }
            return new G2pFallbacks(g2ps.ToArray());
        }

        public void LoadDict(string path, Encoding encoding) {
            if (path.EndsWith(".conf")) {
                LoadConf(path, encoding);
            } else {
                LoadTable(path, encoding);
            }
        }

        public void LoadTable(string path, Encoding encoding) {
            phoneDict.Clear();
            var lines = File.ReadLines(path, encoding);
            foreach (var line in lines) {
                var lineSplit = line.Split();
                phoneDict[lineSplit[0]] = lineSplit[1..];
            }
        }

        public void LoadConf(string path, Encoding encoding) {
            phoneDict.Clear();
            phoneDict["SILENCES"] = new string[] { "sil" };
            phoneDict["PAUSES"] = new string[] { "pau" };
            phoneDict["BREAK"] = new string[] { "br" };
            var lines = File.ReadLines(path, encoding);
            foreach (var line in lines) {
                if (line.Contains('=')) {
                    var lineSplit = line.Split("=");
                    var key = lineSplit[0];
                    var value = lineSplit[1];
                    var phonemes = value.Trim(new char[] { '\"' }).Split(",");
                    phoneDict[key] = phonemes;
                }
            }
        }

        public void LoadQuestionSet(string path, Encoding encoding) {
            var result = hts.load_question_set(path, encoding: encoding);
            binaryDict = result.Item1;
            numericDict = result.Item2;
            pitchIndices = Enumerable.Range(binaryDict.Count, 3).ToArray();
        }

        public override void SetUp(Note[][] groups) {
            if (groups.Length == 0) {
                return;
            }

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

        public string GetPhonemeType(string phoneme) {
            if (phoneme == "xx") {
                return "xx";
            }
            if (vowels.Contains(phoneme)) {
                return "v";
            }
            if (pauses.Contains(phoneme)) {
                return "p";
            }
            if (silences.Contains(phoneme)) {
                return "s";
            }
            if (breaks.Contains(phoneme)) {
                return "b";
            }
            return "c";
        }

        HTSNote NoteToHTSNote(Note[] group, int startTick) {
            //convert Phonemizer.Note to HTSNote
            var htsNote = new HTSNote(
                phonemeStrs: new string[] { group[0].phoneticHint },
                tone: group[0].tone,
                startms: (int)timeAxis.MsBetweenTickPos(startTick, group[0].position) + paddingMs,
                endms: (int)timeAxis.MsBetweenTickPos(startTick, group[^1].position + group[^1].duration) + paddingMs,
                durationTicks: group[^1].position + group[^1].duration - group[0].position
                );
            if (group[0].phoneticHint != null) {
                htsNote.phonemeStrs = group[0].phoneticHint.Split();
                return htsNote;
            }
            if (phoneDict.ContainsKey(group[0].lyric)) {
                htsNote.phonemeStrs = phoneDict[group[0].lyric];
            }
            return htsNote;
        }

        HTSPhoneme[] HTSNoteToPhonemes(HTSNote htsNote) {
            var htsPhonemes = htsNote.phonemeStrs.Select(x => new HTSPhoneme(x, htsNote)).ToArray();
            int prevVowelPos = -1;
            foreach (int i in Enumerable.Range(0, htsPhonemes.Length)) {
                htsPhonemes[i].position = i + 1;
                htsPhonemes[i].position_backward = htsPhonemes.Length - i;
                htsPhonemes[i].type = GetPhonemeType(htsPhonemes[i].phoneme);
                if (htsPhonemes[i].type == "v") {
                    prevVowelPos = i;
                } else {
                    if (prevVowelPos > 0) {
                        htsPhonemes[i].distance_from_previous_vowel = i - prevVowelPos;
                    }
                }
            }
            int nextVowelPos = -1;
            for (int i = htsPhonemes.Length - 1; i > 0; --i) {
                if (htsPhonemes[i].type == "v") {
                    nextVowelPos = i;
                } else {
                    if (nextVowelPos > 0) {
                        htsPhonemes[i].distance_to_next_vowel = nextVowelPos - i;
                    }
                }
            }


            return htsPhonemes;
        }

        void ProcessPart(Note[][] phrase) {
            int offsetTick = phrase[0][0].position;
            int sentenceDurMs = paddingMs + (int)timeAxis.MsBetweenTickPos(
                phrase[0][0].position, phrase[^1][^1].position + phrase[^1][^1].duration);
            int paddingTicks = timeAxis.MsPosToTickPos(paddingMs);
            var notePhIndex = new List<int> { 1 };//每个音符的第一个音素在音素列表上对应的位置
            var phAlignPoints = new List<Tuple<int, double>>();//音素对齐的位置，Ms，绝对时间
            HTSNote PaddingNote = new HTSNote(
                phonemeStrs: new string[] { "sil" },
                tone: 0,
                startms: 0,
                endms: paddingMs,
                durationTicks: paddingTicks
            );
            //convert OpenUtau notes to HTS Labels
            var htsNotes = phrase.Select(x => NoteToHTSNote(x, offsetTick)).Prepend(PaddingNote).ToList();
            foreach (int i in Enumerable.Range(0, htsNotes.Count)) {
                htsNotes[i].index = i;
                htsNotes[i].indexBackwards = htsNotes.Count - i;
                htsNotes[i].sentenceDurMs = sentenceDurMs;
                if (i > 0) {
                    htsNotes[i].prev = htsNotes[i - 1];
                    htsNotes[i - 1].next = htsNotes[i];
                }
            }
            var htsPhonemes = new List<HTSPhoneme>();
            htsPhonemes.AddRange(HTSNoteToPhonemes(PaddingNote));
            for (int noteIndex = 1; noteIndex < htsNotes.Count; ++noteIndex) {
                HTSNote htsNote = htsNotes[noteIndex];
                var notePhonemes = HTSNoteToPhonemes(htsNote);
                //分析第几个音素与音符对齐
                int firstVowelIndex = 0;//The index of the first vowel in the note
                for(int phIndex = 0; phIndex < htsNote.phonemeStrs.Length; phIndex++) {
                    if (g2p.IsVowel(htsNote.phonemeStrs[phIndex])) {
                        //TODO
                        firstVowelIndex = phIndex;
                        break;
                    }
                }
                phAlignPoints.Add(new Tuple<int, double>(
                    htsPhonemes.Count + (firstVowelIndex),//TODO
                    timeAxis.TickPosToMsPos(phrase[noteIndex-1][0].position)
                    ));
                htsPhonemes.AddRange(notePhonemes);
                notePhIndex.Add(htsPhonemes.Count);
            }
            var lastNote = phrase[^1][^1];
            phAlignPoints.Add(new Tuple<int, double>(
                htsPhonemes.Count,
                timeAxis.TickPosToMsPos(lastNote.position + lastNote.duration) / 1000));
            for (int i = 1; i < htsPhonemes.Count; ++i) {
                htsPhonemes[i].prev = htsPhonemes[i - 1];
                htsPhonemes[i - 1].next = htsPhonemes[i];
            }

            var linguistic_features = merlin.linguistic_features(
                hts.load(htsPhonemes.Select(x => x.dump())),
                binaryDict,
                numericDict
            );
            //log_f0_conditioning
            float lastMidi = 60;            
            foreach(int idx in pitchIndices) {
                foreach(var line in linguistic_features) {
                    if (line[idx] > 0) {
                        lastMidi = line[idx];
                    }
                    line[idx] = (float)Math.Log(MusicMath.ToneToFreq(lastMidi));
                }
            }

            int phonemesCount = linguistic_features.Count;
            int featuresDim = linguistic_features[0].Count;

            //timelag inference
            /*var timelag_linguistic_features = timelagInScaler.transformed(linguistic_features);
            var timelagInputs = new List<NamedOnnxValue>();
            timelagInputs.Add(NamedOnnxValue.CreateFromTensor("linguistic_features",
                new DenseTensor<float>(
                    timelag_linguistic_features.SelectMany(x => x).ToArray(),
                    new int[] { 1, phonemesCount, featuresDim }, false)));
            timelagInputs.Add(NamedOnnxValue.CreateFromTensor("lengths",
                new DenseTensor<long>(new long[] { (long)phonemesCount },
                new int[] { 1 }, false)));
            var timelagOutputs = timelagModel.Run(timelagInputs);*/
            //TODO

            //duration inference
            var duration_linguistic_features = durationInScaler.transformed(linguistic_features);
            var durationInputs = new List<NamedOnnxValue>();
            durationInputs.Add(NamedOnnxValue.CreateFromTensor("linguistic_features",
                new DenseTensor<float>(
                    duration_linguistic_features.SelectMany(x => x).ToArray(),
                    new int[] { 1, phonemesCount, featuresDim }, false)));
            durationInputs.Add(NamedOnnxValue.CreateFromTensor("lengths",
                new DenseTensor<long>(new long[] { (long)phonemesCount },
                new int[] { 1 }, false)));
            var durationOutputs = durationModel.Run(durationInputs);
            var ph_dur_float = durationOutputs.First().AsTensor<float>().ToList();
            durationOutScaler[0].inverse_transform(ph_dur_float);//Phoneme Duration Result in Ms
            var ph_dur = ph_dur_float.Select(x => (double)x).ToList();

            //TODO
            //对齐，将时长序列转化为位置序列，单位ms
            var positions = new List<double>();
            List<double> alignGroup = ph_dur.GetRange(0, phAlignPoints[0].Item1 - 1);
            //开头辅音不缩放
            positions.AddRange(stretch(alignGroup, 1, phAlignPoints[0].Item2));
            //其他音素每一段线性缩放//pairwise(alignGroups)
            foreach (var pair in phAlignPoints.Zip(phAlignPoints.Skip(1), (a, b) => Tuple.Create(a, b))) {
                var currAlignPoint = pair.Item1;
                var nextAlignPoint = pair.Item2;
                alignGroup = ph_dur.GetRange(currAlignPoint.Item1, nextAlignPoint.Item1 - currAlignPoint.Item1);
                double ratio = (nextAlignPoint.Item2 - currAlignPoint.Item2) / alignGroup.Sum();
                positions.AddRange(stretch(alignGroup, ratio, nextAlignPoint.Item2));
            }
            //将位置序列转化为tick，填入结果列表
            int index = 1;
            foreach (int groupIndex in Enumerable.Range(0, phrase.Length)) {
                Note[] group = phrase[groupIndex];
                var noteResult = new List<Tuple<string, int>>();
                if (group[0].lyric.StartsWith("+")) {
                    continue;
                }
                double notePos = timeAxis.TickPosToMsPos(group[0].position);//音符起点位置，单位ms
                for (int phIndex = notePhIndex[groupIndex]; phIndex < notePhIndex[groupIndex + 1]; ++phIndex) {
                    noteResult.Add(Tuple.Create(htsPhonemes[phIndex].phoneme, timeAxis.TicksBetweenMsPos(
                       notePos, positions[phIndex-1])));
                }
                partResult[group[0].position] = noteResult;
            }

        }

        //缩放音素时长序列
        public List<double> stretch(IList<double> source, double ratio, double endPos) {
            //source：音素时长序列，单位ms
            //ratio：缩放比例
            //endPos：目标终点时刻，单位ms
            //输出：缩放后的音素位置，单位ms
            double startPos = endPos - source.Sum() * ratio;
            var result = CumulativeSum(source.Select(x => x * ratio).Prepend(0), startPos).ToList();
            result.RemoveAt(result.Count - 1);
            return result;
        }

        //计算累加
        public static IEnumerable<double> CumulativeSum(IEnumerable<double> sequence, double start = 0) {
            double sum = start;
            foreach (var item in sequence) {
                sum += item;
                yield return sum;
            }
        }

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
    }

    
}

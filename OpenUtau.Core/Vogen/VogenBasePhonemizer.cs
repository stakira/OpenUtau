using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Vogen {
    public abstract class VogenBasePhonemizer : Phonemizer {
        private Dictionary<int, List<Tuple<string, int>>> partResult = new Dictionary<int, List<Tuple<string, int>>>();

        protected InferenceSession G2p { get; set; }
        protected InferenceSession Prosody { get; set; }

        public VogenBasePhonemizer() {
            G2p ??= new InferenceSession(Data.VogenRes.g2p_man);
            Prosody ??= new InferenceSession(Data.VogenRes.po_man);
        }

        protected abstract string LangPrefix { get; }
        protected virtual string Romanize(string lyric) => lyric;

        public override void SetSinger(USinger singer) { }

        public override void SetUp(Note[] notes) {
            if (notes.Length == 0) {
                return;
            }
            var phrase = new List<Note>() { notes[0] };
            for (int i = 1; i < notes.Length; ++i) {
                if (notes[i - 1].position + notes[i - 1].duration == notes[i].position) {
                    phrase.Add(notes[i]);
                } else {
                    ProcessPart(phrase);
                    phrase.Clear();
                    phrase.Add(notes[i]);
                }
            }
            if (phrase.Count > 0) {
                ProcessPart(phrase);
            }
        }

        void ProcessPart(IList<Note> notes) {
            float padding = (float)TickToMs(240);
            int totalDur = notes.Sum(n => n.duration);
            var lyrics = new string[notes.Count, 8];
            for (int i = 0; i < notes.Count; ++i) {
                var lyric = Romanize(notes[i].lyric);
                lyric = lyric.PadRight(8, '\0');
                for (int j = 0; j < 8; j++) {
                    lyrics[i, j] = lyric[j].ToString();
                }
            }
            var x = lyrics.ToTensor();
            var inputs = new List<NamedOnnxValue>();
            inputs.Add(NamedOnnxValue.CreateFromTensor("letters", x));
            var outputs = G2p.Run(inputs);
            var phsTensor = outputs.First().AsTensor<string>();
            outputs.Dispose();

            var phs = new List<string>();
            var chPhCounts = new List<long>();
            var noteDursSec = new List<float>();
            phs.Insert(0, "");
            chPhCounts.Add(1);
            noteDursSec.Add(padding);
            for (int i = 0; i < notes.Count; ++i) {
                chPhCounts.Add(0);
                for (int j = 0; j < 4; ++j) {
                    if (phsTensor[i, j] != string.Empty) {
                        chPhCounts[chPhCounts.Count - 1]++;
                        phs.Add($"{LangPrefix}{phsTensor[i, j]}");
                    }
                }
                noteDursSec.Add((float)TickToMs(notes[i].duration) / 1000);
            }
            phs.Add("");
            chPhCounts.Add(1);
            noteDursSec.Add(padding);

            inputs.Clear();
            inputs.Add(NamedOnnxValue.CreateFromTensor("phs",
                new DenseTensor<string>(phs.ToArray(), new int[] { phs.Count })));
            inputs.Add(NamedOnnxValue.CreateFromTensor("chPhCounts",
                new DenseTensor<long>(chPhCounts.ToArray(), new int[] { chPhCounts.Count })));
            inputs.Add(NamedOnnxValue.CreateFromTensor("noteDursSec",
                new DenseTensor<float>(noteDursSec.ToArray(), new int[] { noteDursSec.Count })));
            outputs = Prosody.Run(inputs);
            var positions = outputs.First()
                .AsTensor<float>()
                .Select(t => t - padding)
                .ToArray();
            outputs.Dispose();

            int index = 1;
            double notePos = 0;
            for (int i = 1; i < chPhCounts.Count - 1; ++i) {
                var phonemes = new List<Tuple<string, int>>();
                for (int j = index; j < index + chPhCounts[i]; ++j) {
                    phonemes.Add(Tuple.Create(phs[j], MsToTick((positions[j] - notePos) * 1000)));
                }
                partResult[notes[i - 1].position] = phonemes;
                index += (int)chPhCounts[i];
                notePos += noteDursSec[i];
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

        public override void CleanUp() {
            partResult.Clear();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using K4os.Hash.xxHash;
using Serilog;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using OpenUtau.Api;
using OpenUtau.Core.Render;
using OpenUtau.Core.Util;

namespace OpenUtau.Core.DiffSinger{
    public struct VarianceResult{
        public float[]? energy;
        public float[]? breathiness;
        public float[]? voicing;
        public float[]? tension;
    }
    public class DsVariance : IDisposable{
        string rootPath;
        DsConfig dsConfig;
        List<string> phonemes;
        ulong linguisticHash;
        ulong varianceHash;
        InferenceSession linguisticModel;
        InferenceSession varianceModel;
        IG2p g2p;
        float frameMs;
        const float headMs = DiffSingerUtils.headMs;
        const float tailMs = DiffSingerUtils.tailMs;
        DiffSingerSpeakerEmbedManager speakerEmbedManager;


        public DsVariance(string rootPath)
        {
            this.rootPath = rootPath;
            dsConfig = Yaml.DefaultDeserializer.Deserialize<DsConfig>(
                File.ReadAllText(Path.Combine(rootPath, "dsconfig.yaml"),
                    Encoding.UTF8));
            //Load phonemes list
            string phonemesPath = Path.Combine(rootPath, dsConfig.phonemes);
            phonemes = File.ReadLines(phonemesPath, Encoding.UTF8).ToList();
            //Load models
            var linguisticModelPath = Path.Join(rootPath, dsConfig.linguistic);
            var linguisticModelBytes = File.ReadAllBytes(linguisticModelPath);
            linguisticHash = XXH64.DigestOf(linguisticModelBytes);
            linguisticModel = Onnx.getInferenceSession(linguisticModelBytes);
            var varianceModelPath = Path.Join(rootPath, dsConfig.variance);
            var varianceModelBytes = File.ReadAllBytes(varianceModelPath);
            varianceHash = XXH64.DigestOf(varianceModelBytes);
            varianceModel = Onnx.getInferenceSession(varianceModelBytes);
            frameMs = 1000f * dsConfig.hop_size / dsConfig.sample_rate;
            //Load g2p
            g2p = LoadG2p(rootPath);
        }

        protected IG2p LoadG2p(string rootPath) {
            var g2ps = new List<IG2p>();
            // Load dictionary from singer folder.
            string file = Path.Combine(rootPath, "dsdict.yaml");
            if (File.Exists(file)) {
                try {
                    g2ps.Add(G2pDictionary.NewBuilder().Load(File.ReadAllText(file)).Build());
                } catch (Exception e) {
                    Log.Error(e, $"Failed to load {file}");
                }
            }
            return new G2pFallbacks(g2ps.ToArray());
        }

        public DiffSingerSpeakerEmbedManager getSpeakerEmbedManager(){
            if(speakerEmbedManager is null) {
                speakerEmbedManager = new DiffSingerSpeakerEmbedManager(dsConfig, rootPath);
            }
            return speakerEmbedManager;
        }

        public VarianceResult Process(RenderPhrase phrase){
            int headFrames = (int)Math.Round(headMs / frameMs);
            int tailFrames = (int)Math.Round(tailMs / frameMs);
            //Linguistic Encoder
            var linguisticInputs = new List<NamedOnnxValue>();
            var tokens = phrase.phones
                .Select(p => (Int64)phonemes.IndexOf(p.phoneme))
                .Prepend((Int64)phonemes.IndexOf("SP"))
                .Append((Int64)phonemes.IndexOf("SP"))
                .ToArray();
            var ph_dur = phrase.phones
                .Select(p => (int)Math.Round(p.endMs / frameMs) - (int)Math.Round(p.positionMs / frameMs))//prevent cumulative error
                .Prepend(headFrames)
                .Append(tailFrames)
                .ToArray();
            int totalFrames = ph_dur.Sum();
            linguisticInputs.Add(NamedOnnxValue.CreateFromTensor("tokens",
                new DenseTensor<Int64>(tokens, new int[] { tokens.Length }, false)
                .Reshape(new int[] { 1, tokens.Length })));
            if(dsConfig.predict_dur){
                //if predict_dur is true, use word encode mode
                var vowelIds = Enumerable.Range(0,phrase.phones.Length)
                    .Where(i=>g2p.IsVowel(phrase.phones[i].phoneme))
                    .ToArray();
                var word_div = vowelIds.Zip(vowelIds.Skip(1),(a,b)=>(Int64)(b-a))
                    .Prepend(vowelIds[0] + 1)
                    .Append(phrase.phones.Length - vowelIds[^1] + 1)
                    .ToArray();
                var word_dur = vowelIds.Zip(vowelIds.Skip(1),
                        (a,b)=>(Int64)(phrase.phones[b-1].endMs/frameMs) - (Int64)(phrase.phones[a].positionMs/frameMs))
                    .Prepend((Int64)(phrase.phones[vowelIds[0]].positionMs/frameMs) - (Int64)(phrase.phones[0].positionMs/frameMs) + headFrames)
                    .Append((Int64)(phrase.notes[^1].endMs/frameMs) - (Int64)(phrase.phones[vowelIds[^1]].positionMs/frameMs) + tailFrames)
                    .ToArray();
                linguisticInputs.Add(NamedOnnxValue.CreateFromTensor("word_div",
                    new DenseTensor<Int64>(word_div, new int[] { word_div.Length }, false)
                    .Reshape(new int[] { 1, word_div.Length })));
                linguisticInputs.Add(NamedOnnxValue.CreateFromTensor("word_dur",
                    new DenseTensor<Int64>(word_dur, new int[] { word_dur.Length }, false)
                    .Reshape(new int[] { 1, word_dur.Length })));
            }else{
                //if predict_dur is false, use phoneme encode mode
                linguisticInputs.Add(NamedOnnxValue.CreateFromTensor("ph_dur",
                    new DenseTensor<Int64>(ph_dur.Select(x=>(Int64)x).ToArray(), new int[] { ph_dur.Length }, false)
                    .Reshape(new int[] { 1, ph_dur.Length })));
            }

            Onnx.VerifyInputNames(linguisticModel, linguisticInputs);
            var linguisticCache = Preferences.Default.DiffSingerTensorCache
                ? new DiffSingerCache(linguisticHash, linguisticInputs)
                : null;
            var linguisticOutputs = linguisticCache?.Load();
            if (linguisticOutputs is null) {
                linguisticOutputs = linguisticModel.Run(linguisticInputs).Cast<NamedOnnxValue>().ToList();
                linguisticCache?.Save(linguisticOutputs);
            }
            Tensor<float> encoder_out = linguisticOutputs
                .Where(o => o.Name == "encoder_out")
                .First()
                .AsTensor<float>();

            //Variance Predictor
            var pitch = DiffSingerUtils.SampleCurve(phrase, phrase.pitches, 0, frameMs, totalFrames, headFrames, tailFrames, 
                x => x * 0.01)
                .Select(f => (float)f).ToArray();

            var varianceInputs = new List<NamedOnnxValue>();
            varianceInputs.Add(NamedOnnxValue.CreateFromTensor("encoder_out", encoder_out));
            varianceInputs.Add(NamedOnnxValue.CreateFromTensor("ph_dur",
                new DenseTensor<Int64>(ph_dur.Select(x=>(Int64)x).ToArray(), new int[] { ph_dur.Length }, false)
                .Reshape(new int[] { 1, ph_dur.Length })));
            varianceInputs.Add(NamedOnnxValue.CreateFromTensor("pitch",
                new DenseTensor<float>(pitch, new int[] { pitch.Length }, false)
                .Reshape(new int[] { 1, totalFrames })));
            if (dsConfig.predict_energy) {
                var energy = Enumerable.Repeat(0f, totalFrames).ToArray();
                varianceInputs.Add(NamedOnnxValue.CreateFromTensor("energy",
                    new DenseTensor<float>(energy, new int[] { energy.Length }, false)
                        .Reshape(new int[] { 1, totalFrames })));
            }
            if (dsConfig.predict_breathiness) {
                var breathiness = Enumerable.Repeat(0f, totalFrames).ToArray();
                varianceInputs.Add(NamedOnnxValue.CreateFromTensor("breathiness",
                    new DenseTensor<float>(breathiness, new int[] { breathiness.Length }, false)
                        .Reshape(new int[] { 1, totalFrames })));
            }
            if (dsConfig.predict_voicing) {
                var voicing = Enumerable.Repeat(0f, totalFrames).ToArray();
                varianceInputs.Add(NamedOnnxValue.CreateFromTensor("voicing",
                    new DenseTensor<float>(voicing, new int[] { voicing.Length }, false)
                        .Reshape(new int[] { 1, totalFrames })));
            }
            if (dsConfig.predict_tension) {
                var tension = Enumerable.Repeat(0f, totalFrames).ToArray();
                varianceInputs.Add(NamedOnnxValue.CreateFromTensor("tension",
                    new DenseTensor<float>(tension, new int[] { tension.Length }, false)
                        .Reshape(new int[] { 1, totalFrames })));
            }

            var numVariances = new[] {
                dsConfig.predict_energy,
                dsConfig.predict_breathiness,
                dsConfig.predict_voicing,
                dsConfig.predict_tension,
            }.Sum(Convert.ToInt32);
            var retake = Enumerable.Repeat(true, totalFrames * numVariances).ToArray();
            varianceInputs.Add(NamedOnnxValue.CreateFromTensor("retake",
                new DenseTensor<bool>(retake, new int[] { retake.Length }, false)
                .Reshape(new int[] { 1, totalFrames, numVariances })));
            var steps = Preferences.Default.DiffSingerSteps;
            if (dsConfig.useContinuousAcceleration) {
                varianceInputs.Add(NamedOnnxValue.CreateFromTensor("steps",
                    new DenseTensor<long>(new long[] { steps }, new int[] { 1 }, false)));
            } else {
                // find a largest integer speedup that are less than 1000 / steps and is a factor of 1000
                long speedup = Math.Max(1, 1000 / steps);
                while (1000 % speedup != 0 && speedup > 1) {
                    speedup--;
                }
                varianceInputs.Add(NamedOnnxValue.CreateFromTensor("speedup",
                    new DenseTensor<long>(new long[] { speedup }, new int[] { 1 },false)));
            }
            //Speaker
            if(dsConfig.speakers != null) {
                var speakerEmbedManager = getSpeakerEmbedManager();
                var spkEmbedTensor = speakerEmbedManager.PhraseSpeakerEmbedByFrame(phrase, ph_dur, frameMs, totalFrames, headFrames, tailFrames);
                varianceInputs.Add(NamedOnnxValue.CreateFromTensor("spk_embed", spkEmbedTensor));
            }
            Onnx.VerifyInputNames(varianceModel, varianceInputs);
            var varianceCache = Preferences.Default.DiffSingerTensorCache
                ? new DiffSingerCache(varianceHash, varianceInputs)
                : null;
            var varianceOutputs = varianceCache?.Load();
            if (varianceOutputs is null) {
                varianceOutputs = varianceModel.Run(varianceInputs).Cast<NamedOnnxValue>().ToList();
                varianceCache?.Save(varianceOutputs);
            }
            Tensor<float>? energy_pred = dsConfig.predict_energy
                ? varianceOutputs
                    .Where(o => o.Name == "energy_pred")
                    .First()
                    .AsTensor<float>()
                : null;
            Tensor<float>? breathiness_pred = dsConfig.predict_breathiness
                ? varianceOutputs
                    .Where(o => o.Name == "breathiness_pred")
                    .First()
                    .AsTensor<float>()
                : null;
            Tensor<float>? voicing_pred = dsConfig.predict_voicing
                ? varianceOutputs
                    .Where(o => o.Name == "voicing_pred")
                    .First()
                    .AsTensor<float>()
                : null;
            Tensor<float>? tension_pred = dsConfig.predict_tension
                ? varianceOutputs
                    .Where(o => o.Name == "tension_pred")
                    .First()
                    .AsTensor<float>()
                : null;
            return new VarianceResult{
                energy = energy_pred?.ToArray(),
                breathiness = breathiness_pred?.ToArray(),
                voicing = voicing_pred?.ToArray(),
                tension = tension_pred?.ToArray(),
            };
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    linguisticModel?.Dispose();
                    varianceModel?.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

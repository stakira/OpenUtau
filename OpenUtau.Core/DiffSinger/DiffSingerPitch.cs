using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using OpenUtau.Api;
using OpenUtau.Core.Render;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Core.DiffSinger
{
    public class DsPitch
    {
        string rootPath;
        DsConfig dsConfig;
        List<string> phonemes;
        InferenceSession linguisticModel;
        InferenceSession pitchModel;
        IG2p g2p;
        float frameMs;
        const float headMs = DiffSingerUtils.headMs;

        public DsPitch(string rootPath)
        {
            this.rootPath = rootPath;
            dsConfig = Core.Yaml.DefaultDeserializer.Deserialize<DsConfig>(
                File.ReadAllText(Path.Combine(rootPath, "dsconfig.yaml"),
                    System.Text.Encoding.UTF8));
            //Load phonemes list
            string phonemesPath = Path.Combine(rootPath, dsConfig.phonemes);
            phonemes = File.ReadLines(phonemesPath, Encoding.UTF8).ToList();
            //Load models
            var linguisticModelPath = Path.Join(rootPath, dsConfig.linguistic);
            linguisticModel = Onnx.getInferenceSession(linguisticModelPath);
            var pitchModelPath = Path.Join(rootPath, dsConfig.pitch);
            pitchModel = Onnx.getInferenceSession(pitchModelPath);
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

        public RenderPitchResult Process(RenderPhrase phrase){
            var startMs = Math.Min(phrase.notes[0].positionMs, phrase.phones[0].positionMs) - headMs;
            var endMs = phrase.notes[^1].endMs;
            int n_frames = (int)(endMs/frameMs)-(int)(startMs/frameMs);
            //Linguistic Encoder
            var linguisticInputs = new List<NamedOnnxValue>();
            var tokens = phrase.phones
                .Select(p => (Int64)phonemes.IndexOf(p.phoneme))
                .Prepend((Int64)phonemes.IndexOf("SP"))
                .ToArray();
            var ph_dur = phrase.phones
                .Select(p=>(Int64)(p.endMs/frameMs) - (Int64)(p.positionMs/frameMs))
                .Prepend((Int64)(phrase.phones[0].positionMs/frameMs) - (Int64)(startMs/frameMs))
                .ToArray();
            linguisticInputs.Add(NamedOnnxValue.CreateFromTensor("tokens",
                new DenseTensor<Int64>(tokens, new int[] { tokens.Length }, false)
                .Reshape(new int[] { 1, tokens.Length })));
            if(dsConfig.predict_dur){
                //if predict_dur is true, use word encode mode
                var vowelIds = Enumerable.Range(0,phrase.phones.Length)
                    .Where(i=>g2p.IsVowel(phrase.phones[i].phoneme))
                    .Append(phrase.phones.Length)
                    .ToArray();
                var word_div = vowelIds.Zip(vowelIds.Skip(1),(a,b)=>(Int64)(b-a))
                    .Prepend(vowelIds[0] + 1)
                    .ToArray();
                var word_dur = vowelIds.Zip(vowelIds.Skip(1),
                        (a,b)=>(Int64)(phrase.phones[b-1].endMs/frameMs) - (Int64)(phrase.phones[a].positionMs/frameMs))
                    .Prepend((Int64)(phrase.phones[vowelIds[0]].positionMs/frameMs) - (Int64)(startMs/frameMs))
                    .ToArray();
                linguisticInputs.Add(NamedOnnxValue.CreateFromTensor("word_div",
                    new DenseTensor<Int64>(word_div, new int[] { word_div.Length }, false)
                    .Reshape(new int[] { 1, word_div.Length })));
                linguisticInputs.Add(NamedOnnxValue.CreateFromTensor("word_dur",
                    new DenseTensor<Int64>(word_dur, new int[] { word_dur.Length }, false)
                    .Reshape(new int[] { 1, word_dur.Length })));
            }else{
                //if predict_dur is true, use phoneme encode mode
                linguisticInputs.Add(NamedOnnxValue.CreateFromTensor("ph_dur",
                    new DenseTensor<Int64>(ph_dur, new int[] { ph_dur.Length }, false)
                    .Reshape(new int[] { 1, ph_dur.Length })));
            }
            
            var linguisticOutputs = linguisticModel.Run(linguisticInputs);
            Tensor<float> encoder_out = linguisticOutputs
                .Where(o => o.Name == "encoder_out")
                .First()
                .AsTensor<float>();
            Tensor<bool> x_masks = linguisticOutputs
                .Where(o => o.Name == "x_masks")
                .First()
                .AsTensor<bool>();
            //Pitch Predictor            
            var note_midi = phrase.notes
                .Select(n=>(float)n.tone)
                .Prepend((float)phrase.notes[0].tone)
                .ToArray();
            //use the delta of the positions of the next note and the current note 
            //to prevent incorrect timing when there is a small space between two notes
            var note_dur = phrase.notes.Zip(phrase.notes.Skip(1),
                    (curr,next)=> (Int64)(next.positionMs/frameMs) - (Int64)(curr.positionMs/frameMs))
                .Prepend((Int64)(phrase.notes[0].positionMs/frameMs) - (Int64)(startMs/frameMs))
                .Append((Int64)(phrase.notes[^1].endMs/frameMs)-(Int64)(phrase.notes[^1].positionMs/frameMs))
                .ToArray();
            
            var pitch = Enumerable.Repeat(60f, n_frames).ToArray();
            var retake = Enumerable.Repeat(true, n_frames).ToArray();
            var speedup = Preferences.Default.DiffsingerSpeedup;
            var pitchInputs = new List<NamedOnnxValue>();
            pitchInputs.Add(NamedOnnxValue.CreateFromTensor("encoder_out", encoder_out));
            pitchInputs.Add(NamedOnnxValue.CreateFromTensor("note_midi",
                new DenseTensor<float>(note_midi, new int[] { note_midi.Length }, false)
                .Reshape(new int[] { 1, note_midi.Length })));
            pitchInputs.Add(NamedOnnxValue.CreateFromTensor("note_dur",
                new DenseTensor<Int64>(note_dur, new int[] { note_dur.Length }, false)
                .Reshape(new int[] { 1, note_dur.Length })));
            pitchInputs.Add(NamedOnnxValue.CreateFromTensor("ph_dur",
                new DenseTensor<Int64>(ph_dur, new int[] { ph_dur.Length }, false)
                .Reshape(new int[] { 1, ph_dur.Length })));
            pitchInputs.Add(NamedOnnxValue.CreateFromTensor("pitch",
                new DenseTensor<float>(pitch, new int[] { pitch.Length }, false)
                .Reshape(new int[] { 1, pitch.Length })));
            pitchInputs.Add(NamedOnnxValue.CreateFromTensor("retake",
                new DenseTensor<bool>(retake, new int[] { retake.Length }, false)
                .Reshape(new int[] { 1, retake.Length })));
            pitchInputs.Add(NamedOnnxValue.CreateFromTensor("speedup",
                new DenseTensor<long>(new long[] { speedup }, new int[] { 1 },false)));
            var pitchOutputs = pitchModel.Run(pitchInputs);
            var pitch_out = pitchOutputs.First().AsTensor<float>().ToArray();
            var pitchEnd = phrase.timeAxis.MsPosToTickPos(startMs + (n_frames - 1) * frameMs) - phrase.position;
            if(pitchEnd<=phrase.duration){
                return new RenderPitchResult{
                    ticks = Enumerable.Range(0,n_frames)
                    .Select(i=>(float)phrase.timeAxis.MsPosToTickPos(startMs + i*frameMs) - phrase.position)
                    .Append((float)phrase.duration + 1)
                    .ToArray(),
                    tones = pitch_out.Append(pitch_out[^1]).ToArray()
                };
            }else{
                return new RenderPitchResult{
                    ticks = Enumerable.Range(0,n_frames)
                    .Select(i=>(float)phrase.timeAxis.MsPosToTickPos(startMs + i*frameMs) - phrase.position)
                    .ToArray(),
                    tones = pitch_out
                };
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using K4os.Hash.xxHash;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using OpenUtau.Api;
using OpenUtau.Core.Render;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Core.DiffSinger
{
    public class DsPitch : IDisposable
    {
        string rootPath;
        DsConfig dsConfig;
        Dictionary<string, int> languageIds = new Dictionary<string, int>();
        Dictionary<string, int> phonemeTokens;
        ulong linguisticHash;
        InferenceSession linguisticModel;
        InferenceSession pitchModel;
        IG2p g2p;
        float frameMs;
        DiffSingerSpeakerEmbedManager speakerEmbedManager;
        const float headMs = DiffSingerUtils.headMs;
        const float tailMs = DiffSingerUtils.tailMs;
        const string PEXP = DiffSingerUtils.PEXP;

        public DsPitch(string rootPath)
        {
            this.rootPath = rootPath;
            dsConfig = Core.Yaml.DefaultDeserializer.Deserialize<DsConfig>(
                File.ReadAllText(Path.Combine(rootPath, "dsconfig.yaml"),
                    System.Text.Encoding.UTF8));
            if(dsConfig.pitch == null){
                throw new Exception("This voicebank doesn't contain a pitch model");
            }
            //Load language id if needed
            if(dsConfig.use_lang_id){
                if(dsConfig.languages == null){
                    Log.Error("\"languages\" field is not specified in dsconfig.yaml");
                    return;
                }
                var langIdPath = Path.Join(rootPath, dsConfig.languages);
                try {
                    languageIds = DiffSingerUtils.LoadLanguageIds(langIdPath);
                } catch (Exception e) {
                    Log.Error(e, $"failed to load language id from {langIdPath}");
                    return;
                }
            }
            //Load phonemes list
            string phonemesPath = Path.Combine(rootPath, dsConfig.phonemes);
            phonemeTokens = DiffSingerUtils.LoadPhonemes(phonemesPath);
            //Load models
            var linguisticModelPath = Path.Join(rootPath, dsConfig.linguistic);
            var linguisticModelBytes = File.ReadAllBytes(linguisticModelPath);
            linguisticHash = XXH64.DigestOf(linguisticModelBytes);
            linguisticModel = Onnx.getInferenceSession(linguisticModelBytes);
            var pitchModelPath = Path.Join(rootPath, dsConfig.pitch);
            pitchModel = Onnx.getInferenceSession(pitchModelPath);
            frameMs = 1000f * dsConfig.hop_size / dsConfig.sample_rate;
            //Load g2p
            g2p = LoadG2p(rootPath);
        }

        protected IG2p LoadG2p(string rootPath) {
            // Load dictionary from singer folder.
            string file = Path.Combine(rootPath, "dsdict.yaml");
            if(!File.Exists(file)){
                throw new Exception($"File not found: {file}");
            }
            var g2pBuilder = G2pDictionary.NewBuilder().Load(File.ReadAllText(file));
            //SP and AP should always be vowel
            g2pBuilder.AddSymbol("SP", true);
            g2pBuilder.AddSymbol("AP", true);
            return g2pBuilder.Build();
        }

        public DiffSingerSpeakerEmbedManager getSpeakerEmbedManager(){
            if(speakerEmbedManager is null) {
                speakerEmbedManager = new DiffSingerSpeakerEmbedManager(dsConfig, rootPath);
            }
            return speakerEmbedManager;
        }

        void SetRange<T>(T[] list, T value, int startIndex, int endIndex){
            for(int i=startIndex;i<endIndex;i++){
                list[i] = value;
            }
        }

        int PhonemeTokenize(string phoneme){
            bool success = phonemeTokens.TryGetValue(phoneme, out int token);
            if(!success){
                throw new Exception($"Phoneme \"{phoneme}\" isn't supported by pitch model. Please check {Path.Combine(rootPath, dsConfig.phonemes)}");
            }
            return token;
        }
        
        public RenderPitchResult Process(RenderPhrase phrase){
            var startMs = Math.Min(phrase.notes[0].positionMs, phrase.phones[0].positionMs) - headMs;
            var endMs = phrase.notes[^1].endMs + tailMs;
            int headFrames = (int)Math.Round(headMs / frameMs);
            int tailFrames = (int)Math.Round(tailMs / frameMs);
            //Check if all phonemes are defined in dsdict.yaml (for their types)
            foreach (var phone in phrase.phones) {
                if (!g2p.IsValidSymbol(phone.phoneme)) {
                    throw new InvalidDataException(
                        $"Type definition of symbol \"{phone.phoneme}\" not found. Consider adding it to dsdict.yaml of the pitch predictor.");
                }
            }
            //Linguistic Encoder
            var linguisticInputs = new List<NamedOnnxValue>();
            var tokens = phrase.phones
                .Select(p => p.phoneme)
                .Prepend("SP")
                .Append("SP")
                .Select(x => (Int64)PhonemeTokenize(x))
                .ToArray();
            var ph_dur = phrase.phones
                .Select(p=>(int)Math.Round(p.endMs/frameMs) - (int)Math.Round(p.positionMs/frameMs))
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
                if(vowelIds.Length == 0){
                    vowelIds = new int[]{phrase.phones.Length-1};
                }
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
            } else {
                //if predict_dur is false, use phoneme encode mode
                linguisticInputs.Add(NamedOnnxValue.CreateFromTensor("ph_dur",
                    new DenseTensor<Int64>(ph_dur.Select(x=>(Int64)x).ToArray(), new int[] { ph_dur.Length }, false)
                    .Reshape(new int[] { 1, ph_dur.Length })));
            }
            //Language id
            if(dsConfig.use_lang_id){
                var langIdByPhone = phrase.phones
                    .Select(p => (long)languageIds.GetValueOrDefault(
                        DiffSingerUtils.PhonemeLanguage(p.phoneme),0
                        ))
                    .Prepend(0)
                    .Append(0)
                    .ToArray();
                var langIdTensor = new DenseTensor<Int64>(langIdByPhone, new int[] { langIdByPhone.Length }, false)
                    .Reshape(new int[] { 1, langIdByPhone.Length });
                linguisticInputs.Add(NamedOnnxValue.CreateFromTensor("languages", langIdTensor));
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
            Tensor<bool> x_masks = linguisticOutputs
                .Where(o => o.Name == "x_masks")
                .First()
                .AsTensor<bool>();
            
            //Pitch Predictor   
            var note_rest = new List<bool>{true};
            bool prevNoteRest = true;
            int phIndex = 0;
            foreach(var note in phrase.notes) {
                //Slur notes follow the previous note's rest status
                if(note.lyric.StartsWith("+")) {
                    note_rest.Add(prevNoteRest);
                    continue;
                }
                //find all the phonemes in the note's time range
                while(phIndex<phrase.phones.Length && phrase.phones[phIndex].endMs<=note.endMs) {
                    phIndex++;
                }
                var phs = phrase.phones
                    .SkipWhile(ph => ph.end <= note.position + 1)
                    .TakeWhile(ph => ph.position < note.end - 1)
                    .ToArray();
                //If all the phonemes in a note's time range are AP, SP or consonant,
                //it is a rest note
                bool isRest = phs.Length == 0
                    || phs.All(ph => ph.phoneme == "AP" || ph.phoneme == "SP" || !g2p.IsVowel(ph.phoneme));
                note_rest.Add(isRest);
                prevNoteRest = isRest;
            }

            var note_midi = phrase.notes
                .Select(n=>(float)n.tone)
                .Prepend((float)phrase.notes[0].tone)
                .ToArray();
            //get the index of groups of consecutive rest notes
            var restGroups = new List<Tuple<int,int>>();
            for (var i = 0; i < note_rest.Count; ++i) {
                if (!note_rest[i]) continue;
                var j = i + 1;
                for (; j < note_rest.Count && note_rest[j]; ++j) { }
                restGroups.Add(new Tuple<int, int>(i, j));
                i = j;
            }
            //Set tone for each rest group
            foreach(var restGroup in restGroups){
                if(restGroup.Item1 == 0 && restGroup.Item2 == note_rest.Count){
                    //If All the notes are rest notes, don't set tone
                    break;
                }
                if(restGroup.Item1 == 0){
                    //If the first note is a rest note, set the tone to the tone of the first non-rest note
                    SetRange<float>(note_midi, note_midi[restGroup.Item2], 0, restGroup.Item2);
                } else if(restGroup.Item2 == note_rest.Count){
                    //If the last note is a rest note, set the tone to the tone of the last non-rest note
                    SetRange<float>(note_midi, note_midi[restGroup.Item1-1], restGroup.Item1, note_rest.Count);
                } else {
                    //If the first and last notes are non-rest notes, set the tone to the nearest non-rest note
                    SetRange<float>(note_midi, 
                        note_midi[restGroup.Item1-1], 
                        restGroup.Item1, 
                        (restGroup.Item1 + restGroup.Item2 + 1)/2
                    );
                    SetRange<float>(note_midi, 
                        note_midi[restGroup.Item2], 
                        (restGroup.Item1 + restGroup.Item2 + 1)/2, 
                        restGroup.Item2
                    );
                }
            }

            //use the delta of the positions of the next note and the current note
            //to prevent incorrect timing when there is a small space between two notes
            var note_dur = phrase.notes.Zip(phrase.notes.Skip(1),
                    (curr,next)=> (int)Math.Round(next.positionMs/frameMs) - (int)Math.Round(curr.positionMs/frameMs))
                .Prepend((int)Math.Round(phrase.notes[0].positionMs/frameMs) - (int)Math.Round(startMs/frameMs))
                .Append(0)
                .ToList();
            note_dur[^1]=totalFrames-note_dur.Sum();
            var pitch = Enumerable.Repeat(60f, totalFrames).ToArray();
            var retake = Enumerable.Repeat(true, totalFrames).ToArray();
            var pitchInputs = new List<NamedOnnxValue>();
            pitchInputs.Add(NamedOnnxValue.CreateFromTensor("encoder_out", encoder_out));
            pitchInputs.Add(NamedOnnxValue.CreateFromTensor("note_midi",
                new DenseTensor<float>(note_midi, new int[] { note_midi.Length }, false)
                .Reshape(new int[] { 1, note_midi.Length })));
            pitchInputs.Add(NamedOnnxValue.CreateFromTensor("note_dur",
                new DenseTensor<Int64>(note_dur.Select(x=>(Int64)x).ToArray(), new int[] { note_dur.Count }, false)
                .Reshape(new int[] { 1, note_dur.Count })));
            pitchInputs.Add(NamedOnnxValue.CreateFromTensor("ph_dur",
                new DenseTensor<Int64>(ph_dur.Select(x=>(Int64)x).ToArray(), new int[] { ph_dur.Length }, false)
                .Reshape(new int[] { 1, ph_dur.Length })));
            pitchInputs.Add(NamedOnnxValue.CreateFromTensor("pitch",
                new DenseTensor<float>(pitch, new int[] { pitch.Length }, false)
                .Reshape(new int[] { 1, pitch.Length })));
            pitchInputs.Add(NamedOnnxValue.CreateFromTensor("retake",
                new DenseTensor<bool>(retake, new int[] { retake.Length }, false)
                .Reshape(new int[] { 1, retake.Length })));
            var steps = Preferences.Default.DiffSingerSteps;
            if (dsConfig.useContinuousAcceleration) {
                pitchInputs.Add(NamedOnnxValue.CreateFromTensor("steps",
                    new DenseTensor<long>(new long[] { steps }, new int[] { 1 }, false)));
            } else {
                // find a largest integer speedup that are less than 1000 / steps and is a factor of 1000
                long speedup = Math.Max(1, 1000 / steps);
                while (1000 % speedup != 0 && speedup > 1) {
                    speedup--;
                }
                pitchInputs.Add(NamedOnnxValue.CreateFromTensor("speedup",
                    new DenseTensor<long>(new long[] { speedup }, new int[] { 1 },false)));
            }

            //expressiveness
            if (dsConfig.use_expr) {
                var exprCurve = phrase.curves.FirstOrDefault(curve => curve.Item1 == PEXP);
                float[] expr;
                if (exprCurve != null) {
                    expr = DiffSingerUtils.SampleCurve(phrase, exprCurve.Item2, 1, frameMs, totalFrames, headFrames, tailFrames,
                            x => Math.Min(1, Math.Max(0, x / 100)))
                        .Select(f => (float)f).ToArray();
                } else {
                    expr = Enumerable.Repeat(1f, totalFrames).ToArray();
                }
                pitchInputs.Add(NamedOnnxValue.CreateFromTensor("expr",
                    new DenseTensor<float>(expr, new int[] { expr.Length }, false)
                        .Reshape(new int[] { 1, expr.Length })));
            }

            //Speaker
            if(dsConfig.speakers != null) {
                var speakerEmbedManager = getSpeakerEmbedManager();
                var spkEmbedTensor = speakerEmbedManager.PhraseSpeakerEmbedByFrame(phrase, ph_dur, frameMs, totalFrames, headFrames, tailFrames);
                pitchInputs.Add(NamedOnnxValue.CreateFromTensor("spk_embed", spkEmbedTensor));
            }

            //Melody encoder
            if(dsConfig.use_note_rest) {
                pitchInputs.Add(NamedOnnxValue.CreateFromTensor("note_rest",
                new DenseTensor<bool>(note_rest.ToArray(), new int[] { note_rest.Count }, false)
                .Reshape(new int[] { 1, note_rest.Count })));
            }

            Onnx.VerifyInputNames(pitchModel, pitchInputs);
            var pitchOutputs = pitchModel.Run(pitchInputs);
            var pitch_out = pitchOutputs.First().AsTensor<float>().ToArray();
            var pitchEnd = phrase.timeAxis.MsPosToTickPos(startMs + (totalFrames - 1) * frameMs) - phrase.position;
            if(pitchEnd<=phrase.duration){
                return new RenderPitchResult{
                    ticks = Enumerable.Range(0,totalFrames)
                    .Select(i=>(float)phrase.timeAxis.MsPosToTickPos(startMs + i*frameMs) - phrase.position)
                    .Append((float)phrase.duration + 1)
                    .ToArray(),
                    tones = pitch_out.Append(pitch_out[^1]).ToArray()
                };
            }else{
                return new RenderPitchResult{
                    ticks = Enumerable.Range(0,totalFrames)
                    .Select(i=>(float)phrase.timeAxis.MsPosToTickPos(startMs + i*frameMs) - phrase.position)
                    .ToArray(),
                    tones = pitch_out
                };
            }
        }

        private bool disposedValue;
        
        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    linguisticModel?.Dispose();
                    pitchModel?.Dispose();
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

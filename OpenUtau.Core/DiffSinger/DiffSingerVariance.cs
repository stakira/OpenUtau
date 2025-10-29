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
        Dictionary<string, int> languageIds = new Dictionary<string, int>();
        Dictionary<string, int> phonemeTokens;
        ulong linguisticHash;
        ulong varianceHash;
        InferenceSession linguisticModel;
        InferenceSession varianceModel;
        IG2p g2p;
        float frameMs;
        const float headMs = DiffSingerUtils.headMs;
        const float tailMs = DiffSingerUtils.tailMs;
        DiffSingerSpeakerEmbedManager speakerEmbedManager;

        public float FrameMs => frameMs;

        public DsVariance(string rootPath)
        {
            this.rootPath = rootPath;
            dsConfig = Yaml.DefaultDeserializer.Deserialize<DsConfig>(
                File.ReadAllText(Path.Combine(rootPath, "dsconfig.yaml"),
                    Encoding.UTF8));
            if(dsConfig.variance == null){
                throw new Exception("This voicebank doesn't contain a variance model");
            }
            //Load language id if needed
            if(dsConfig.use_lang_id){
                if(dsConfig.languages == null){
                    throw new Exception("\"languages\" field is not specified in dsconfig.yaml");
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
            if (dsConfig.phonemes == null) {
                throw new Exception("Configuration key \"phonemes\" is null.");
            }
            string phonemesPath = Path.Combine(rootPath, dsConfig.phonemes);
            phonemeTokens = DiffSingerUtils.LoadPhonemes(phonemesPath);
            //Load models
            if (dsConfig.linguistic == null) {
                throw new Exception("Configuration key \"linguistic\" is null.");
            }
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

        int PhonemeTokenize(string phoneme){
            bool success = phonemeTokens.TryGetValue(phoneme, out int token);
            if(!success){
                throw new Exception($"Phoneme \"{phoneme}\" isn't supported by variance model. Please check {Path.Combine(rootPath, dsConfig.phonemes)}");
            }
            return token;
        }

        public VarianceResult Process(RenderPhrase phrase){
            int headFrames = (int)Math.Round(headMs / frameMs);
            int tailFrames = (int)Math.Round(tailMs / frameMs);
            Tensor<float>? energy_pred = null;
            Tensor<float>? breathiness_pred = null;
            Tensor<float>? voicing_pred = null;
            Tensor<float>? tension_pred = null;
            if (dsConfig.predict_dur) {
                //Check if all phonemes are defined in dsdict.yaml (for their types)
                foreach (var phone in phrase.phones) {
                    if (!g2p.IsValidSymbol(phone.phoneme)) {
                        throw new InvalidDataException(
                            $"Type definition of symbol \"{phone.phoneme}\" not found. Consider adding it to dsdict.yaml of the variance predictor.");
                    }
                }
            }
            //Linguistic Encoder
            var linguisticInputs = new List<NamedOnnxValue>();
            var tokens = phrase.phones.Select(p => p.phoneme)
                .Prepend("SP")
                .Append("SP")
                .Select(x => (Int64)PhonemeTokenize(x))
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
            }else{
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
                phrase.AddCacheFile(linguisticCache?.Filename);
            }
            Tensor<float> encoder_out = linguisticOutputs
                .Where(o => o.Name == "encoder_out")
                .First()
                .AsTensor<float>();

            //Variance Predictor
            var pitch = DiffSingerUtils.SampleCurve(phrase, phrase.pitches, 0, frameMs, totalFrames, headFrames, tailFrames, 
                x => x * 0.01).Select(f => (float)f).ToArray();
            var toneShift = DiffSingerUtils.SampleCurve(phrase, phrase.toneShift, 0, frameMs, totalFrames, headFrames, tailFrames,
                x => x * 0.01).Select(f => (float)f).ToArray();
            pitch = pitch.Zip(toneShift, (x, d) => x + d).ToArray();

            // 1. Prepare IDENTITY INPUTS. These define the stable cache key.
            var identityInputs = new List<NamedOnnxValue>();
            identityInputs.Add(NamedOnnxValue.CreateFromTensor("encoder_out", encoder_out));
            identityInputs.Add(NamedOnnxValue.CreateFromTensor("ph_dur",
                new DenseTensor<Int64>(ph_dur.Select(x => (Int64)x).ToArray(), new int[] { ph_dur.Length }, false)
                .Reshape(new int[] { 1, ph_dur.Length })));
            // inpaint考虑pitch修改常驻生成掩码，所以cache不再考虑pitch

            if (dsConfig.speakers != null) {
                var speakerEmbedManager = getSpeakerEmbedManager();
                var spkEmbedTensor = speakerEmbedManager.PhraseSpeakerEmbedByFrame(phrase, ph_dur, frameMs, totalFrames, headFrames, tailFrames);
                identityInputs.Add(NamedOnnxValue.CreateFromTensor("spk_embed", spkEmbedTensor));
            }

            var steps = Preferences.Default.DiffSingerStepsVariance;
            if (dsConfig.useContinuousAcceleration) {
                identityInputs.Add(NamedOnnxValue.CreateFromTensor("steps",
                    new DenseTensor<long>(new long[] { steps }, new int[] { 1 }, false)));
            } else {
                long speedup = Math.Max(1, 1000 / steps);
                while (1000 % speedup != 0 && speedup > 1) {
                    speedup--;
                }
                identityInputs.Add(NamedOnnxValue.CreateFromTensor("speedup",
                    new DenseTensor<long>(new long[] { speedup }, new int[] { 1 }, false)));
            }

            // 将varianceInputs初始化移动到了这里，因为要提前处理inpaint掩码
            // This list will hold all inputs for the model run.
            var varianceInputs = new List<NamedOnnxValue>(identityInputs);
            // inpaint考虑pitch修改常驻生成掩码，所以cache不再考虑pitch
            varianceInputs.Add(NamedOnnxValue.CreateFromTensor("pitch",
                new DenseTensor<float>(pitch, new int[] { pitch.Length }, false)
                .Reshape(new int[] { 1, totalFrames })));

            // 2. Create the cache object based on identity.
            var varianceCache = Preferences.Default.DiffSingerTensorCache
                ? new DiffSingerCache(varianceHash, identityInputs)
                : null;
            
            var previousBaseOutput = varianceCache?.Load();
            bool[] perFrameMask;

            // 如果没有缓存，则必须完全重新渲染
            if (previousBaseOutput == null) {
                perFrameMask = Enumerable.Repeat(true, totalFrames).ToArray();
            } else {
                // 如果有缓存，则比较音高以确定是否需要 inpaint
                var oldPitchValue = previousBaseOutput.FirstOrDefault(o => o.Name == "pitch");
                // 检查缓存中是否有音高、长度是否匹配
                if (oldPitchValue != null && oldPitchValue.AsTensor<float>().Length == pitch.Length) {
                    var oldPitch = oldPitchValue.AsTensor<float>().ToArray();
                    perFrameMask = new bool[pitch.Length];
                    bool pitchChanged = false;
                    for (int i = 0; i < pitch.Length; i++) {
                        if (Math.Abs(pitch[i] - oldPitch[i]) > 1e-6) {
                            perFrameMask[i] = true;
                            pitchChanged = true;
                        }
                    }

                    // 如果音高没有变化，直接返回缓存结果 (Fast Path)
                    if (!pitchChanged) {
                        energy_pred = dsConfig.predict_energy
                            ? previousBaseOutput.First(o => o.Name == "energy_pred").AsTensor<float>() : null;
                        breathiness_pred = dsConfig.predict_breathiness
                            ? previousBaseOutput.First(o => o.Name == "breathiness_pred").AsTensor<float>() : null;
                        voicing_pred = dsConfig.predict_voicing
                            ? previousBaseOutput.First(o => o.Name == "voicing_pred").AsTensor<float>() : null;
                        tension_pred = dsConfig.predict_tension
                            ? previousBaseOutput.First(o => o.Name == "tension_pred").AsTensor<float>() : null;

                        return new VarianceResult {
                            energy = energy_pred?.ToArray(),
                            breathiness = breathiness_pred?.ToArray(),
                            voicing = voicing_pred?.ToArray(),
                            tension = tension_pred?.ToArray(),
                        };
                    }
                } else {
                    // 缓存无效 (没有音高或长度不匹配)，完全重新渲染
                    perFrameMask = Enumerable.Repeat(true, totalFrames).ToArray();
                }
            }
            
            // This list will hold all inputs for the model run.因为现在需要比较pitch输入所以初始化被提前到了l241
            // var varianceInputs = new List<NamedOnnxValue>(identityInputs);
            var baseCurves = new Dictionary<string, float[]>();

            // 4. Prepare STATEFUL INPUTS.
            if (dsConfig.predict_energy) {
                float[] baseCurve;
                float[] modelInput;
                if (previousBaseOutput != null) {
                    baseCurve = previousBaseOutput.First(o => o.Name == "energy_pred").AsTensor<float>().ToArray();
                    var energyCurve = phrase.curves.FirstOrDefault(c => c.Item1 == DiffSingerUtils.ENE);
                    IEnumerable<float> userEnergy = (energyCurve != null)
                        ? DiffSingerUtils.SampleCurve(phrase, energyCurve.Item2, 0, frameMs, totalFrames, headFrames, tailFrames, x => x).Select(x=>(float)x)
                        : Enumerable.Repeat(0f, totalFrames);
                    modelInput = baseCurve.Zip(userEnergy, (x, y) => x + y * 12 / 100).ToArray();
                } else {
                    baseCurve = Enumerable.Repeat(0f, totalFrames).ToArray();
                    modelInput = baseCurve;
                }
                baseCurves["energy"] = baseCurve;
                varianceInputs.Add(NamedOnnxValue.CreateFromTensor("energy", 
                    new DenseTensor<float>(modelInput, new int[] { 1, totalFrames })));
            }

            if (dsConfig.predict_breathiness) {
                float[] baseCurve;
                float[] modelInput;
                if (previousBaseOutput != null) {
                    baseCurve = previousBaseOutput.First(o => o.Name == "breathiness_pred").AsTensor<float>().ToArray();
                    var userBreathiness = DiffSingerUtils.SampleCurve(phrase, phrase.breathiness, 0, frameMs, totalFrames, headFrames, tailFrames, x => x).Select(x => (float)x);
                    modelInput = baseCurve.Zip(userBreathiness, (x, y) => x + y * 12 / 100).ToArray();
                } else {
                    baseCurve = Enumerable.Repeat(0f, totalFrames).ToArray();
                    modelInput = baseCurve;
                }
                baseCurves["breathiness"] = baseCurve;
                varianceInputs.Add(NamedOnnxValue.CreateFromTensor("breathiness", 
                    new DenseTensor<float>(modelInput, new int[] { 1, totalFrames })));
            }

            if (dsConfig.predict_voicing) {
                float[] baseCurve;
                float[] modelInput;
                if (previousBaseOutput != null) {
                    baseCurve = previousBaseOutput.First(o => o.Name == "voicing_pred").AsTensor<float>().ToArray();
                    var userVoicing = DiffSingerUtils.SampleCurve(phrase, phrase.voicing, 0, frameMs, totalFrames, headFrames, tailFrames, x => x).Select(x => (float)x);
                    modelInput = baseCurve.Zip(userVoicing, (x, y) => x + (y - 100) * 12 / 100).ToArray();
                } else {
                    baseCurve = Enumerable.Repeat(0f, totalFrames).ToArray();
                    modelInput = baseCurve;
                }
                baseCurves["voicing"] = baseCurve;
                varianceInputs.Add(NamedOnnxValue.CreateFromTensor("voicing",
                    new DenseTensor<float>(modelInput, new int[] { 1, totalFrames })));
            }

            if (dsConfig.predict_tension) {
                float[] baseCurve;
                float[] modelInput;
                if (previousBaseOutput != null) {
                    baseCurve = previousBaseOutput.First(o => o.Name == "tension_pred").AsTensor<float>().ToArray();
                    var userTension = DiffSingerUtils.SampleCurve(phrase, phrase.tension, 0, frameMs, totalFrames, headFrames, tailFrames, x => x).Select(x => (float)x);
                    modelInput = baseCurve.Zip(userTension, (x, y) => x + y / 20).ToArray();
                } else {
                    baseCurve = Enumerable.Repeat(0f, totalFrames).ToArray();
                    modelInput = baseCurve;
                }
                baseCurves["tension"] = baseCurve;
                varianceInputs.Add(NamedOnnxValue.CreateFromTensor("tension",
                    new DenseTensor<float>(modelInput, new int[] { 1, totalFrames })));
            }

            // 5. Prepare retake mask.
            var numVariances = new[] {
                dsConfig.predict_energy,
                dsConfig.predict_breathiness,
                dsConfig.predict_voicing,
                dsConfig.predict_tension,
            }.Sum(Convert.ToInt32);

            var expandedRetakeMask = perFrameMask
                .SelectMany(shouldRetake => Enumerable.Repeat(shouldRetake, numVariances))
                .ToArray();
            varianceInputs.Add(NamedOnnxValue.CreateFromTensor("retake",
                new DenseTensor<bool>(expandedRetakeMask, new int[] { 1, totalFrames, numVariances })));

            // 6. Run the model.
            Onnx.VerifyInputNames(varianceModel, varianceInputs);
            var modelOutputs = varianceModel.Run(varianceInputs).Cast<NamedOnnxValue>().ToList();

            // 7. Post-process to get the new base curves.
            var newBaseOutputs = new List<NamedOnnxValue>();

            newBaseOutputs.Add(varianceInputs.First(v => v.Name == "pitch")); // 缓存输入作为下一个渲染循环的比较对象

            if (dsConfig.predict_energy) {
                var mixedCurve = modelOutputs.First(o => o.Name == "energy_pred").AsTensor<float>().ToArray();
                var oldBaseCurve = baseCurves["energy"];
                var newBaseCurve = new float[totalFrames];
                for (int i = 0; i < totalFrames; ++i) {
                    newBaseCurve[i] = perFrameMask[i] ? mixedCurve[i] : oldBaseCurve[i];
                }
                newBaseOutputs.Add(NamedOnnxValue.CreateFromTensor("energy_pred",
                    new DenseTensor<float>(newBaseCurve, new int[] { 1, totalFrames })));
            }

            if (dsConfig.predict_breathiness) {
                var mixedCurve = modelOutputs.First(o => o.Name == "breathiness_pred").AsTensor<float>().ToArray();
                var oldBaseCurve = baseCurves["breathiness"];
                var newBaseCurve = new float[totalFrames];
                for (int i = 0; i < totalFrames; ++i) {
                    newBaseCurve[i] = perFrameMask[i] ? mixedCurve[i] : oldBaseCurve[i];
                }
                newBaseOutputs.Add(NamedOnnxValue.CreateFromTensor("breathiness_pred",
                    new DenseTensor<float>(newBaseCurve, new int[] { 1, totalFrames })));
            }
            
            if (dsConfig.predict_voicing) {
                var mixedCurve = modelOutputs.First(o => o.Name == "voicing_pred").AsTensor<float>().ToArray();
                var oldBaseCurve = baseCurves["voicing"];
                var newBaseCurve = new float[totalFrames];
                for (int i = 0; i < totalFrames; ++i) {
                    newBaseCurve[i] = perFrameMask[i] ? mixedCurve[i] : oldBaseCurve[i];
                }
                newBaseOutputs.Add(NamedOnnxValue.CreateFromTensor("voicing_pred",
                    new DenseTensor<float>(newBaseCurve, new int[] { 1, totalFrames })));
            }

            if (dsConfig.predict_tension) {
                var mixedCurve = modelOutputs.First(o => o.Name == "tension_pred").AsTensor<float>().ToArray();
                var oldBaseCurve = baseCurves["tension"];
                var newBaseCurve = new float[totalFrames];
                for (int i = 0; i < totalFrames; ++i) {
                    newBaseCurve[i] = perFrameMask[i] ? mixedCurve[i] : oldBaseCurve[i];
                }
                newBaseOutputs.Add(NamedOnnxValue.CreateFromTensor("tension_pred",
                    new DenseTensor<float>(newBaseCurve, new int[] { 1, totalFrames })));
            }

            // 8. Save the new base curves to cache.
            varianceCache?.Save(newBaseOutputs);
            if (varianceCache?.Filename != null) {
                phrase.AddCacheFile(varianceCache.Filename);
            }

            // 9. Parse and return the new base curves.
            energy_pred = dsConfig.predict_energy
                ? newBaseOutputs
                    .Where(o => o.Name == "energy_pred")
                    .First()
                    .AsTensor<float>()
                : null;
            breathiness_pred = dsConfig.predict_breathiness
                ? newBaseOutputs
                    .Where(o => o.Name == "breathiness_pred")
                    .First()
                    .AsTensor<float>()
                : null;
            voicing_pred = dsConfig.predict_voicing
                ? newBaseOutputs
                    .Where(o => o.Name == "voicing_pred")
                    .First()
                    .AsTensor<float>()
                : null;
            tension_pred = dsConfig.predict_tension
                ? newBaseOutputs
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

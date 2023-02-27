using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NAudio.Wave;
using OpenUtau.Core.Format;
using OpenUtau.Core.Render;
using OpenUtau.Core.SignalChain;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Core.DiffSinger {
    public class DiffSingerRenderer : IRenderer {
        const float headMs = DiffSingerUtils.headMs;
        const float tailMs = DiffSingerUtils.tailMs;
        const string VELC = DiffSingerUtils.VELC;

        static readonly HashSet<string> supportedExp = new HashSet<string>(){
            Format.Ustx.DYN,
            Format.Ustx.PITD,
            Format.Ustx.GENC,
            VELC,
        };

        static readonly object lockObj = new object();

        public USingerType SingerType => USingerType.DiffSinger;

        public bool SupportsRenderPitch => false;

        public bool SupportsExpression(UExpressionDescriptor descriptor) {
            return supportedExp.Contains(descriptor.abbr);
        }

        //计算时间轴，包括位置、头辅音长度、预估总时长
        public RenderResult Layout(RenderPhrase phrase) {
            return new RenderResult() {
                leadingMs = headMs,
                positionMs = phrase.positionMs,
                estimatedLengthMs = headMs + phrase.durationMs + tailMs,
            };
        }

        public Task<RenderResult> Render(RenderPhrase phrase, Progress progress, CancellationTokenSource cancellation, bool isPreRender) {
            var task = Task.Run(() => {
                lock (lockObj) {
                    if (cancellation.IsCancellationRequested) {
                        return new RenderResult();
                    }
                    var result = Layout(phrase);
                    int speedup = Core.Util.Preferences.Default.DiffsingerSpeedup;
                    var wavPath = Path.Join(PathManager.Inst.CachePath, $"ds-{phrase.hash:x16}-{speedup}x.wav");
                    string progressInfo = $"{this}{speedup}x \"{string.Join(" ", phrase.phones.Select(p => p.phoneme))}\"";
                    if (File.Exists(wavPath)) {
                        try {
                            using (var waveStream = Wave.OpenFile(wavPath)) {
                                result.samples = Wave.GetSamples(waveStream.ToSampleProvider().ToMono(1, 0));
                            }
                        } catch (Exception e) {
                            Log.Error(e, "Failed to render.");
                        }
                    }
                    if (result.samples == null) {
                        result.samples = InvokeDiffsinger(phrase, speedup);
                        var source = new WaveSource(0, 0, 0, 1);
                        source.SetSamples(result.samples);
                        WaveFileWriter.CreateWaveFile16(wavPath, new ExportAdapter(source).ToMono(1, 0));
                    }
                    if (result.samples != null) {
                        Renderers.ApplyDynamics(phrase, result);
                    }
                    progress.Complete(phrase.phones.Length, progressInfo);
                    return result;
                }
            });
            return task;
        }
        /*result的格式：
        result.samples：渲染好的音频，float[]
        leadingMs、positionMs、estimatedLengthMs：时间轴相关，单位：毫秒，double
         */

        float[] InvokeDiffsinger(RenderPhrase phrase,int speedup) {
            //调用Diffsinger模型
            var singer = phrase.singer as DiffSingerSinger;
            //检测dsconfig.yaml是否正确
            if(String.IsNullOrEmpty(singer.dsConfig.vocoder) ||
                String.IsNullOrEmpty(singer.dsConfig.acoustic) ||
                String.IsNullOrEmpty(singer.dsConfig.phonemes)){
                throw new Exception("Invalid dsconfig.yaml. Please ensure that dsconfig.yaml contains keys \"vocoder\", \"acoustic\" and \"phonemes\".");
            }

            var vocoder = singer.getVocoder();
            var frameMs = vocoder.frameMs();
            var frameSec = frameMs / 1000;
            int headFrames = (int)(headMs / frameMs);
            int tailFrames = (int)(tailMs / frameMs);
            var result = Layout(phrase);
            //acoustic
            //mel = session.run(['mel'], {'tokens': tokens, 'durations': durations, 'f0': f0, 'speedup': speedup})[0]
            //tokens: 音素编号
            //durations: 时长，帧数
            //f0: 音高曲线，Hz，采样率为帧数
            //speedup：加速倍数
            var tokens = phrase.phones
                .Select(p => p.phoneme)
                .Append("SP")
                .Select(x => (long)(singer.phonemes.IndexOf(x)))
                .ToList();
            var durations = phrase.phones
                .Select(p => (long)(p.endMs / frameMs) - (long)(p.positionMs / frameMs))//防止累计误差
                .Append(tailFrames)
                .ToList();
            var totalFrames = (int)(durations.Sum());
            float[] f0 = DiffSingerUtils.SampleCurve(phrase, phrase.pitches, 0, frameMs, totalFrames, headFrames, tailFrames, 
                x => MusicMath.ToneToFreq(x * 0.01))
                .Select(f => (float)f).ToArray();
            //toneShift isn't supported

            var acousticInputs = new List<NamedOnnxValue>();
            acousticInputs.Add(NamedOnnxValue.CreateFromTensor("tokens",
                new DenseTensor<long>(tokens.ToArray(), new int[] { tokens.Count },false)
                .Reshape(new int[] { 1, tokens.Count })));
            acousticInputs.Add(NamedOnnxValue.CreateFromTensor("durations",
                new DenseTensor<long>(durations.ToArray(), new int[] { durations.Count }, false)
                .Reshape(new int[] { 1, durations.Count })));
            var f0tensor = new DenseTensor<float>(f0, new int[] { f0.Length })
                .Reshape(new int[] { 1, f0.Length });
            acousticInputs.Add(NamedOnnxValue.CreateFromTensor("f0",f0tensor));
            acousticInputs.Add(NamedOnnxValue.CreateFromTensor("speedup",
                new DenseTensor<long>(new long[] { speedup }, new int[] { 1 },false)));

            //gender
            //OpenUTAU中，GENC的定义：100=共振峰移动12个半音，正的GENC为向下移动
            if (singer.dsConfig.useKeyShiftEmbed) {
                var range = singer.dsConfig.augmentationArgs.randomPitchShifting.range;
                var positiveScale = (range[1]==0) ? 0 : (12/range[1]/100);
                var negativeScale = (range[0]==0) ? 0 : (-12/range[0]/100);
                float[] gender = DiffSingerUtils.SampleCurve(phrase, phrase.gender, 
                    0, frameMs, totalFrames, headFrames, tailFrames,
                    x=> (x<0)?(-x * positiveScale):(-x * negativeScale))
                    .Select(f => (float)f).ToArray();
                var genderTensor = new DenseTensor<float>(gender, new int[] { gender.Length })
                    .Reshape(new int[] { 1, gender.Length });
                acousticInputs.Add(NamedOnnxValue.CreateFromTensor("gender", genderTensor));
            }

            //velocity
            //OpenUTAU中，velocity的定义：默认100为原速，每增大100为速度乘2
            if (singer.dsConfig.useSpeedEmbed) {
                var velocityCurve = phrase.curves.FirstOrDefault(curve => curve.Item1 == VELC);
                float[] velocity;
                if (velocityCurve != null) {
                    velocity = DiffSingerUtils.SampleCurve(phrase, velocityCurve.Item2,
                        1, frameMs, totalFrames, headFrames, tailFrames,
                        x => Math.Pow(2, (x - 100) / 100))
                        .Select(f => (float)f).ToArray();
                } else {
                    velocity = Enumerable.Repeat(1f, totalFrames).ToArray();
                }
                var velocityTensor = new DenseTensor<float>(velocity, new int[] { velocity.Length })
                    .Reshape(new int[] { 1, velocity.Length });
                acousticInputs.Add(NamedOnnxValue.CreateFromTensor("velocity", velocityTensor));
            }

            Tensor<float> mel;
            var acousticOutputs = singer.getAcousticSession().Run(acousticInputs);
            mel = acousticOutputs.First().AsTensor<float>().Clone();
            
            //vocoder
            //waveform = session.run(['waveform'], {'mel': mel, 'f0': f0})[0]
            var vocoderInputs = new List<NamedOnnxValue>();
            vocoderInputs.Add(NamedOnnxValue.CreateFromTensor("mel", mel));
            vocoderInputs.Add(NamedOnnxValue.CreateFromTensor("f0",f0tensor));
            float[] samples;
            var vocoderOutputs = vocoder.session.Run(vocoderInputs);
            samples = vocoderOutputs.First().AsTensor<float>().ToArray();
            return samples;
        }

        //加载音高渲染结果（不支持）
        public RenderPitchResult LoadRenderedPitch(RenderPhrase phrase) {
            return null;
        }

        public UExpressionDescriptor[] GetSuggestedExpressions(USinger singer, URenderSettings renderSettings) {
            return new UExpressionDescriptor[] {
                new UExpressionDescriptor{
                    name="velocity (curve)",
                    abbr=VELC,
                    type=UExpressionType.Curve,
                    min=0,
                    max=200,
                    defaultValue=100,
                    isFlag=false,
                }
            };
        }

        public override string ToString() => Renderers.DIFFSINGER;
    }
}

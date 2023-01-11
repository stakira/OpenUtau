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
        const float headMs = 0;
        const float tailMs = 0;

        static readonly HashSet<string> supportedExp = new HashSet<string>(){
            Format.Ustx.DYN,
            Format.Ustx.PITD,
        };

        struct AcousticResult {
            public string path_acoustic;
            public string path_f0;
            public string path_spectrogram;
            public string path_aperiodicity;
        }

        struct AcousticResponse {
            public string error;
            public AcousticResult result;
        }

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
            var singer = phrase.singer as DiffSingerSinger;
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
                .Select(x => (long)(singer.phonemes.IndexOf(x)))
                .ToList();
            var durations = phrase.phones
                .Select(p => (long)(p.endMs / frameMs) - (long)(p.positionMs / frameMs))//防止累计误差
                .ToList();
            var totalFrames = (int)(durations.Sum());
            var f0 = SampleCurve(phrase, phrase.pitches, 0, totalFrames, headFrames, tailFrames, x => MusicMath.ToneToFreq(x * 0.01));
            float[] f0Shifted = f0.Select(f => (float)f).ToArray();
            //TODO:toneShift

            var acousticInputs = new List<NamedOnnxValue>();
            acousticInputs.Add(NamedOnnxValue.CreateFromTensor("tokens",
                new DenseTensor<long>(tokens.ToArray(), new int[] { tokens.Count },false)
                .Reshape(new int[] { 1, tokens.Count })));
            acousticInputs.Add(NamedOnnxValue.CreateFromTensor("durations",
                new DenseTensor<long>(durations.ToArray(), new int[] { durations.Count }, false)
                .Reshape(new int[] { 1, durations.Count })));
            var f0tensor = new DenseTensor<float>(f0Shifted, new int[] { f0Shifted.Length })
                .Reshape(new int[] { 1, f0Shifted.Length });
            acousticInputs.Add(NamedOnnxValue.CreateFromTensor("f0",f0tensor));
            acousticInputs.Add(NamedOnnxValue.CreateFromTensor("speedup",
                new DenseTensor<long>(new long[] { speedup }, new int[] { },false)));
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

        //参数曲线采样
        double[] SampleCurve(RenderPhrase phrase, float[] curve, double defaultValue, int length, int headFrames, int tailFrames, Func<double, double> convert) {
            var singer = phrase.singer as DiffSingerSinger;
            var frameMs = singer.getVocoder().frameMs();
            const int interval = 5;
            var result = new double[length];
            if (curve == null) {
                Array.Fill(result, defaultValue);
                return result;
            }
            
            for (int i = 0; i < length - headFrames - tailFrames; i++) {
                double posMs = phrase.positionMs - phrase.leadingMs + i * frameMs;
                int ticks = phrase.timeAxis.MsPosToTickPos(posMs) - (phrase.position - phrase.leading);
                int index = Math.Max(0, (int)((double)ticks / interval));
                if (index < curve.Length) {
                    result[i + headFrames] = convert(curve[index]);
                }
            }
            //填充头尾
            Array.Fill(result, defaultValue, 0, headFrames);
            Array.Fill(result, defaultValue, length - tailFrames, tailFrames);
            return result;
        }

        //加载音高渲染结果（不支持）
        public RenderPitchResult LoadRenderedPitch(RenderPhrase phrase) {
            return null;
        }

        public override string ToString() => Renderers.DIFFSINGER;
    }
}

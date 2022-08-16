using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using K4os.Hash.xxHash;
using NAudio.Wave;
using NumSharp;
using OpenUtau.Core.Format;
using OpenUtau.Core.Render;
using OpenUtau.Core.SignalChain;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Core.DiffSinger {
    public class DiffSingerRenderer : IRenderer {
        public const int headTicks = 0;
        public const int tailTicks = 0;

        static readonly HashSet<string> supportedExp = new HashSet<string>(){
            /*Format.Ustx.DYN,
            Format.Ustx.PITD,
            Format.Ustx.GENC,
            Format.Ustx.BREC,
            Format.Ustx.TENC,
            Format.Ustx.VOIC,*/
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
            var firstPhone = phrase.phones.First();
            var lastPhone = phrase.phones.Last();
            return new RenderResult() {
                leadingMs = headTicks * phrase.tickToMs,
                positionMs = (phrase.position + firstPhone.position) * phrase.tickToMs,
                estimatedLengthMs = (lastPhone.duration + lastPhone.position - firstPhone.position + headTicks + tailTicks) * phrase.tickToMs,
            };
        }

        public Task<RenderResult> Render(RenderPhrase phrase, Progress progress, CancellationTokenSource cancellation, bool isPreRender) {
            var task = Task.Run(() => {
                lock (lockObj) {
                    if (cancellation.IsCancellationRequested) {
                        return new RenderResult();
                    }
                    string progressInfo = string.Join(" ", phrase.phones.Select(p => p.phoneme));
                    progress.Complete(0, progressInfo);
                    ulong preEffectHash = PreEffectsHash(phrase);
                    //分配缓存目录
                    var tmpPath = Path.Join(PathManager.Inst.CachePath, $"enu-{phrase.hash:x16}");
                    var ustPath = tmpPath + ".tmp";
                    var wavPath = Path.Join(PathManager.Inst.CachePath, $"enu-{phrase.hash:x16}.wav");
                    var result = Layout(phrase);
                    //如果还没合成wav文件，则合成。否则直接读
                    if (!File.Exists(wavPath)) {
                        Log.Information($"Starting DiffSinger acoustic \"{ustPath}\"");
                        var DiffSingerNotes = PhraseToDiffSingerNotes(phrase);
                        //写入ust
                        DiffSingerUtils.WriteUst(DiffSingerNotes, phrase.tempo, phrase.singer, ustPath);
                        //向python服务器发送请求，合成。"acoustic"
                        var response = DiffSingerClient.Inst.SendRequest<AcousticResponse>(new string[] { "acoustic", ustPath });
                        if (response.error != null) {
                            throw new Exception(response.error);
                        }
                        if (cancellation.IsCancellationRequested) {
                            return new RenderResult();
                        }
                    }
                    progress.Complete(phrase.phones.Length, progressInfo);
                    if (File.Exists(wavPath)) {
                        using (var waveStream = Wave.OpenFile(wavPath)) {
                            result.samples = Wave.GetSamples(waveStream.ToSampleProvider().ToMono(1, 0));
                        }
                        if (result.samples != null) {
                            ApplyDynamics(phrase, result.samples);
                        }
                    } else {
                        result.samples = new float[0];
                    }
                    return result;
                }
            });
            return task;
        }
        /*result的格式：
        result.samples：渲染好的音频，float[]
        leadingMs、positionMs、estimatedLengthMs：时间轴相关，单位：毫秒，double
         */

        //加载音高渲染结果（待支持）
        public RenderPitchResult LoadRenderedPitch(RenderPhrase phrase) {
            return null;
        }
        /*
        public RenderPitchResult LoadRenderedPitch(RenderPhrase phrase) {
            ulong preEffectHash = PreEffectsHash(phrase);
            var tmpPath = Path.Join(PathManager.Inst.CachePath, $"enu-{preEffectHash:x16}");
            var enutmpPath = tmpPath + "_enutemp";
            var f0Path = Path.Join(enutmpPath, "f0.npy");
            var layout = Layout(phrase);
            if (!File.Exists(f0Path)) {
                return null;
            }
            var config = DiffSingerConfig.Load(phrase.singer);
            var f0 = np.Load<double[]>(f0Path);
            var result = new RenderPitchResult() {
                tones = f0.Select(f => (float)MusicMath.FreqToTone(f)).ToArray(),
            };
            result.ticks = new float[result.tones.Length];
            var t = layout.positionMs - layout.leadingMs;
            for (int i = 0; i < result.tones.Length; i++) {
                t += config.framePeriod;
                result.ticks[i] = (float)(t / phrase.tickToMs) - phrase.position;
            }
            return result;
        }*/

        //仅考虑谱面，而不考虑参数的hash
        private ulong PreEffectsHash(RenderPhrase phrase) {
            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {
                    writer.Write(phrase.singerId);
                    writer.Write(phrase.tempo);
                    writer.Write(phrase.tickToMs);
                    foreach (var phone in phrase.phones) {
                        writer.Write(phone.hash);
                    }
                    return XXH64.DigestOf(stream.ToArray());
                }
            }
        }

        //将OpenUTAU音素转化为diffsinger音符
        //#TODO:连音符
        static DiffSingerNote[] PhraseToDiffSingerNotes(RenderPhrase phrase) {
            var notes = new List<DiffSingerNote>();
            notes.Add(new DiffSingerNote {
                lyric = "R",
                length = headTicks,
                noteNum = 60,
            });
            foreach (var phone in phrase.phones) {
                notes.Add(new DiffSingerNote {
                    lyric = phone.phoneme,
                    length = phone.duration,
                    noteNum = phone.tone,
                });
            }
            notes.Add(new DiffSingerNote {
                lyric = "R",
                length = tailTicks,
                noteNum = 60,
            });
            return notes.ToArray();
        }

        double[] DownSampleCurve(float[] curve, double defaultValue, double frameMs, int length, int headFrames, int tailFrames, double tickToMs, Func<double, double> convert) {
            const int interval = 5;
            var result = new double[length];
            if (curve == null) {
                Array.Fill(result, defaultValue);
                return result;
            }
            for (int i = 0; i < length - headFrames - tailFrames; i++) {
                int index = (int)(i * frameMs / tickToMs / interval);
                if (index < curve.Length) {
                    result[i + headFrames] = convert(curve[index]);
                }
            }
            Array.Fill(result, defaultValue, 0, headFrames);
            Array.Fill(result, defaultValue, length - tailFrames, tailFrames);
            return result;
        }

        //将音量曲线运用到输出的音频上
        void ApplyDynamics(RenderPhrase phrase, float[] samples) {
            const int interval = 5;
            if (phrase.dynamics == null) {
                return;
            }
            int pos = 0;
            int offset = (int)(240 * phrase.tickToMs / 1000 * 44100);
            for (int i = 0; i < phrase.dynamics.Length; ++i) {
                int endPos = (int)((i + 1) * interval * phrase.tickToMs / 1000 * 44100);
                float a = phrase.dynamics[i];
                float b = (i + 1) == phrase.dynamics.Length ? phrase.dynamics[i] : phrase.dynamics[i + 1];
                for (int j = pos; j < endPos; ++j) {
                    samples[offset + j] *= a + (b - a) * (j - pos) / (endPos - pos);
                }
                pos = endPos;
            }
        }

        public override string ToString() => "DiffSinger";
    }
}

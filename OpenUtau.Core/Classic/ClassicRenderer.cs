using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Classic {
    public class ClassicRenderer : IRenderer {
        static readonly HashSet<string> supportedExp = new HashSet<string>(){
            Core.Format.Ustx.DYN,
            Core.Format.Ustx.PITD,
            Core.Format.Ustx.CLR,
            Core.Format.Ustx.SHFT,
            Core.Format.Ustx.ENG,
            Core.Format.Ustx.VEL,
            Core.Format.Ustx.VOL,
            Core.Format.Ustx.ATK,
            Core.Format.Ustx.DEC,
            Core.Format.Ustx.MOD,
            Core.Format.Ustx.ALT,
        };

        public USingerType SingerType => USingerType.Classic;

        public bool SupportsRenderPitch => false;

        public bool SupportsExpression(UExpressionDescriptor descriptor) {
            return descriptor.isFlag
                || !string.IsNullOrEmpty(descriptor.flag)
                || supportedExp.Contains(descriptor.abbr);
        }

        public RenderResult Layout(RenderPhrase phrase) {
            var firstPhone = phrase.phones.First();
            var lastPhone = phrase.phones.Last();
            return new RenderResult() {
                leadingMs = firstPhone.leadingMs,
                positionMs = firstPhone.positionMs,
                estimatedLengthMs = lastPhone.positionMs - firstPhone.positionMs + firstPhone.leadingMs,
            };
        }

        public Task<RenderResult> Render(RenderPhrase phrase, Progress progress, CancellationTokenSource cancellation, bool isPreRender) {
            var resamplerItems = new List<ResamplerItem>();
            foreach (var phone in phrase.phones) {
                resamplerItems.Add(new ResamplerItem(phrase, phone));
            }
            var task = Task.Run(() => {
                Parallel.ForEach(source: resamplerItems, parallelOptions: new ParallelOptions() {
                    MaxDegreeOfParallelism = 2
                }, body: item => {
                    if (!cancellation.IsCancellationRequested && !File.Exists(item.outputFile)) {
                        if (!(item.resampler is WorldlineResampler)) {
                            VoicebankFiles.Inst.CopySourceTemp(item.inputFile, item.inputTemp);
                        }
                        lock (Renderers.GetCacheLock(item.outputFile)) {
                            item.resampler.DoResamplerReturnsFile(item, Serilog.Log.Logger);
                        }
                        if (!File.Exists(item.outputFile)) {
                            throw new InvalidDataException($"{item.resampler.Name} failed to resample \"{item.phone.phoneme}\"");
                        }
                        if (!(item.resampler is WorldlineResampler)) {
                            VoicebankFiles.Inst.CopyBackMetaFiles(item.inputFile, item.inputTemp);
                        }
                    }
                    progress.Complete(1, $"Resampling \"{item.phone.phoneme}\"");
                });
                var result = Layout(phrase);
                result.samples = Concatenate(resamplerItems, cancellation);
                if (result.samples != null) {
                    ApplyDynamics(phrase, result);
                }
                return result;
            });
            return task;
        }

        float[] Concatenate(List<ResamplerItem> resamplerItems, CancellationTokenSource cancellation) {
            var wavtool = new SharpWavtool(Core.Util.Preferences.Default.PhaseCompensation == 1);
            return wavtool.Concatenate(resamplerItems, cancellation);
        }

        void ApplyDynamics(RenderPhrase phrase, RenderResult result) {
            const int interval = 5;
            if (phrase.dynamics == null) {
                return;
            }
            int startTick = phrase.position + phrase.phones.First().position - phrase.phones.First().leading;
            double startMs = result.positionMs - result.leadingMs;
            int startSample = 0;
            for (int i = 0; i < phrase.dynamics.Length; ++i) {
                int endTick = startTick + interval;
                double endMs = phrase.timeAxis.TickPosToMsPos(endTick);
                int endSample = (int)((endMs - startMs) / 1000 * 44100);
                float a = phrase.dynamics[i];
                float b = (i + 1) == phrase.dynamics.Length ? phrase.dynamics[i] : phrase.dynamics[i + 1];
                for (int j = startSample; j < endSample; ++j) {
                    result.samples[j] *= a + (b - a) * (j - startSample) / (endSample - startSample);
                }
                startTick = endTick;
                startSample = endSample;
            }
        }

        public RenderPitchResult LoadRenderedPitch(RenderPhrase phrase) {
            return null;
        }

        public override string ToString() => "CLASSIC";
    }
}

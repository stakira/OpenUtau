using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Classic {
    class ClassicRenderer : IRenderer {
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

        public bool SupportsExpression(UExpressionDescriptor descriptor) {
            return descriptor.isFlag
                || !string.IsNullOrEmpty(descriptor.flag)
                || supportedExp.Contains(descriptor.abbr);
        }

        public RenderResult Layout(RenderPhrase phrase) {
            var firstPhone = phrase.phones.First();
            var lastPhone = phrase.phones.Last();
            return new RenderResult() {
                leadingMs = firstPhone.preutterMs,
                positionMs = (phrase.position + firstPhone.position) * phrase.tickToMs,
                estimatedLengthMs = (lastPhone.duration + lastPhone.position - firstPhone.position) * phrase.tickToMs + firstPhone.preutterMs,
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
                        VoicebankFiles.CopySourceTemp(item.inputFile, item.inputTemp);
                        item.resampler.DoResamplerReturnsFile(item, Serilog.Log.Logger);
                        if (!File.Exists(item.outputFile)) {
                            throw new InvalidDataException($"{item.resampler.Name} failed to resample \"{item.phone.phoneme}\"");
                        }
                        VoicebankFiles.CopyBackMetaFiles(item.inputFile, item.inputTemp);
                    }
                    progress.CompleteOne($"Resampling \"{item.phone.phoneme}\"");
                });
                var firstPhone = phrase.phones.First();
                var result = Layout(phrase);
                result.samples = Concatenate(resamplerItems, cancellation);
                if (result.samples != null) {
                    ApplyDynamics(phrase, result.samples);
                }
                return result;
            });
            return task;
        }

        float[] Concatenate(List<ResamplerItem> resamplerItems, CancellationTokenSource cancellation) {
            var wavtool = new SharpWavtool(Core.Util.Preferences.Default.PhaseCompensation == 1);
            return wavtool.Concatenate(resamplerItems, cancellation);
        }

        void ApplyDynamics(RenderPhrase phrase, float[] samples) {
            const int interval = 5;
            if (phrase.dynamics == null) {
                return;
            }
            int pos = 0;
            for (int i = 0; i < phrase.dynamics.Length; ++i) {
                int endPos = (int)((i + 1) * interval * phrase.tickToMs / 1000 * 44100);
                float a = phrase.dynamics[i];
                float b = (i + 1) == phrase.dynamics.Length ? phrase.dynamics[i] : phrase.dynamics[i + 1];
                for (int j = pos; j < endPos; ++j) {
                    samples[j] *= a + (b - a) * (j - pos) / (endPos - pos);
                }
                pos = endPos;
            }
        }
    }
}

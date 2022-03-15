using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenUtau.Core.Render;

namespace OpenUtau.Classic {
    class ClassicRenderer : IRenderer {
        public Task<RenderResult> Render(RenderPhrase phrase, Progress progress, CancellationTokenSource cancellation) {
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
                        VoicebankFiles.CopyBackMetaFiles(item.inputFile, item.inputTemp);
                    }
                    progress.CompleteOne($"Resampling \"{item.phone.phoneme}\"");
                });
                var samples = Concatenate(resamplerItems, cancellation);
                var firstPhone = phrase.phones.First();
                return new RenderResult() {
                    samples = samples,
                    leadingMs = firstPhone.preutterMs,
                    positionMs = (phrase.position + firstPhone.position) * phrase.tickToMs,
                };
            });
            return task;
        }

        float[] Concatenate(List<ResamplerItem> resamplerItems, CancellationTokenSource cancellation) {
            var wavtool = new SharpWavtool(Core.Util.Preferences.Default.PhaseCompensation == 1);
            return wavtool.Concatenate(resamplerItems, cancellation);
        }
    }
}

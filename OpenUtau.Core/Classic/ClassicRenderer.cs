using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OpenUtau.Core.Render;
using OpenUtau.Core.ResamplerDriver;

namespace OpenUtau.Classic {
    public class ClassicRenderer : IRenderer {
        public Task<float[]> Render(RenderPhrase phrase, CancellationTokenSource cancellation) {
            var resamplerItems = new List<ResamplerItem>();
            foreach (var phone in phrase.phones) {
                resamplerItems.Add(new ResamplerItem(phrase, phone));
            }
            var task = Task.Run(() => {
                Parallel.ForEach(source: resamplerItems, parallelOptions: new ParallelOptions() {
                    MaxDegreeOfParallelism = 2
                }, body: item => {
                    if (cancellation.IsCancellationRequested) {
                        return;
                    }
                    var engineInput = new DriverModels.EngineInput() {
                        inputWaveFile = item.inputTemp,
                        outputWaveFile = item.outputFile,
                        NoteString = item.tone,
                        Velocity = item.velocity,
                        StrFlags = item.flags,
                        Offset = item.offset,
                        RequiredLength = item.requiredLength,
                        Consonant = item.consonant,
                        Cutoff = item.cutoff,
                        Volume = item.volume,
                        Modulation = item.modulation,
                        pitchBend = item.pitches,
                        nPitchBend = item.pitches.Length,
                        Tempo = item.tempo,
                    };
                    item.resampler.DoResamplerReturnsFile(engineInput, Serilog.Log.Logger);
                });
                var samples = Concatenate(resamplerItems, cancellation);
                foreach (var item in resamplerItems) {
                    if (File.Exists(item.outputFile)) {
                        //File.Delete(item.outputFile);
                    }
                }
                return samples;
            });
            return task;
        }

        float[] Concatenate(List<ResamplerItem> resamplerItems, CancellationTokenSource cancellation) {
            var wavtool = new SharpWavtool();
            return wavtool.Concatenate(resamplerItems, cancellation);
        }
    }
}

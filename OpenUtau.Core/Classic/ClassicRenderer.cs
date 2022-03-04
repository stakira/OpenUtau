using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OpenUtau.Core.Render;
using OpenUtau.Core.ResamplerDriver;
using OpenUtau.Core.SignalChain;

namespace OpenUtau.Classic {
    public class ClassicRenderer : IRenderer {
        public Task<ISignalSource> Render(RenderPhrase phrase, Progress progress, CancellationTokenSource cancellation) {
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
                        VoicebankFiles.CopyBackMetaFiles(item.inputFile, item.inputTemp);
                    }
                    progress.CompleteOne($"Resampling \"{item.phone.phoneme}\"");
                });
                return Concatenate(resamplerItems, cancellation);
            });
            return task;
        }

        ISignalSource Concatenate(List<ResamplerItem> resamplerItems, CancellationTokenSource cancellation) {
            var wavtool = new SharpWavtool();
            return wavtool.Concatenate(resamplerItems, cancellation);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using OpenUtau.Core;
using OpenUtau.Core.Format;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Classic {
    public class ClassicRenderer : IRenderer {
        static readonly HashSet<string> supportedExp = new HashSet<string>(){
            Ustx.DYN,
            Ustx.PITD,
            Ustx.CLR,
            Ustx.SHFT,
            Ustx.ENG,
            Ustx.VEL,
            Ustx.VOL,
            Ustx.ATK,
            Ustx.DEC,
            Ustx.MOD,
            Ustx.ALT,
        };

        public USingerType SingerType => USingerType.Classic;

        public bool SupportsRenderPitch => false;

        public bool SupportsExpression(UExpressionDescriptor descriptor) {
            return descriptor.isFlag
                || !string.IsNullOrEmpty(descriptor.flag)
                || supportedExp.Contains(descriptor.abbr);
        }

        public RenderResult Layout(RenderPhrase phrase) {
            return new RenderResult() {
                leadingMs = phrase.leadingMs,
                positionMs = phrase.positionMs,
                estimatedLengthMs = phrase.durationMs + phrase.leadingMs,
            };
        }

        public Task<RenderResult> Render(RenderPhrase phrase, Progress progress, CancellationTokenSource cancellation, bool isPreRender) {
            if (phrase.wavtool == SharpWavtool.nameConvergence || phrase.wavtool == SharpWavtool.nameSimple) {
                return RenderInternal(phrase, progress, cancellation, isPreRender);
            } else {
                return RenderExternal(phrase, progress, cancellation, isPreRender);
            }
        }

        public Task<RenderResult> RenderInternal(RenderPhrase phrase, Progress progress, CancellationTokenSource cancellation, bool isPreRender) {
            var resamplerItems = new List<ResamplerItem>();
            foreach (var phone in phrase.phones) {
                resamplerItems.Add(new ResamplerItem(phrase, phone));
            }
            var task = Task.Run(() => {
                Parallel.ForEach(source: resamplerItems, parallelOptions: new ParallelOptions() {
                    MaxDegreeOfParallelism = Preferences.Default.NumRenderThreads
                }, body: item => {
                    if (!cancellation.IsCancellationRequested && !File.Exists(item.outputFile)) {
                        if (!(item.resampler is WorldlineResampler)) {
                            VoicebankFiles.Inst.CopySourceTemp(item.inputFile, item.inputTemp);
                        }
                        lock (Renderers.GetCacheLock(item.outputFile)) {
                            item.resampler.DoResamplerReturnsFile(item, Log.Logger);
                        }
                        if (!File.Exists(item.outputFile)) {
                            throw new InvalidDataException($"{item.resampler} failed to resample \"{item.phone.phoneme}\"");
                        }
                        if (!(item.resampler is WorldlineResampler)) {
                            VoicebankFiles.Inst.CopyBackMetaFiles(item.inputFile, item.inputTemp);
                        }
                    }
                    progress.Complete(1, $"{item.resampler} \"{item.phone.phoneme}\"");
                });
                var result = Layout(phrase);
                var wavtool = new SharpWavtool(true);
                result.samples = wavtool.Concatenate(resamplerItems, string.Empty, cancellation);
                if (result.samples != null) {
                    Renderers.ApplyDynamics(phrase, result);
                }
                return result;
            });
            return task;
        }

        public Task<RenderResult> RenderExternal(RenderPhrase phrase, Progress progress, CancellationTokenSource cancellation, bool isPreRender) {
            var resamplerItems = new List<ResamplerItem>();
            foreach (var phone in phrase.phones) {
                resamplerItems.Add(new ResamplerItem(phrase, phone));
            }
            var task = Task.Run(() => {
                string progressInfo = $"{phrase.wavtool} \"{string.Join(" ", phrase.phones.Select(p => p.phoneme))}\"";
                progress.Complete(0, progressInfo);
                var wavPath = Path.Join(PathManager.Inst.CachePath, $"cat-{phrase.hash:x16}.wav");
                var result = Layout(phrase);
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
                    foreach (var item in resamplerItems) {
                        VoicebankFiles.Inst.CopySourceTemp(item.inputFile, item.inputTemp);
                    }
                    var wavtool = ToolsManager.Inst.GetWavtool(phrase.wavtool);
                    result.samples = wavtool.Concatenate(resamplerItems, wavPath, cancellation);
                    foreach (var item in resamplerItems) {
                        VoicebankFiles.Inst.CopyBackMetaFiles(item.inputFile, item.inputTemp);
                    }
                }
                progress.Complete(phrase.phones.Length, progressInfo);
                if (result.samples != null) {
                    Renderers.ApplyDynamics(phrase, result);
                }
                return result;
            });
            return task;
        }

        public RenderPitchResult LoadRenderedPitch(RenderPhrase phrase) {
            return null;
        }

        public UExpressionDescriptor[] GetSuggestedExpressions(USinger singer, URenderSettings renderSettings) {
            var resamplerPath = renderSettings.Resampler.FilePath;
            if (resamplerPath == null) {
                return new UExpressionDescriptor[] { };
            }
            var resamplerManifestPath = Path.ChangeExtension(resamplerPath, ".yaml");
            try {
                return Yaml.DefaultDeserializer.Deserialize<ResamplerManifest>(
                    File.ReadAllText(resamplerManifestPath, encoding:Encoding.UTF8)
                    ).expressions.Values.ToArray();
            } catch (Exception ex) {
                Log.Error($"Failed loading suggested expressions from {resamplerManifestPath}: {ex}");
            }
            return new UExpressionDescriptor[] { };
        }

        public override string ToString() => Renderers.CLASSIC;
    }

    public class ResamplerManifest {
        public Dictionary<string,UExpressionDescriptor> expressions = new Dictionary<string, UExpressionDescriptor> { };
    }
}

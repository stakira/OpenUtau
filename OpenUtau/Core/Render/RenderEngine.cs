using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OpenUtau.Core.ResamplerDriver;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Core.Render {
    class RenderEngine {

        public class Progress {
            readonly int total;
            int completed = 0;
            public Progress(int total) {
                this.total = total;
            }

            public void CompleteOne(string info) {
                completed++;
                DocManager.Inst.ExecuteCmd(new ProgressBarNotification(completed * 100.0 / total, info), true);
            }

            public void Clear() {
                DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, string.Empty), true);
            }
        }

        readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        readonly UProject project;
        readonly IResamplerDriver driver;
        readonly RenderCache cache;

        public RenderEngine(UProject project, IResamplerDriver driver, RenderCache cache) {
            this.project = project;
            this.driver = driver;
            this.cache = cache;
        }

        public void Cancel() {
            cancellationTokenSource.Cancel();
        }

        public async Task<List<TrackSampleProvider>> RenderAsync() {
            List<TrackSampleProvider> trackSampleProviders = project.tracks.Select(
                track => new TrackSampleProvider() {
                    Volume = PlaybackManager.DecibelToVolume(track.Volume)
                }).ToList();
            var cacheDir = PathManager.Inst.GetCachePath(project.filePath);
            foreach (UPart part in project.parts) {
                UVoicePart voicePart = part as UVoicePart;
                if (voicePart != null) {
                    SequencingSampleProvider sampleProvider = await RenderPartAsync(voicePart, project, cacheDir);
                    if (sampleProvider != null) {
                        trackSampleProviders[voicePart.TrackNo].AddSource(sampleProvider, TimeSpan.Zero);
                    }
                }
                UWavePart wavePart = part as UWavePart;
                if (wavePart != null) {
                    try {
                        var stream = new AudioFileReader(wavePart.FilePath);
                        trackSampleProviders[wavePart.TrackNo].AddSource(
                            new WaveToSampleProvider(stream),
                            TimeSpan.FromMilliseconds(project.TickToMillisecond(wavePart.PosTick)));
                    } catch (Exception e) {
                        Log.Error(e, "Failed to open audio file");
                    }
                }
            }
            return trackSampleProviders;
        }

        async Task<SequencingSampleProvider> RenderPartAsync(UVoicePart part, UProject project, string cacheDir) {
            var singer = project.tracks[part.TrackNo].Singer;
            if (singer == null || !singer.Loaded) {
                return null;
            }
            var tasks = new List<Task<RenderItem>>();
            var progress = new Progress(part.notes.Sum(note => note.phonemes.Count));
            progress.Clear();
            foreach (var note in part.notes) {
                foreach (var phoneme in note.phonemes) {
                    if (string.IsNullOrEmpty(phoneme.oto.File)) {
                        Log.Warning($"Cannot find phoneme in note {note.lyric}");
                        continue;
                    }
                    var item = new RenderItem(phoneme, part, project);
                    item.progress = progress;
                    tasks.Add(Task<RenderItem>.Factory.StartNew(ResamplePhonemeAsync, item, cancellationTokenSource.Token));
                }
            }
            await Task.WhenAll(tasks.ToArray());
            progress.Clear();
            return new SequencingSampleProvider(tasks.Select(task => new RenderItemSampleProvider(task.Result)));
        }

        RenderItem ResamplePhonemeAsync(object state) {
            RenderItem item = state as RenderItem;
            uint hash = item.HashParameters();
            byte[] data = cache.Get(hash);
            if (data == null) {
                data = driver.DoResampler(DriverModels.CreateInputModel(item, 0));
                cache.Put(hash, data);
                Log.Information($"Sound {hash:x} {item.GetResamplerExeArgs()} resampled.");
            } else {
                Log.Information($"Sound {hash:x} {item.GetResamplerExeArgs()} cache retrieved.");
            }
            var stream = new MemoryStream(data);
            item.Sound = MemorySampleProvider.FromStream(stream);
            item.progress.CompleteOne($"Resampling \"{item.phonemeName}\"");
            return item;
        }
    }
}

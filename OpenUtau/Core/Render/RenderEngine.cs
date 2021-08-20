using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
                DocManager.Inst.ExecuteCmd(new ProgressBarNotification(completed * 100.0 / total, info));
            }

            public void Clear() {
                DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, string.Empty));
            }
        }

        readonly UProject project;
        readonly IResamplerDriver driver;
        readonly RenderCache cache;
        readonly int startTick;

        public RenderEngine(UProject project, IResamplerDriver driver, RenderCache cache, int startTick = 0) {
            this.project = project;
            this.driver = driver;
            this.cache = cache;
            this.startTick = startTick;
        }

        public async Task<List<TrackSampleProvider>> RenderAsync() {
            TimeSpan skip = TimeSpan.FromMilliseconds(project.TickToMillisecond(startTick));
            List<TrackSampleProvider> trackSampleProviders = project.tracks.Select(
                track => new TrackSampleProvider() {
                    Volume = PlaybackManager.DecibelToVolume(track.Volume)
                }).ToList();
            foreach (UPart part in project.parts) {
                UVoicePart voicePart = part as UVoicePart;
                if (voicePart != null) {
                    SequencingSampleProvider sampleProvider = await RenderPartAsync(voicePart, project);
                    if (sampleProvider != null) {
                        trackSampleProviders[voicePart.TrackNo].AddSource(sampleProvider, TimeSpan.Zero);
                    }
                }
                UWavePart wavePart = part as UWavePart;
                if (wavePart != null) {
                    try {
                        var stream = new AudioFileReader(wavePart.FilePath);
                        var offset = TimeSpan.FromMilliseconds(project.TickToMillisecond(wavePart.PosTick)) - skip;
                        var skipOver = offset < TimeSpan.Zero ? -offset : TimeSpan.Zero;
                        var delayBy = offset > TimeSpan.Zero ? offset : TimeSpan.Zero;
                        trackSampleProviders[wavePart.TrackNo].AddSource(
                            new WaveToSampleProvider(stream).Skip(skipOver), delayBy);
                    } catch (Exception e) {
                        Log.Error(e, "Failed to open audio file");
                    }
                }
            }
            return trackSampleProviders;
        }

        async Task<SequencingSampleProvider> RenderPartAsync(UVoicePart part, UProject project) {
            TimeSpan skip = TimeSpan.FromMilliseconds(project.TickToMillisecond(startTick));
            var singer = project.tracks[part.TrackNo].Singer;
            if (singer == null || !singer.Loaded) {
                return null;
            }
            var source = new CancellationTokenSource();
            var items = PreparePart(part, project).ToArray();
            var progress = new Progress(items.Length);
            progress.Clear();
            var tasks = items.Select(item => {
                item.progress = progress;
                return Task<RenderItem>.Factory.StartNew(ResampleItem, item, source.Token);
            }).ToArray();
            await Task.WhenAll(tasks);
            source.Dispose();
            progress.Clear();
            return new SequencingSampleProvider(tasks.Select(task => new RenderItemSampleProvider(task.Result, skip)));
        }

        public CancellationTokenSource PreRenderProject() {
            var source = new CancellationTokenSource();
            Task.Run(() => {
                Thread.Sleep(200);
                if (source.Token.IsCancellationRequested) {
                    return;
                }
                TaskFactory<RenderItem> factory = new TaskFactory<RenderItem>(source.Token);
                using (SemaphoreSlim slim = new SemaphoreSlim(4)) {
                    var items = PrepareProject(project).ToArray();
                    var progress = new Progress(items.Length);
                    var tasks = items
                        .Select(item => factory.StartNew((obj) => {
                            slim.Wait();
                            if (source.Token.IsCancellationRequested) {
                                return null;
                            }
                            var renderItem = obj as RenderItem;
                            renderItem.progress = progress;
                            Resample(renderItem);
                            slim.Release();
                            return renderItem;
                        }, item, source.Token)).ToArray();
                    Task.WaitAll(tasks);
                    ReleaseSourceTemp();
                    progress.Clear();
                }
            });
            return source;
        }

        IEnumerable<RenderItem> PrepareProject(UProject project) {
            return project.parts
                .Where(part => part is UVoicePart)
                .Select(part => part as UVoicePart)
                .SelectMany(part => PreparePart(part, project));
        }

        IEnumerable<RenderItem> PreparePart(UVoicePart part, UProject project) {
            return part.notes
                .SelectMany(note => note.phonemes)
                .Where(phoneme => !phoneme.Error)
                .Where(phoneme => part.PosTick + phoneme.Parent.position + phoneme.End > startTick)
                .Select(phoneme => new RenderItem(phoneme, part, project, driver.GetInfo().Name));
        }

        RenderItem ResampleItem(object state) {
            var item = state as RenderItem;
            Resample(item);
            return item;
        }

        void Resample(RenderItem item) {
            uint hash = item.HashParameters();
            byte[] data = cache.Get(hash);
            if (data == null) {
                CopySourceTemp(item);
                data = driver.DoResampler(DriverModels.CreateInputModel(item, 0));
                cache.Put(hash, data);
                Log.Information($"Sound {hash:x} {item.GetResamplerExeArgs()} resampled.");
            }
            item.Data = data;
            item.progress?.CompleteOne($"Resampling \"{item.phonemeName}\"");
        }

        void CopySourceTemp(RenderItem item) {
            string sourceTemp = item.SourceTemp;
            Log.Information($"Copy temp {item.SourceFile} {sourceTemp}");
            CopyOrStamp(item.SourceFile, sourceTemp);
            var dir = Path.GetDirectoryName(item.SourceFile);
            var pattern = Path.GetFileName(item.SourceFile).Replace(".wav", "_wav.*");
            Directory.EnumerateFiles(dir, pattern, SearchOption.TopDirectoryOnly)
                .ToList()
                .ForEach(frq => {
                    string newFrq = sourceTemp.Replace(".wav", "_wav") + Path.GetExtension(frq);
                    CopyOrStamp(frq, newFrq);
                    Log.Information($"Copy frequency map {frq} {newFrq}");
                });
        }

        void CopyOrStamp(string source, string dest) {
            bool exists = File.Exists(dest);
            if (!exists) {
                File.Copy(source, dest);
            } else {
                File.SetLastWriteTime(dest, DateTime.Now);
            }
        }

        void ReleaseSourceTemp() {
            var expire = DateTime.Now - TimeSpan.FromDays(3);
            string path = PathManager.Inst.GetCachePath(null);
            Log.Information($"ReleaseSourceTemp {path}");
            Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly)
                .Where(file =>
                    !File.GetAttributes(file).HasFlag(FileAttributes.Directory)
                        && File.GetLastWriteTime(file) < expire)
                .ToList()
                .ForEach(file => File.Delete(file));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OpenUtau.Core.ResamplerDriver;
using OpenUtau.Core.SignalChain;
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
        readonly Dictionary<string, IResamplerDriver> resamplers;

        public RenderEngine(UProject project, IResamplerDriver driver, RenderCache cache, int startTick = 0) {
            this.project = project;
            this.driver = driver;
            this.cache = cache;
            this.startTick = startTick;
            resamplers = ResamplerDriver.ResamplerDriver
                .Search(PathManager.Inst.GetEngineSearchPath())
                .ToDictionary(resampler => resampler.GetInfo().Name, resampler => resampler);
        }

        public Tuple<MasterAdapter, List<Fader>, CancellationTokenSource, Task> RenderProject(int startTick) {
            var source = new CancellationTokenSource();
            var items = new List<RenderItem>();
            var faders = new List<Fader>();
            foreach (var track in project.tracks) {
                var trackItems = PrepareTrack(track, project, startTick).ToArray();
                var sources = trackItems.Select(item => {
                    var waveSource = new WaveSource(item.PosMs, item.DurMs, item.Envelope, item.SkipOver);
                    item.OnComplete = data => waveSource.SetWaveData(data);
                    return waveSource;
                }).ToList();
                sources.AddRange(project.parts
                    .Where(part => part is UWavePart && part.trackNo == track.TrackNo)
                    .Select(part => part as UWavePart)
                    .Select(part => {
                        var waveSource = new WaveSource(
                            project.TickToMillisecond(part.position),
                            project.TickToMillisecond(part.Duration),
                            null,
                            project.TickToMillisecond(part.HeadTrimTick));
                        if (part.Samples != null) {
                            waveSource.SetSamples(part.Samples);
                        }
                        return waveSource;
                    }));
                var trackMix = new WaveMix(sources);
                items.AddRange(trackItems);
                var fader = new Fader(trackMix);
                fader.Scale = PlaybackManager.DecibelToVolume(track.Volume);
                faders.Add(fader);
            }
            items = items.OrderBy(item => item.PosMs).ToList();
            int threads = Util.Preferences.Default.PrerenderThreads;
            var progress = new Progress(items.Count);
            var task = Task.Run(() => {
                var progress = new Progress(items.Count);
                Parallel.ForEach(source: items, parallelOptions: new ParallelOptions() {
                    MaxDegreeOfParallelism = threads
                }, body: item => {
                    if (source.Token.IsCancellationRequested) {
                        return;
                    }
                    item.progress = progress;
                    Resample(item);
                });
                ReleaseSourceTemp();
                progress.Clear();
            });
            var master = new MasterAdapter(new WaveMix(faders));
            master.SetPosition((int)(project.TickToMillisecond(startTick) * 44100 / 1000));
            return Tuple.Create(master, faders, source, task);
        }

        public List<WaveMix> RenderTracks() {
            List<WaveMix> result = new List<WaveMix>();
            foreach (var track in project.tracks) {
                var items = PrepareTrack(track, project, 0);
                var progress = new Progress(items.Count());
                var mix = new WaveMix(items.Select(item => {
                    var waveSource = new WaveSource(item.PosMs, item.DurMs, item.Envelope, item.SkipOver);
                    item.progress = progress;
                    item.OnComplete = data => waveSource.SetWaveData(data);
                    Resample(item);
                    return waveSource;
                }));
                progress.Clear();
                result.Add(mix);
            }
            return result;
        }

        public CancellationTokenSource PreRenderProject() {
            int threads = Util.Preferences.Default.PrerenderThreads;
            var source = new CancellationTokenSource();
            Task.Run(() => {
                try {
                    Thread.Sleep(200);
                    if (source.Token.IsCancellationRequested) {
                        return;
                    }
                    RenderItem[] items;
                    lock (project) {
                        items = PrepareProject(project, startTick)
                            .OrderBy(item => item.PosMs)
                            .ToArray();
                    }
                    var progress = new Progress(items.Length);
                    Parallel.ForEach(source: items, parallelOptions: new ParallelOptions() {
                        MaxDegreeOfParallelism = threads
                    }, body: item => {
                        if (source.Token.IsCancellationRequested) {
                            return;
                        }
                        item.progress = progress;
                        Resample(item);
                    });
                    ReleaseSourceTemp();
                    progress.Clear();
                } catch (Exception e) {
                    if (!source.IsCancellationRequested) {
                        Log.Error(e, "Failed to pre-render.");
                    }
                }
            });
            return source;
        }

        IEnumerable<RenderItem> PrepareProject(UProject project, int startTick) {
            return project.tracks
                .SelectMany(track => PrepareTrack(track, project, startTick));
        }

        IEnumerable<RenderItem> PrepareTrack(UTrack track, UProject project, int startTick) {
            return project.parts
                .Where(part => part.trackNo == track.TrackNo)
                .Where(part => part is UVoicePart)
                .Select(part => part as UVoicePart)
                .SelectMany(part => PreparePart(part, track, project, startTick));
        }

        IEnumerable<RenderItem> PreparePart(UVoicePart part, UTrack track, UProject project, int startTick) {
            return part.notes
                .Where(note => !note.OverlapError)
                .SelectMany(note => note.phonemes)
                .Where(phoneme => !phoneme.Error)
                .Where(phoneme => part.position + phoneme.Parent.position + phoneme.End > startTick)
                .Select(phoneme => new RenderItem(phoneme, part, track, project, driver.GetInfo().Name));
        }

        RenderItem ResampleItem(object state) {
            var item = state as RenderItem;
            Resample(item);
            return item;
        }

        void Resample(RenderItem item) {
            byte[] data = null;
            try {
                uint hash = item.HashParameters();
                data = cache.Get(hash);
                if (data == null) {
                    CopySourceTemp(item);
                    if (!resamplers.TryGetValue(item.ResamplerName, out var driver)) {
                        driver = this.driver;
                    }
                    data = driver.DoResampler(DriverModels.CreateInputModel(item, 0), Log.Logger);
                    if (data == null || data.Length == 0) {
                        throw new Exception("Empty render result.");
                    }
                    cache.Put(hash, data);
                    Log.Information($"Sound {hash:x} {item.Oto.Alias} {item.GetResamplerExeArgs()} resampled.");
                }
            } catch (Exception e) {
                Log.Error(e, $"Failed to render item {item.SourceFile} {item.Oto.Alias} {item.GetResamplerExeArgs()}.");
            } finally {
                item.Data = data ?? new byte[0];
                item.OnComplete?.Invoke(item.Data);
                item.progress?.CompleteOne($"Resampling \"{item.phonemeName}\"");
            }
        }

        void CopySourceTemp(RenderItem item) {
            string sourceTemp = item.SourceTemp;
            CopyOrStamp(item.SourceFile, sourceTemp);
            var dir = Path.GetDirectoryName(item.SourceFile);
            var pattern = Path.GetFileName(item.SourceFile).Replace(".wav", "_wav.*");
            Directory.EnumerateFiles(dir, pattern, SearchOption.TopDirectoryOnly)
                .ToList()
                .ForEach(frq => {
                    string newFrq = sourceTemp.Replace(".wav", "_wav") + Path.GetExtension(frq);
                    CopyOrStamp(frq, newFrq);
                });
        }

        object fileAccessLock = new object();
        void CopyOrStamp(string source, string dest) {
            lock (fileAccessLock) {
                bool exists = File.Exists(dest);
                if (!exists) {
                    Log.Information($"Copy temp {source} {dest}");
                    File.Copy(source, dest);
                    File.SetLastAccessTime(dest, DateTime.Now);
                } else {
                    File.SetLastAccessTime(dest, DateTime.Now);
                }
            }
        }

        void ReleaseSourceTemp() {
            var expire = DateTime.Now - TimeSpan.FromDays(3);
            string path = PathManager.Inst.GetCachePath(null);
            Log.Information($"ReleaseSourceTemp {path}");
            Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly)
                .Where(file =>
                    !File.GetAttributes(file).HasFlag(FileAttributes.Directory)
                        && File.GetLastAccessTime(file) < expire)
                .ToList()
                .ForEach(file => File.Delete(file));
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OpenUtau.Core.ResamplerDriver;
using OpenUtau.Core.USTx;
using Serilog;
using xxHashSharp;

namespace OpenUtau.Core.Render {
    class RenderEngine {

        public class Progress {
            int total;
            int completed = 0;
            public Progress(int total) {
                this.total = total;
            }

            public void CompleteOne(string info) {
                completed++;
                DocManager.Inst.ExecuteCmd(new ProgressBarNotification(completed * 100 / total, info), true);
            }

            public void Clear() {
                DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, string.Empty), true);
            }
        }

        readonly object srcFileLock = new object();
        readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        readonly UProject project;
        readonly IResamplerDriver driver;

        public RenderEngine(UProject project, IResamplerDriver driver) {
            this.project = project;
            this.driver = driver;
        }

        public void Cancel() {
            cancellationTokenSource.Cancel();
        }

        public async Task<List<TrackSampleProvider>> RenderAsync() {
            List<TrackSampleProvider> trackSampleProviders = project.Tracks.Select(
                track => new TrackSampleProvider() {
                    Volume = PlaybackManager.DecibelToVolume(track.Volume)
                }).ToList();
            var cacheDir = PathManager.Inst.GetCachePath(project.FilePath);
            foreach (UPart part in project.Parts) {
                UVoicePart voicePart = part as UVoicePart;
                if (voicePart != null) {
                    SequencingSampleProvider sampleProvider = await RenderPartAsync(voicePart, project, cacheDir);
                    if (sampleProvider != null) {
                        trackSampleProviders[voicePart.TrackNo].AddSource(
                            sampleProvider,
                            TimeSpan.FromMilliseconds(project.TickToMillisecond(voicePart.PosTick)));
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
            var singer = project.Tracks[part.TrackNo].Singer;
            if (singer == null || !singer.Loaded) {
                return null;
            }
            var tasks = new List<Task<RenderItem>>();
            var progress = new Progress(part.Notes.Sum(note => note.Phonemes.Count));
            progress.Clear();
            foreach (var note in part.Notes) {
                foreach (var phoneme in note.Phonemes) {
                    if (string.IsNullOrEmpty(phoneme.Oto.File)) {
                        Log.Warning($"Cannot find phoneme in note {note.Lyric}");
                        continue;
                    }
                    var item = new RenderItem(phoneme, part, project);
                    item.progress = progress;
                    PrepareSourceFile(cacheDir, item);
                    tasks.Add(Task<RenderItem>.Factory.StartNew(ResamplePhonemeAsync, item, cancellationTokenSource.Token));
                }
            }
            await Task.WhenAll(tasks.ToArray());
            progress.Clear();
            return new SequencingSampleProvider(tasks.Select(task => new RenderItemSampleProvider(task.Result)));
        }

        RenderItem ResamplePhonemeAsync(object state) {
            RenderItem item = state as RenderItem;
            Log.Verbose($"Sound {item.HashParameters():x} resampling {item.GetResamplerExeArgs()}");
            var output = driver.DoResampler(DriverModels.CreateInputModel(item, 0));
            item.Sound = MemorySampleProvider.FromStream(output);
            output.Dispose();
            item.progress.CompleteOne($"Resampling \"{item.phonemeName}\"");
            return item;
        }

        /// <summary>
        /// Non-ASCII source file path does not well work with Process.Start(). Makes a copy to avoid Non-ASCII path.
        /// </summary>
        /// <param name="cacheDir"></param>
        /// <param name="item"></param>
        void PrepareSourceFile(string cacheDir, RenderItem item) {
            uint srcHash = xxHash.CalculateHash(Encoding.UTF8.GetBytes(item.SourceFile));
            string srcFilePath = Path.Combine(cacheDir, $"src_{srcHash}.wav");
            lock (srcFileLock) {
                if (!File.Exists(srcFilePath)) {
                    File.Copy(item.SourceFile, srcFilePath);
                }
            }
            item.SourceFile = srcFilePath;
        }
    }
}

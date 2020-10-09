using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using OpenUtau.Core.ResamplerDriver;
using OpenUtau.Core.USTx;
using Serilog;
using xxHashSharp;

namespace OpenUtau.Core.Render {

    internal class ResamplerInterface {
        Action<SequencingSampleProvider> resampleDoneCallback;
        object srcFileLock = new object();

        public void ResamplePart(UVoicePart part, UProject project, IResamplerDriver driver, Action<SequencingSampleProvider> resampleDoneCallback) {
            this.resampleDoneCallback = resampleDoneCallback;
            var worker = new BackgroundWorker {
                WorkerReportsProgress = true
            };
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerAsync(new Tuple<UVoicePart, UProject, IResamplerDriver>(part, project, driver));
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            DocManager.Inst.ExecuteCmd(new ProgressBarNotification(e.ProgressPercentage, (string)e.UserState), true);
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e) {
            var args = e.Argument as Tuple<UVoicePart, UProject, IResamplerDriver>;
            var part = args.Item1;
            var project = args.Item2;
            var engine = args.Item3;
            e.Result = RenderAsync(part, project, engine, sender as BackgroundWorker);
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            try {
                var renderItems = e.Result as List<RenderItem>;
                var renderItemSampleProviders = new List<RenderItemSampleProvider>();
                foreach (var item in renderItems) {
                    renderItemSampleProviders.Add(new RenderItemSampleProvider(item));
                }
                DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, string.Format(string.Empty)));
                resampleDoneCallback(new SequencingSampleProvider(renderItemSampleProviders));
            } catch (Exception ex) {
                Log.Error(ex, "Error while resampling.");
                resampleDoneCallback(null);
            }
        }

        private List<RenderItem> RenderAsync(UVoicePart part, UProject project, IResamplerDriver engine, BackgroundWorker worker) {
            var renderItems = new List<RenderItem>();
            var watch = new Stopwatch();
            watch.Start();
            Log.Information("Resampling start.");
            lock (part) {
                var cacheDir = PathManager.Inst.GetCachePath(project.FilePath);
                var cacheFiles = Directory.EnumerateFiles(cacheDir).ToArray();
                int count = 0, i = 0;
                foreach (var note in part.Notes) {
                    foreach (var phoneme in note.Phonemes) {
                        count++;
                    }
                }

                foreach (var note in part.Notes) {
                    foreach (var phoneme in note.Phonemes) {
                        if (string.IsNullOrEmpty(phoneme.Oto.File)) {
                            Log.Warning($"Cannot find phoneme in note {note.Lyric}");
                            continue;
                        }

                        var item = new RenderItem(phoneme, part, project);
                        PrepareSourceFile(cacheDir, item);

                        Log.Verbose($"Sound {item.HashParameters():x} resampling {item.GetResamplerExeArgs()}");
                        var engineArgs = DriverModels.CreateInputModel(item, 0);
                        var output = engine.DoResampler(engineArgs);
                        item.Sound = MemorySampleProvider.FromStream(output);
                        output.Dispose();
                        renderItems.Add(item);
                        worker.ReportProgress(100 * ++i / count, $"Resampling \"{phoneme.Phoneme}\" {i}/{count}");
                    }
                }
            }
            watch.Stop();
            Log.Information($"Resampling end, total time {watch.Elapsed}");
            return renderItems;
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;

using OpenUtau.Core.USTx;
using OpenUtau.Core.ResamplerDriver;

namespace OpenUtau.Core.Render
{
    class ResamplerInterface
    {
        Action<SequencingSampleProvider> resampleDoneCallback;

        public void ResamplePart(UVoicePart part, UProject project, IResamplerDriver engine, Action<SequencingSampleProvider> resampleDoneCallback)
        {
            this.resampleDoneCallback = resampleDoneCallback;
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerAsync(new Tuple<UVoicePart, UProject, IResamplerDriver>(part, project, engine));
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            DocManager.Inst.ExecuteCmd(new ProgressBarNotification(e.ProgressPercentage, (string)e.UserState), true);
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as Tuple<UVoicePart, UProject, IResamplerDriver>;
            var part = args.Item1;
            var project = args.Item2;
            var engine = args.Item3;
            e.Result = RenderAsync(part, project, engine, sender as BackgroundWorker);
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            List<RenderItem> renderItems = e.Result as List<RenderItem>;
            List<RenderItemSampleProvider> renderItemSampleProviders = new List<RenderItemSampleProvider>();
            foreach (var item in renderItems) renderItemSampleProviders.Add(new RenderItemSampleProvider(item));
            DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, string.Format(string.Empty)));
            resampleDoneCallback(new SequencingSampleProvider(renderItemSampleProviders));
        }

        private List<RenderItem> RenderAsync(UVoicePart part, UProject project, IResamplerDriver engine, BackgroundWorker worker)
        {
            List<RenderItem> renderItems = new List<RenderItem>();
            System.Diagnostics.Stopwatch watch = new Stopwatch();
            watch.Start();
            System.Diagnostics.Debug.WriteLine("Resampling start");
            lock (part)
            {
                string cacheDir = PathManager.Inst.GetCachePath(project.FilePath);
                string[] cacheFiles = Directory.EnumerateFiles(cacheDir).ToArray();
                int count = 0, i = 0;
                foreach (UNote note in part.Notes) foreach (UPhoneme phoneme in note.Phonemes) count++;

                foreach (UNote note in part.Notes)
                {
                    foreach (UPhoneme phoneme in note.Phonemes)
                    {
                        if (string.IsNullOrEmpty(phoneme.Oto.File))
                        {
                            Debug.WriteLine($"[Error] Cannot find phoneme in note {note.Lyric}");
                            continue;
                        }

                        RenderItem item = BuildRenderItem(phoneme, part, project);
                        var sound = RenderCache.Inst.Get(item.HashParameters());

                        if (sound == null)
                        {
                            string cachefile = Path.Combine(cacheDir, $"{item.HashParameters():x}.wav");
                            if (!cacheFiles.Contains(cachefile))
                            {
                                // System.Diagnostics.Debug.WriteLine("Sound {0:x} resampling {1}", item.HashParameters(), item.GetResamplerExeArgs());
                                DriverModels.EngineInput engineArgs = DriverModels.CreateInputModel(item, 0);
                                System.IO.Stream output = engine.DoResampler(engineArgs);
                                sound = new CachedSound(output);
                            }
                            else
                            {
                                // System.Diagnostics.Debug.WriteLine("Sound {0:x} found on disk {1}", item.HashParameters(), item.GetResamplerExeArgs());
                                sound = new CachedSound(cachefile);
                            }
                            RenderCache.Inst.Put(item.HashParameters(), sound, engine.GetInfo().ToString());
                        }
                        else
                        {
                            // System.Diagnostics.Debug.WriteLine("Sound {0} found in cache {1}", item.HashParameters(), item.GetResamplerExeArgs());
                        }

                        item.Sound = sound;
                        renderItems.Add(item);
                        worker.ReportProgress(100 * ++i / count, $"Resampling \"{phoneme.Phoneme}\" {i}/{count}");
                    }
                }
            }
            watch.Stop();
            System.Diagnostics.Debug.WriteLine("Resampling end");
            System.Diagnostics.Debug.WriteLine("Total cache size {0:n0} bytes", RenderCache.Inst.TotalMemSize);
            System.Diagnostics.Debug.WriteLine("Total time {0} ms", watch.Elapsed.TotalMilliseconds);
            return renderItems;
        }

        private RenderItem BuildRenderItem(UPhoneme phoneme, UVoicePart part, UProject project)
        {
            USinger singer = project.Tracks[part.TrackNo].Singer;
            string rawfile = Lib.EncodingUtil.ConvertEncoding(singer.FileEncoding, singer.PathEncoding, phoneme.Oto.File);
            rawfile = Path.Combine(singer.Path, rawfile);

            double strechRatio = Math.Pow(2, 1.0 - (double)(int)phoneme.Parent.Expressions["velocity"].Data / 100);
            double length = phoneme.Oto.Preutter * strechRatio + phoneme.Envelope.Points[4].X;
            double requiredLength = Math.Ceiling(length / 50 + 1) * 50;
            double lengthAdjustment = phoneme.TailIntrude == 0 ? phoneme.Preutter : phoneme.Preutter - phoneme.TailIntrude + phoneme.TailOverlap;

            RenderItem item = new RenderItem()
            {
                // For resampler
                RawFile = rawfile,
                NoteNum = phoneme.Parent.NoteNum,
                Velocity = (int)phoneme.Parent.Expressions["velocity"].Data,
                Volume = (int)phoneme.Parent.Expressions["volume"].Data,
                StrFlags = phoneme.Parent.GetResamplerFlags(),
                PitchData = BuildPitchData(phoneme, part, project),
                RequiredLength = (int)requiredLength,
                Oto = phoneme.Oto,
                Tempo = project.BPM,

                // For connector
                SkipOver = phoneme.Oto.Preutter * strechRatio - phoneme.Preutter,
                PosMs = project.TickToMillisecond(part.PosTick + phoneme.Parent.PosTick + phoneme.PosTick) - phoneme.Preutter,
                DurMs = project.TickToMillisecond(phoneme.DurTick) + lengthAdjustment,
                Envelope = phoneme.Envelope.Points
            };

            return item;
        }

        private List<int> BuildPitchData(UPhoneme phoneme, UVoicePart part, UProject project)
        {
            List<int> pitches = new List<int>();
            UNote lastNote = part.Notes.OrderByDescending(x => x).Where(x => x.CompareTo(phoneme.Parent) < 0).FirstOrDefault();
            UNote nextNote = part.Notes.Where(x => x.CompareTo(phoneme.Parent) > 0).FirstOrDefault();
            // Get relevant pitch points
            List<PitchPoint> pps = new List<PitchPoint>();

            bool lastNoteInvolved = lastNote != null && phoneme.Overlapped;
            bool nextNoteInvolved = nextNote != null && nextNote.Phonemes[0].Overlapped;

            double lastVibratoStartMs = 0;
            double lastVibratoEndMs = 0;
            double vibratoStartMs = 0;
            double vibratoEndMs = 0;

            if (lastNoteInvolved)
            {
                double offsetMs = DocManager.Inst.Project.TickToMillisecond(phoneme.Parent.PosTick - lastNote.PosTick);
                foreach (PitchPoint pp in lastNote.PitchBend.Points)
                {
                    var newpp = pp.Clone();
                    newpp.X -= offsetMs;
                    newpp.Y -= (phoneme.Parent.NoteNum - lastNote.NoteNum) * 10;
                    pps.Add(newpp);
                }
                if (lastNote.Vibrato.Depth != 0)
                {
                    lastVibratoStartMs = -DocManager.Inst.Project.TickToMillisecond(lastNote.DurTick) * lastNote.Vibrato.Length / 100;
                    lastVibratoEndMs = 0;
                }
            }

            foreach (PitchPoint pp in phoneme.Parent.PitchBend.Points) pps.Add(pp);
            if (phoneme.Parent.Vibrato.Depth != 0)
            {
                vibratoEndMs = DocManager.Inst.Project.TickToMillisecond(phoneme.Parent.DurTick);
                vibratoStartMs = vibratoEndMs * (1 - phoneme.Parent.Vibrato.Length / 100);
            }

            if (nextNoteInvolved)
            {
                double offsetMs = DocManager.Inst.Project.TickToMillisecond(phoneme.Parent.PosTick - nextNote.PosTick);
                foreach (PitchPoint pp in nextNote.PitchBend.Points)
                {
                    var newpp = pp.Clone();
                    newpp.X -= offsetMs;
                    newpp.Y -= (phoneme.Parent.NoteNum - nextNote.NoteNum) * 10;
                    pps.Add(newpp);
                }
            }

            double startMs = DocManager.Inst.Project.TickToMillisecond(phoneme.PosTick) - phoneme.Oto.Preutter;
            double endMs = DocManager.Inst.Project.TickToMillisecond(phoneme.DurTick) -
                (nextNote != null && nextNote.Phonemes[0].Overlapped ? nextNote.Phonemes[0].Preutter - nextNote.Phonemes[0].Overlap : 0);
            if (pps.Count > 0)
            {
                if (pps.First().X > startMs) pps.Insert(0, new PitchPoint(startMs, pps.First().Y));
                if (pps.Last().X < endMs) pps.Add(new PitchPoint(endMs, pps.Last().Y));
            }
            else
            {
                throw new Exception("Zero pitch points.");
            }

            // Interpolation
            const int intervalTick = 5;
            double intervalMs = DocManager.Inst.Project.TickToMillisecond(intervalTick);
            double currMs = startMs;
            int i = 0;

            while (currMs < endMs)
            {
                while (pps[i + 1].X < currMs) i++;
                double pit = MusicMath.InterpolateShape(pps[i].X, pps[i + 1].X, pps[i].Y, pps[i + 1].Y, currMs, pps[i].Shape);
                pit *= 10;

                // Apply vibratos
                if (currMs < lastVibratoEndMs && currMs >= lastVibratoStartMs)
                    pit += InterpolateVibrato(lastNote.Vibrato, currMs - lastVibratoStartMs);

                if (currMs < vibratoEndMs && currMs >= vibratoStartMs)
                    pit += InterpolateVibrato(phoneme.Parent.Vibrato, currMs - vibratoStartMs);

                pitches.Add((int)pit);
                currMs += intervalMs;
            }

            return pitches;
        }

        private double InterpolateVibrato(VibratoExpression vibrato, double posMs)
        {
            double lengthMs = vibrato.Length / 100 * DocManager.Inst.Project.TickToMillisecond(vibrato.Parent.DurTick);
            double inMs = lengthMs * vibrato.In / 100;
            double outMs = lengthMs * vibrato.Out / 100;

            double value = -Math.Sin(2 * Math.PI * (posMs / vibrato.Period + vibrato.Shift / 100)) * vibrato.Depth;

            if (posMs < inMs) value *= posMs / inMs;
            else if (posMs > lengthMs - outMs) value *= (lengthMs - posMs) / outMs;

            return value;
        }
    }
}

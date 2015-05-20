using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;

using OpenUtau.Core.USTx;

namespace OpenUtau.Core.Render
{
    class ResamplerInterface
    {
        const string DefaultEnvelope = "0 5 35 0 100 100 0";
        public void RenderAll(UProject project)
        {
            foreach (UPart part in project.Parts)
                if (part is UVoicePart)
                    Start((UVoicePart)part, project);
        }

        public void Start(UVoicePart part, UProject project)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerAsync(new Tuple<UVoicePart, UProject>(part, project));
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            DocManager.Inst.ExecuteCmd(new ProgressBarNotification(e.ProgressPercentage, (string)e.UserState));
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as Tuple<UVoicePart, UProject>;
            var part = args.Item1;
            var project = args.Item2;
            e.Result = RenderAsync(part, project, sender as BackgroundWorker);
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var waveConnector = e.Result as WaveConnector;
            DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, string.Format("")));
            System.Diagnostics.Debug.WriteLine("Render end");
            System.Diagnostics.Debug.WriteLine("Total cache size {0:n0} bytes", RenderCache.Inst.TotalMemSize);

            //waveConnector.WriteToFile("out.wav");
            var waveout = new NAudio.Wave.WaveOut();
            var stream = new NAudio.Wave.SampleProviders.SampleToWaveProvider16(waveConnector.GetMixingSampleProvider());
            waveout.Init(stream);
            waveout.Play();
        }

        public WaveConnector RenderAsync(UVoicePart part, UProject project, BackgroundWorker worker)
        {
            WaveConnector waveConnector = new WaveConnector();
            System.Diagnostics.Debug.WriteLine("Render start");
            lock (part)
            {
                string cache_dir = PathManager.Inst.GetCachePath(project.FilePath);

                int count = 0, i = 0;
                foreach (UNote note in part.Notes) foreach (UPhoneme phoneme in note.Phonemes) count++;

                foreach (UNote note in part.Notes)
                {
                    foreach (UPhoneme phoneme in note.Phonemes)
                    {
                        RenderItem item = BuildRenderItem(phoneme, part, project);
                        var sound = RenderCache.Inst.Get(item.HashParameters());

                        if (sound == null)
                        {
                            string cachefile = Path.Combine(cache_dir, string.Format("{0:x}.wav", item.HashParameters()));
                            if (!File.Exists(cachefile))
                            {
                                System.Diagnostics.Debug.WriteLine("Sound {0:x} not found in cache, resampling {1}", item.HashParameters(), item.GetResamplerExeArgs());
                                ProcessStartInfo pinfo = new ProcessStartInfo(
                                    PathManager.Inst.GetTool1Path(),
                                    string.Format("{0} {1} {2}", item.RawFile, cachefile, item.GetResamplerExeArgs()));
                                pinfo.CreateNoWindow = true;
                                pinfo.UseShellExecute = false;
                                var p = Process.Start(pinfo);
                                p.WaitForExit();
                            }
                            else System.Diagnostics.Debug.WriteLine("Sound {0:x} found on disk {1}", item.HashParameters(), item.GetResamplerExeArgs());
                            sound = new CachedSound(cachefile);
                            RenderCache.Inst.Put(item.HashParameters(), sound);
                        }
                        else System.Diagnostics.Debug.WriteLine("Sound {0} found in cache {1}", item.HashParameters(), item.GetResamplerExeArgs());

                        item.Sound = sound;
                        waveConnector.RenderItems.Add(item);
                        worker.ReportProgress(100 * ++i / count, string.Format("Rendering \"{0}\" {1}/{2}", phoneme.Phoneme, i, count));
                    }
                }
            }
            return waveConnector;
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
                B64Pitch = BuildPitchArgs(phoneme, part, project),
                RequiredLength = (int)requiredLength,
                Oto = phoneme.Oto,

                // For connector
                SkipOver = phoneme.Oto.Preutter * strechRatio - phoneme.Preutter,
                PosMs = project.TickToMillisecond(part.PosTick + phoneme.Parent.PosTick + phoneme.PosTick) - phoneme.Preutter,
                DurMs = project.TickToMillisecond(phoneme.DurTick) + lengthAdjustment,
                Envelope = phoneme.Envelope.Points
            };

            return item;
        }

        private string BuildPitchArgs(UPhoneme phoneme, UVoicePart part, UProject project)
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
            if (phoneme.Parent.Vibrato.Depth !=0)
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
                double pit = pps[i].Shape == PitchPointShape.i ? MusicMath.SinEasingIn(pps[i].X, pps[i + 1].X, pps[i].Y, pps[i + 1].Y, currMs) :
                    pps[i].Shape == PitchPointShape.o ? MusicMath.SinEasingOut(pps[i].X, pps[i + 1].X, pps[i].Y, pps[i + 1].Y, currMs) :
                    pps[i].Shape == PitchPointShape.io ? MusicMath.SinEasingInOut(pps[i].X, pps[i + 1].X, pps[i].Y, pps[i + 1].Y, currMs) :
                    MusicMath.Liner(pps[i].X, pps[i + 1].X, pps[i].Y, pps[i + 1].Y, currMs);

                pit *= 10;

                // Apply vibratos
                if (currMs < lastVibratoEndMs && currMs >= lastVibratoStartMs)
                    pit += InterpolateVibrato(lastNote.Vibrato, currMs - lastVibratoStartMs);

                if (currMs < vibratoEndMs && currMs >= vibratoStartMs)
                    pit += InterpolateVibrato(phoneme.Parent.Vibrato, currMs - vibratoStartMs);

                pitches.Add((int)pit);
                currMs += intervalMs;
            }

            return string.Format("!{0} {1}", project.BPM, Base64EncodeInt12(pitches));
        }

        private double InterpolateVibrato(VibratoExpression vibrato, double posMs)
        {
            double lengthMs = vibrato.Length / 100 * DocManager.Inst.Project.TickToMillisecond(vibrato.Parent.DurTick);
            double inMs = lengthMs * vibrato.In / 100;
            double outMs = lengthMs * vibrato.Out / 100;

            double value = -Math.Sin(2 * Math.PI * (posMs / vibrato.Period + vibrato.Shift / 100)) *vibrato.Depth;

            if (posMs < inMs) value *= posMs / inMs;
            else if (posMs > lengthMs - outMs) value *= (lengthMs - posMs) / outMs;

            return value;
        }

        private const string intToBase64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

        private string Base64EncodeInt12(int data)
        {
            if (data < 0) data += 4096;
            char[] base64 = new char[2];
            base64[0] = intToBase64[(data >> 6) & 0x003F];
            base64[1] = intToBase64[data & 0x003F];
            return new String(base64);
        }

        private string Base64EncodeInt12(List<int> data)
        {
            List<string> l = new List<string>();
            foreach (int d in data) l.Add(Base64EncodeInt12(d));
            StringBuilder base64 = new StringBuilder();
            string last = "";
            int dups = 0;
            foreach (string b in l)
            {
                if (last == b) dups++;
                else if (dups == 0) base64.Append(b);
                else
                {
                    base64.Append('#');
                    base64.Append(dups + 1);
                    base64.Append('#');
                    dups = 0;
                    base64.Append(b);
                }
                last = b;
            }
            if (dups != 0)
            {
                base64.Append('#');
                base64.Append(dups + 1);
                base64.Append('#');
            }
            return base64.ToString();
        }
    }
}

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

        public RenderEngine(UProject project, IResamplerDriver driver, RenderCache cache, int startTick = 0) {
            this.project = project;
            this.driver = driver;
            this.cache = cache;
            this.startTick = startTick;
        }

        public Tuple<MasterAdapter, List<Fader>, CancellationTokenSource> RenderProject(int startTick) {
            var cancellation = new CancellationTokenSource();
            var faders = new List<Fader>();
            var renderer = new Classic.ClassicRenderer();
            foreach (var track in project.tracks) {
                var phrases = PrepareTrack(track, project);
                var progress = new Progress(phrases.Sum(phrase => phrase.phones.Length + 1));
                var vocal = new WaveMix(phrases.Select(phrase => {
                    var task = renderer.Render(phrase, cancellation);
                    task.Wait();
                    return task.Result;
                }));
                var sources = project.parts
                     .Where(part => part is UWavePart && part.trackNo == track.TrackNo)
                     .Select(part => part as UWavePart)
                     .Where(part => part.Samples != null)
                     .Select(part => {
                         var waveSource = new WaveSource(
                             project.TickToMillisecond(part.position),
                             project.TickToMillisecond(part.Duration),
                             null,
                             part.skipMs, part.channels);
                         if (part.Samples != null) {
                             waveSource.SetSamples(part.Samples);
                         } else {
                             waveSource.SetSamples(new float[0]);
                         }
                         return (ISignalSource)waveSource;
                     }).ToList();
                sources.Insert(0, vocal);
                var fader = new Fader(new WaveMix(sources));
                fader.Scale = PlaybackManager.DecibelToVolume(track.Mute ? -24 : track.Volume);
                fader.SetScaleToTarget();
                faders.Add(fader);
            }
            var master = new MasterAdapter(new WaveMix(faders));
            master.SetPosition((int)(project.TickToMillisecond(startTick) * 44100 / 1000) * 2);
            return Tuple.Create(master, faders, cancellation);
        }

        public List<WaveMix> RenderTracks() {
            var cancellation = new CancellationTokenSource();
            var result = new List<WaveMix>();
            var renderer = new Classic.ClassicRenderer();
            foreach (var track in project.tracks) {
                var phrases = PrepareTrack(track, project);
                var progress = new Progress(phrases.Sum(phrase => phrase.phones.Length + 1));
                var mix = new WaveMix(phrases.Select(phrase => {
                    var task = renderer.Render(phrase, cancellation);
                    task.Wait();
                    return task.Result;
                }));
                progress.Clear();
                result.Add(mix);
            }
            return result;
        }

        public CancellationTokenSource PreRenderProject() {
            int threads = Util.Preferences.Default.PrerenderThreads;
            var cancellation = new CancellationTokenSource();
            Task.Run(() => {
                try {
                    Thread.Sleep(200);
                    if (cancellation.Token.IsCancellationRequested) {
                        return;
                    }
                    RenderPhrase[] phrases;
                    lock (project) {
                        phrases = PrepareProject(project).ToArray();
                    }
                    phrases = phrases.Where(phrase => {
                        var last = phrase.phones.Last();
                        var endPos = phrase.position + last.position + last.duration;
                        return startTick < endPos;
                    }).ToArray();
                    if (phrases.Length == 0) {
                        return;
                    }
                    var progress = new Progress(phrases.Sum(phrase => phrase.phones.Length) + phrases.Length);
                    var renderer = new Classic.ClassicRenderer();
                    foreach (var phrase in phrases) {
                        var task = renderer.Render(phrase, cancellation);
                        task.Wait();
                        var samples = task.Result;
                    }
                    progress.Clear();
                } catch (Exception e) {
                    if (!cancellation.IsCancellationRequested) {
                        Log.Error(e, "Failed to pre-render.");
                    }
                }
            });
            return cancellation;
        }

        IEnumerable<RenderPhrase> PrepareProject(UProject project) {
            return project.tracks
                .SelectMany(track => PrepareTrack(track, project));
        }

        IEnumerable<RenderPhrase> PrepareTrack(UTrack track, UProject project) {
            return project.parts
                .Where(part => part.trackNo == track.TrackNo)
                .Where(part => part is UVoicePart)
                .Select(part => part as UVoicePart)
                .SelectMany(part => RenderPhrase.FromPart(project, project.tracks[part.trackNo], part));
        }

        public static void ReleaseSourceTemp() {
            Classic.VoicebankFiles.ReleaseSourceTemp();
        }
    }
}

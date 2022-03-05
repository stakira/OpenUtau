using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenUtau.Core.SignalChain;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Core.Render {
    public class Progress {
        readonly int total;
        int completed = 0;
        public Progress(int total) {
            this.total = total;
        }

        public void CompleteOne(string info) {
            Interlocked.Increment(ref completed);
            DocManager.Inst.ExecuteCmd(new ProgressBarNotification(completed * 100.0 / total, info));
        }

        public void Clear() {
            DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, string.Empty));
        }
    }

    class RenderEngine {
        readonly UProject project;
        readonly int startTick;

        public RenderEngine(UProject project, int startTick = 0) {
            this.project = project;
            this.startTick = startTick;
        }

        public Tuple<MasterAdapter, List<Fader>, CancellationTokenSource> RenderProject(int startTick) {
            var cancellation = new CancellationTokenSource();
            var faders = new List<Fader>();
            var renderer = new Classic.ClassicRenderer();
            var renderTasks = new List<Tuple<RenderPhrase, WaveSource>>();
            int totalProgress = 0;
            foreach (var track in project.tracks) {
                var phrases = PrepareTrack(track, project);
                var vocal = new WaveMix(phrases.Select(phrase => {
                    var firstPhone = phrase.phones.First();
                    var lastPhone = phrase.phones.Last();
                    double posMs = (phrase.position + firstPhone.position) * phrase.tickToMs - firstPhone.preutterMs;
                    double durMs = (lastPhone.duration + lastPhone.position - firstPhone.position) * phrase.tickToMs;
                    if (posMs + durMs < startTick * phrase.tickToMs) {
                        return null;
                    }
                    var source = new WaveSource(posMs, durMs, null, 0, 1);
                    renderTasks.Add(Tuple.Create(phrase, source));
                    totalProgress += phrase.phones.Length;
                    return source;
                }).OfType<WaveSource>());
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
            var task = Task.Run(() => {
                var progress = new Progress(totalProgress);
                foreach (var renderTask in renderTasks) {
                    if (cancellation.IsCancellationRequested) {
                        break;
                    }
                    var task = renderer.Render(renderTask.Item1, progress, cancellation);
                    task.Wait();
                    renderTask.Item2.SetSamples(task.Result.samples);
                }
                progress.Clear();
            });
            return Tuple.Create(master, faders, cancellation);
        }

        public List<WaveMix> RenderTracks() {
            var cancellation = new CancellationTokenSource();
            var result = new List<WaveMix>();
            var renderer = new Classic.ClassicRenderer();
            foreach (var track in project.tracks) {
                var phrases = PrepareTrack(track, project);
                var progress = new Progress(phrases.Sum(phrase => phrase.phones.Length));
                var mix = new WaveMix(phrases.Select(phrase => {
                    var task = renderer.Render(phrase, progress, cancellation);
                    task.Wait();
                    float durMs = task.Result.samples.Length * 1000f / 44100f;
                    var source = new WaveSource(task.Result.positionMs - task.Result.leadingMs, durMs, null, 0, 1);
                    source.SetSamples(task.Result.samples);
                    return source;
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
                    var progress = new Progress(phrases.Sum(phrase => phrase.phones.Length));
                    var renderer = new Classic.ClassicRenderer();
                    foreach (var phrase in phrases) {
                        var task = renderer.Render(phrase, progress, cancellation);
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

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

        public void Complete(int n, string info) {
            Interlocked.Add(ref completed, n);
            Notify(completed * 100.0 / total, info);
        }

        public void Clear() {
            Notify(0, string.Empty);
        }

        private void Notify(double progress, string info) {
            var notif = new ProgressBarNotification(progress, info);
            var task = new Task(() => DocManager.Inst.ExecuteCmd(notif));
            task.Start(DocManager.Inst.MainScheduler);
        }
    }

    class RenderPartRequest {
        public UVoicePart part;
        public long timestamp;
        public int trackNo;
        public RenderPhrase[] phrases;
        public WaveSource[] sources;
        public WaveMix mix;
    }

    class RenderEngine {
        readonly UProject project;
        readonly int startTick;

        public RenderEngine(UProject project, int startTick = 0) {
            this.project = project;
            this.startTick = startTick;
        }

        public Tuple<MasterAdapter, List<Fader>> RenderProject(int startTick, TaskScheduler uiScheduler, ref CancellationTokenSource cancellation) {
            var newCancellation = new CancellationTokenSource();
            var oldCancellation = Interlocked.Exchange(ref cancellation, newCancellation);
            if (oldCancellation != null) {
                oldCancellation.Cancel();
                oldCancellation.Dispose();
            }
            double startMs = project.timeAxis.TickPosToMsPos(startTick);
            var faders = new List<Fader>();
            var requests = PrepareRequests()
                .Where(request => request.sources.Length > 0 && request.sources.Max(s => s.EndMs) > startMs)
                .ToArray();
            for (int i = 0; i < project.tracks.Count; ++i) {
                var track = project.tracks[i];
                var trackRequests = requests
                    .Where(req => req.trackNo == i)
                    .ToArray();
                var trackSources = trackRequests.Select(req => req.mix)
                    .OfType<ISignalSource>()
                    .ToList();
                trackSources.AddRange(project.parts
                    .Where(part => part is UWavePart && part.trackNo == i)
                    .Select(part => part as UWavePart)
                    .Where(part => part.Samples != null)
                    .Select(part => {
                        double offsetMs = project.timeAxis.TickPosToMsPos(part.position);
                        double estimatedLengthMs = project.timeAxis.TickPosToMsPos(part.End) - offsetMs;
                        var waveSource = new WaveSource(
                            offsetMs,
                            estimatedLengthMs,
                            part.skipMs, part.channels);
                        waveSource.SetSamples(part.Samples);
                        return (ISignalSource)waveSource;
                    }));
                var trackMix = new WaveMix(trackSources);
                var fader = new Fader(trackMix);
                fader.Scale = PlaybackManager.DecibelToVolume(track.Mute ? -24 : track.Volume);
                fader.SetScaleToTarget();
                faders.Add(fader);
            }
            Task.Run(() => {
                RenderRequests(requests, uiScheduler, newCancellation, playing: true);
            }).ContinueWith(task => {
                if (task.IsFaulted) {
                    Log.Error(task.Exception, "Failed to render.");
                    DocManager.Inst.ExecuteCmd(new UserMessageNotification(task.Exception.Flatten().Message));
                    throw task.Exception;
                }
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, uiScheduler);
            var master = new MasterAdapter(new WaveMix(faders));
            master.SetPosition((int)(startMs * 44100 / 1000) * 2);
            return Tuple.Create(master, faders);
        }

        public List<WaveMix> RenderTracks(TaskScheduler uiScheduler, ref CancellationTokenSource cancellation) {
            var newCancellation = new CancellationTokenSource();
            var oldCancellation = Interlocked.Exchange(ref cancellation, newCancellation);
            if (oldCancellation != null) {
                oldCancellation.Cancel();
                oldCancellation.Dispose();
            }
            var trackMixes = new List<WaveMix>();
            var requests = PrepareRequests();
            if (requests.Length == 0) {
                return trackMixes;
            }
            Enumerable.Range(0, requests.Max(req => req.trackNo) + 1)
                .Select(trackNo => requests.Where(req => req.trackNo == trackNo).ToArray())
                .ToList()
                .ForEach(trackRequests => {
                    if (trackRequests.Length == 0) {
                        trackMixes.Add(null);
                    } else {
                        RenderRequests(trackRequests, uiScheduler, newCancellation);
                        var mix = new WaveMix(trackRequests.Select(req => req.mix).ToArray());
                        trackMixes.Add(mix);
                    }
                });
            return trackMixes;
        }

        public void PreRenderProject(TaskScheduler uiScheduler, ref CancellationTokenSource cancellation) {
            var newCancellation = new CancellationTokenSource();
            var oldCancellation = Interlocked.Exchange(ref cancellation, newCancellation);
            if (oldCancellation != null) {
                oldCancellation.Cancel();
                oldCancellation.Dispose();
            }
            Task.Run(() => {
                try {
                    Thread.Sleep(200);
                    if (newCancellation.Token.IsCancellationRequested) {
                        return;
                    }
                    RenderRequests(PrepareRequests(), uiScheduler, newCancellation);
                } catch (Exception e) {
                    if (!newCancellation.IsCancellationRequested) {
                        Log.Error(e, "Failed to pre-render.");
                    }
                }
            });
        }

        private RenderPartRequest[] PrepareRequests() {
            RenderPartRequest[] requests;
            lock (project) {
                requests = project.parts
                    .Where(part => part is UVoicePart)
                    .Select(part => part as UVoicePart)
                    .Select(part => part.GetRenderRequest())
                    .Where(request => request != null)
                    .ToArray();
            }
            foreach (var request in requests) {
                request.sources = new WaveSource[request.phrases.Length];
                for (var i = 0; i < request.phrases.Length; i++) {
                    var phrase = request.phrases[i];
                    var firstPhone = phrase.phones.First();
                    var lastPhone = phrase.phones.Last();
                    var layout = phrase.renderer.Layout(phrase);
                    double posMs = layout.positionMs - layout.leadingMs;
                    double durMs = layout.estimatedLengthMs;
                    request.sources[i] = new WaveSource(posMs, durMs, 0, 1);
                }
                request.mix = new WaveMix(request.sources);
            }
            return requests;
        }

        private void RenderRequests(
            RenderPartRequest[] requests,
            TaskScheduler uiScheduler,
            CancellationTokenSource cancellation,
            bool playing = false) {
            if (requests.Length == 0 || cancellation.IsCancellationRequested) {
                return;
            }
            var tuples = requests
                .SelectMany(req => req.phrases
                    .Zip(req.sources, (phrase, source) => Tuple.Create(phrase, source, req)))
                .ToArray();
            if (playing) {
                var orderedTuples = tuples
                    .Where(tuple => tuple.Item1.end > startTick)
                    .OrderBy(tuple => tuple.Item1.end)
                    .Concat(tuples.Where(tuple => tuple.Item1.end <= startTick))
                    .ToArray();
                tuples = orderedTuples;
            }
            var progress = new Progress(tuples.Sum(t => t.Item1.phones.Length));
            foreach (var tuple in tuples) {
                var phrase = tuple.Item1;
                var source = tuple.Item2;
                var request = tuple.Item3;
                var task = phrase.renderer.Render(phrase, progress, cancellation, true);
                task.Wait();
                if (cancellation.IsCancellationRequested) {
                    break;
                }
                source.SetSamples(task.Result.samples);
                if (request.sources.All(s => s.HasSamples)) {
                    request.part.SetMix(request.mix);
                    new Task(() => DocManager.Inst.ExecuteCmd(new PartRenderedNotification(request.part)))
                        .Start(uiScheduler);
                }
            }
            progress.Clear();
        }

        public static void ReleaseSourceTemp() {
            Classic.VoicebankFiles.Inst.ReleaseSourceTemp();
        }
    }
}

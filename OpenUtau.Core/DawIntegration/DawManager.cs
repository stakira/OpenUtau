using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using K4os.Hash.xxHash;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Core.DawIntegration {
    public class DawManager : SingletonBase<DawManager>, ICmdSubscriber {
        public DawClient? dawClient = null;
        CancellationTokenSource? renderCancellation = null;
        private Debounce sendLayoutDebounce = new Debounce();
        private Debounce sendAudioDebounce = new Debounce();

        private DawManager() {
            DocManager.Inst.AddSubscriber(this);
        }

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is UNotification && !(
                cmd is DawConnectedNotification ||
                cmd is PartRenderedNotification ||
                cmd is VolumeChangeNotification ||
                cmd is PanChangeNotification
                )) {
                return;
            }

            sendLayoutDebounce.Do(TimeSpan.FromSeconds(1), async () => {
                await UpdateUstx();
                await UpdateTracks();
            });
            sendAudioDebounce.Do(TimeSpan.FromSeconds(1), async () => {
                await UpdateAudio();
            });
        }

        public async Task Disconnect() {
            if (this.dawClient == null) {
                return;
            }
            await UpdateUstx();
            await UpdateTracks();
            await UpdateAudio();

            var dawClient = this.dawClient;
            this.dawClient = null;
            dawClient.Disconnect();
        }

        internal bool isDawClientLocked = false;

        private async Task UpdateUstx() {
            if (dawClient == null) {
                return;
            }

            Log.Information("Updating ustx in DAW...");

            try {
                var ustx = Format.Ustx.FromProject(DocManager.Inst.Project);
                await dawClient.SendNotification(
                    new UpdateUstxNotification(ustx)
                );
                Log.Information("Sent ustx to DAW.");
            } catch (Exception e) {
                Log.Error(e, "Failed to send ustx to DAW.");
            }
        }
        private async Task UpdateTracks() {
            if (dawClient == null) {
                return;
            }

            Log.Information("Updating tracks in DAW...");

            try {
                await dawClient.SendNotification(
                    new UpdateTracksNotification(
                            DocManager.Inst.Project.tracks.Select(track => new UpdateTracksNotification.Track(
                                track.TrackName,
                                track.Volume,
                                track.Pan
                            )).ToList()
                        )
                );
                Log.Information("Sent tracks to DAW.");
            } catch (Exception e) {
                Log.Error(e, "Failed to send tracks to DAW.");
            }
        }


        private async Task UpdateAudio() {
            if (dawClient == null) {
                return;
            }
            try {
                var readyParts = DocManager.Inst.Project.parts.Where(part => part is UVoicePart uPart && uPart.Mix != null)
                    .Select(part => (part as UVoicePart)!)
                    .ToList();

                Log.Information("Rendering prerenders for DAW...");
                var hashToAudioPart = new Dictionary<int, byte[]>();
                var buffers = readyParts.Select(part => {
                    double startMs = DocManager.Inst.Project.timeAxis.TickPosToMsPos(part.position);
                    double endMs = DocManager.Inst.Project.timeAxis.TickPosToMsPos(part.position + part.duration);
                    int samplePos = (int)(startMs * 44100 / 1000) * 2;
                    int sampleCount = (int)((endMs - startMs) * 44100 / 1000) * 2;

                    // TODO: memoize this
                    var floatBuffer = new float[sampleCount];
                    part.Mix.Mix(samplePos, floatBuffer, 0, sampleCount);
                    var byteBuffer = new byte[floatBuffer.Length * 4];
                    Buffer.BlockCopy(floatBuffer, 0, byteBuffer, 0, byteBuffer.Length);
                    int hash = unchecked((int)XXH32.DigestOf(byteBuffer));

                    hashToAudioPart[hash] = byteBuffer;

                    return (part, startMs, endMs, hash);
                });
                Log.Information("Sending part layout to DAW...");
                var missingAudios = await dawClient.SendRequest<UpdatePartLayoutResponse>(
                    new UpdatePartLayoutRequest(
                        buffers.Select(buffer => new UpdatePartLayoutRequest.Part(
                            buffer.part.trackNo,
                            buffer.startMs,
                            buffer.endMs,
                            buffer.hash
                        )).ToList()
                    )
                );
                Log.Information("Sent part layout to DAW.");

                if (missingAudios.missingAudios.Count > 0) {
                    Log.Information($"DAW requested {missingAudios.missingAudios.Count} missing audios.");
                    var buffersDict = buffers.GroupBy(buffer => buffer.hash).ToDictionary(group => group.Key, group => group.First());
                    var audios = new Dictionary<int, string>();
                    foreach (var audioHash in missingAudios.missingAudios) {
                        if (!hashToAudioPart.ContainsKey(audioHash)) {
                            Log.Warning($"DAW requested missing audio {audioHash}, but it is not in the project.");
                            continue;
                        }

                        var byteBuffer = hashToAudioPart[audioHash];
                        var compressed = Zstd.Compress(byteBuffer);

                        audios[audioHash] = Convert.ToBase64String(compressed);
                    }

                    await dawClient.SendNotification(
                        new UpdateAudioNotification(audios)
                    );
                    Log.Information("Sent missing audios to DAW.");
                } else {
                    Log.Information("Audios in DAW are up to date.");
                }
            } catch (Exception e) {
                Log.Error(e, "Failed to send status to DAW.");
            }
        }
    }
}

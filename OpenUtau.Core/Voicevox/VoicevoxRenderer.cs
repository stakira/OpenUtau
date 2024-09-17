using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using K4os.Hash.xxHash;
using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenUtau.Core.Format;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;
using Serilog;
using static OpenUtau.Api.Phonemizer;

/*
 * This source code is partially based on the VOICEVOX engine.
 * https://github.com/VOICEVOX/voicevox_engine/blob/master/LGPL_LICENSE
 */

namespace OpenUtau.Core.Voicevox {
    public class VoicevoxRenderer : IRenderer {
        const string VOLC = VoicevoxUtils.VOLC;
        const string PITD = Format.Ustx.PITD;

        static readonly HashSet<string> supportedExp = new HashSet<string>(){
            Format.Ustx.DYN,
            PITD,
            Format.Ustx.CLR,
            Format.Ustx.VOL,
            VOLC,
            //Format.Ustx.SHFC,
            Format.Ustx.SHFT
        };

        static readonly object lockObj = new object();

        public USingerType SingerType => USingerType.Voicevox;

        public bool SupportsRenderPitch => true;

        public bool SupportsExpression(UExpressionDescriptor descriptor) {
            return supportedExp.Contains(descriptor.abbr);
        }

        public RenderResult Layout(RenderPhrase phrase) {
            return new RenderResult() {
                leadingMs = phrase.leadingMs,
                positionMs = phrase.positionMs - ((VoicevoxUtils.headS * 1000) + 10),
                estimatedLengthMs = phrase.durationMs + phrase.leadingMs,
            };
        }

        public Task<RenderResult> Render(RenderPhrase phrase, Progress progress, int trackNo, CancellationTokenSource cancellation, bool isPreRender) {
            var task = Task.Run(() => {
                lock (lockObj) {
                    if (cancellation.IsCancellationRequested) {
                        return new RenderResult();
                    }
                    string progressInfo = $"Track {trackNo + 1}: {this} \"{string.Join(" ", phrase.phones.Select(p => p.phoneme))}\"";
                    progress.Complete(0, progressInfo);
                    ulong hash = HashPhraseGroups(phrase);
                    var wavPath = Path.Join(PathManager.Inst.CachePath, $"vv-{phrase.hash:x16}-{hash:x16}.wav");
                    var result = Layout(phrase);
                    if (!File.Exists(wavPath)) {
                        var singer = phrase.singer as VoicevoxSinger;
                        if (singer != null) {
                            if (VoicevoxUtils.dic == null) {
                                VoicevoxUtils.Loaddic(singer);
                            }
                            try {
                                Log.Information($"Starting Voicevox synthesis");
                                VoicevoxNote vvNotes = new VoicevoxNote();
                                if (!singer.voicevoxConfig.Tag.Equals("VOICEVOX JA")) {
                                    Note[][] notes = new Note[phrase.phones.Length][];
                                    for (int i = 0; i < phrase.phones.Length; i++) {
                                        notes[i] = new Note[1];
                                        notes[i][0] = new Note() {
                                            lyric = phrase.phones[i].phoneme,
                                            position = phrase.phones[i].position,
                                            duration = phrase.phones[i].duration,
                                            tone = (int)(phrase.phones[i].tone + phrase.phones[0].toneShift)
                                        };
                                    }

                                    var qNotes = VoicevoxUtils.NoteGroupsToVoicevox(notes, phrase.timeAxis);

                                    //Prepare for future additions of Teacher Singer.
                                    string baseSingerID = VoicevoxUtils.getBaseSingerID(singer);
                                    vvNotes = VoicevoxUtils.VoicevoxVoiceBase(qNotes, baseSingerID);
                                    if (vvNotes.phonemes.Count == 0) {

                                    }
                                    int vvTotalFrames = 0;
                                    double frameMs = (1000d / VoicevoxUtils.fps);
                                    vvNotes.phonemes.ForEach(x => vvTotalFrames += x.frame_length);
                                    if (!phrase.phones[0].direct) {
                                        vvNotes.f0 = VoicevoxUtils.SampleCurve(phrase, phrase.pitches, 0, frameMs, vvTotalFrames, vvNotes.phonemes[0].frame_length, vvNotes.phonemes[^1].frame_length, 0, x => MusicMath.ToneToFreq(x * 0.01)).ToList();
                                    } else {
                                        //Compatible with toneShift (key shift), for adjusting the range of tones when synthesizing
                                        vvNotes.f0 = vvNotes.f0.Select(f0 => f0 = f0 * Math.Pow(2, ((phrase.phones[0].toneShift * -1) / 12d))).ToList();
                                    }

                                    //Volume parameter for synthesis. Scheduled to be revised
                                    var volumeCurve = phrase.curves.FirstOrDefault(c => c.Item1 == VOLC);
                                    if (volumeCurve != null) {
                                        var volumes = VoicevoxUtils.SampleCurve(phrase, volumeCurve.Item2, 0, frameMs, vvTotalFrames, vvNotes.phonemes[0].frame_length, vvNotes.phonemes[^1].frame_length, -10, x => x * 0.01);
                                        vvNotes.volume = vvNotes.volume.Select((vol, i) => vol = vol * volumes[i]).ToList();
                                    } else {
                                        vvNotes.volume = vvNotes.volume.Select(vol => vol = vol * phrase.phones[0].volume).ToList();
                                    }
                                    for (int i = 0; i < vvNotes.phonemes[0].frame_length; i++) {
                                        vvNotes.volume[i] = 0;
                                    }
                                    for (int i = vvNotes.volume.Count - vvNotes.phonemes[vvNotes.phonemes.Count - 1].frame_length; i < vvNotes.volume.Count; i++) {
                                        vvNotes.volume[i] = 0;
                                    }
                                } else {
                                    vvNotes = PhraseToVoicevoxNotes(phrase);
                                }
                                if (vvNotes.phonemes.Count() > 0) {
                                    result.positionMs = phrase.positionMs - phrase.timeAxis.TickPosToMsPos((vvNotes.phonemes.First().frame_length / VoicevoxUtils.fps) * 1000d);
                                }

                                int speaker = 0;
                                singer.voicevoxConfig.styles.ForEach(style => {
                                    if (style.name.Equals(phrase.singer.Subbanks[1].Suffix) && style.type.Equals("frame_decode")) {
                                        speaker = style.id;
                                    }
                                    if (style.name.Equals(phrase.phones[0].suffix) && style.type.Equals("frame_decode")) {
                                        speaker = style.id;
                                    } else if ((style.name + "_" + style.type).Equals(phrase.phones[0].suffix)) {
                                        speaker = style.id;
                                    }
                                });
                                var queryurl = new VoicevoxURL() { method = "POST", path = "/frame_synthesis", query = new Dictionary<string, string> { { "speaker", speaker.ToString() } }, body = JsonConvert.SerializeObject(vvNotes), accept = "audio/wav" };
                                var response = VoicevoxClient.Inst.SendRequest(queryurl);
                                byte[] bytes = null;
                                if (!response.Item2.Equals(null)) {
                                    bytes = response.Item2;
                                } else if (!string.IsNullOrEmpty(response.Item1)) {
                                    var jObj = JObject.Parse(response.Item1);
                                    if (jObj.ContainsKey("detail")) {
                                        Log.Error($"Failed to create a voice base. : {jObj}");
                                    }
                                }
                                if (bytes != null) {
                                    File.WriteAllBytes(wavPath, bytes);
                                }
                            } catch (Exception e) {
                                Log.Error($"Failed to create a voice base.:{e}");
                            }
                            if (cancellation.IsCancellationRequested) {
                                return new RenderResult();
                            }
                        }
                    }
                    progress.Complete(phrase.phones.Length, progressInfo);
                    try {
                        if (File.Exists(wavPath)) {
                            using (var waveStream = new WaveFileReader(wavPath)) {

                                result.samples = Wave.GetSamples(waveStream.ToSampleProvider().ToMono(1, 0));
                            }
                            if (result.samples != null) {
                                Renderers.ApplyDynamics(phrase, result);
                            }
                        }
                    } catch (Exception e) {
                        Log.Error(e.Message);
                        result.samples = new float[0];
                    }
                    return result;
                }
            });
            return task;
        }

        //Synthesize with parameters of phoneme, F0, and volume. Under development
        static VoicevoxNote PhraseToVoicevoxNotes(RenderPhrase phrase) {
            VoicevoxNote notes = new VoicevoxNote();

            int headFrames = (int)(VoicevoxUtils.headS * VoicevoxUtils.fps);
            int tailFrames = (int)(VoicevoxUtils.tailS * VoicevoxUtils.fps);

            notes.phonemes.Add(new Phonemes {
                phoneme = "pau",
                frame_length = headFrames
            });
            foreach (var phone in phrase.phones) {
                notes.phonemes.Add(new Phonemes {
                    phoneme = phone.phoneme,
                    frame_length = (int)(phone.durationMs / 1000d * VoicevoxUtils.fps),
                });
            }
            notes.phonemes.Add(new Phonemes {
                phoneme = "pau",
                frame_length = tailFrames
            });

            int vvTotalFrames = -(headFrames + tailFrames);
            notes.phonemes.ForEach(x => vvTotalFrames += x.frame_length);
            double frameMs = (1000d / VoicevoxUtils.fps);
            int totalFrames = (int)(vvTotalFrames / VoicevoxUtils.fps * 1000d);
            int frameRatio = vvTotalFrames / totalFrames;
            const int pitchInterval = 5;


            notes.f0 = VoicevoxUtils.SampleCurve(phrase, phrase.pitches, 0, frameMs, vvTotalFrames, notes.phonemes[0].frame_length, notes.phonemes[^1].frame_length, -(VoicevoxUtils.headS + 10), x => MusicMath.ToneToFreq(x * 0.01)).ToList(); float[] f0Shifted = notes.f0.Select(f => (float)f).ToArray();
            if (phrase.toneShift != null) {
                for (int i = 0; i < notes.f0.Count; i++) {
                    double posMs = phrase.positionMs - phrase.leadingMs + i * frameMs;
                    int ticks = phrase.timeAxis.MsPosToTickPos(posMs) - (phrase.position - phrase.leading);
                    int index = Math.Max(0, (int)((double)ticks / pitchInterval));
                    if (index < phrase.pitches.Length) {
                        f0Shifted[i] = (float)MusicMath.ToneToFreq((phrase.pitches[index] + phrase.toneShift[index]) * 0.01);
                    }
                }
            }


            var volumeCurve = phrase.curves.FirstOrDefault(c => c.Item1 == VOLC);
            if (volumeCurve != null) {
                notes.f0 = VoicevoxUtils.SampleCurve(phrase, phrase.pitches, 0, frameMs, vvTotalFrames, notes.phonemes[0].frame_length, notes.phonemes[^1].frame_length, -(VoicevoxUtils.headS + 10), x => MusicMath.ToneToFreq(x * 0.01)).ToList();
            } else {
                notes.volume = Enumerable.Repeat(1d, vvTotalFrames).ToList();
            }

            notes.outputStereo = false;
            notes.outputSamplingRate = 44100;
            notes.volumeScale = 1;
            return notes;
        }


        public UExpressionDescriptor[] GetSuggestedExpressions(USinger singer, URenderSettings renderSettings) {
            //under development
            var result = new List<UExpressionDescriptor> {
                new UExpressionDescriptor{
                    name="volume (curve)",
                    abbr=VOLC,
                    type=UExpressionType.Curve,
                    min=0,
                    max=200,
                    defaultValue=100,
                    isFlag=false,
                },
            };

            return result.ToArray();
        }

        public override string ToString() => Renderers.VOICEVOX;

        RenderPitchResult IRenderer.LoadRenderedPitch(RenderPhrase phrase) {
            try {
                var singer = phrase.singer as VoicevoxSinger;
                if (singer != null) {
                    string singerID = VoicevoxUtils.defaultID;
                    Note[][] notes = new Note[phrase.phones.Length][];

                    for (int i = 0; i < phrase.phones.Length; i++) {
                        notes[i] = new Note[1];
                        notes[i][0] = new Note() {
                            lyric = phrase.phones[i].phoneme,
                            position = phrase.phones[i].position,
                            duration = phrase.phones[i].duration,
                            tone = phrase.phones[i].tone + phrase.phones[0].toneShift
                        };
                    }

                    var qNotes = VoicevoxUtils.NoteGroupsToVoicevox(notes, phrase.timeAxis);

                    string baseSingerID = VoicevoxUtils.getBaseSingerID(singer);
                    VoicevoxNote vvNotes = VoicevoxUtils.VoicevoxVoiceBase(qNotes, singerID);
                    int vvTotalFrames = 0 - vvNotes.phonemes[0].frame_length;
                    vvNotes.phonemes.ForEach(x => vvTotalFrames += x.frame_length);
                    var f0 = new double[vvTotalFrames];
                    for (int i = vvNotes.phonemes[0].frame_length;i< vvTotalFrames; i++) {
                        f0[i] = vvNotes.f0[i];
                    }

                    var result = new RenderPitchResult { tones = f0.Select(f => (float)MusicMath.FreqToTone(f * Math.Pow(2, ((phrase.phones[0].toneShift * -1) / 12d)))).ToArray(), ticks = new float[vvTotalFrames] };
                    var layout = Layout(phrase);
                    var t = layout.positionMs - layout.leadingMs;
                    for (int i = 0; i < result.tones.Length; i++) {
                        t += (1000d / VoicevoxUtils.fps);
                        result.ticks[i] = phrase.timeAxis.MsPosToTickPos(t) - phrase.position;
                    }
                    return result;
                }
            } catch {
            }
            return null;
        }


        ulong HashPhraseGroups(RenderPhrase phrase) {
            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {
                    writer.Write(phrase.preEffectHash);
                    writer.Write(phrase.phones[0].tone);
                    writer.Write(phrase.phones[0].direct);
                    //if (phrase.phones[0].direct) {
                    writer.Write(phrase.phones[0].toneShift);
                    //} else {
                    //    phrase.phones.ForEach(x => writer.Write(x.toneShift));
                    //}
                    writer.Write(phrase.phones[0].volume);
                    return XXH64.DigestOf(stream.ToArray());
                }
            }
        }
    }
}

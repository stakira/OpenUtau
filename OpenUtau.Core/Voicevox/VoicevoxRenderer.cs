using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using K4os.Hash.xxHash;
using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenUtau.Core.DiffSinger;
using OpenUtau.Core.Format;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;
using Serilog;
using SharpCompress;
using static OpenUtau.Api.Phonemizer;

namespace OpenUtau.Core.Voicevox {
    public class VoicevoxRenderer : IRenderer {
        const string VOLC = VoicevoxUtils.VOLC;
        const string KEYS = VoicevoxUtils.KEYS;
        const string PITD = Format.Ustx.PITD;

        static readonly HashSet<string> supportedExp = new HashSet<string>(){
            Format.Ustx.DYN,
            PITD,
            Format.Ustx.CLR,
            VOLC,
            KEYS,
            Format.Ustx.SHFC,
            Format.Ustx.SHFT
        };

        static readonly object lockObj = new object();

        public USingerType SingerType => USingerType.Voicevox;

        public bool SupportsRenderPitch => false;

        public bool SupportsExpression(UExpressionDescriptor descriptor) {
            return supportedExp.Contains(descriptor.abbr);
        }

        public RenderResult Layout(RenderPhrase phrase) {
            return new RenderResult() {
                leadingMs = phrase.leadingMs,
                positionMs = phrase.positionMs,
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
                    var wavPath = Path.Join(PathManager.Inst.CachePath, $"vv-{phrase.hash:x16}-{phrase.preEffectHash:x8}.wav");
                    var result = Layout(phrase);
                    if (!File.Exists(wavPath)) {
                        var singer = phrase.singer as VoicevoxSinger;
                        if (singer != null) {
                            Log.Information($"Starting Voicevox synthesis");
                            VoicevoxNote vvNotes = new VoicevoxNote();
                            string singerID = VoicevoxUtils.defaultID;
                            if (!singer.voicevoxConfig.Tag.Equals("VOICEVOX")) {
                                Note[][] notes = new Note[phrase.notes.Length][];

                                for (int i = 0; i < phrase.notes.Length; i++) {
                                    notes[i] = new Note[1];
                                    notes[i][0] = new Note() {
                                        lyric = phrase.notes[i].lyric,
                                        position = phrase.notes[i].position,
                                        duration = phrase.notes[i].duration,
                                        tone = phrase.notes[i].tone
                                    };
                                }

                                var qNotes = VoicevoxUtils.NoteGroupsToVoicevox(notes, phrase.timeAxis, singer);

                                if (singer.voicevoxConfig.base_singer_style != null) {
                                    foreach (var s in singer.voicevoxConfig.base_singer_style) {
                                        if (s.name.Equals(singer.voicevoxConfig.base_singer_name)) {
                                            vvNotes = VoicevoxUtils.VoicevoxVoiceBase(qNotes, s.styles.id.ToString());
                                            if (s.styles.name.Equals(singer.voicevoxConfig.base_singer_style_name)) {
                                                break;
                                            }
                                        } else {
                                            vvNotes = VoicevoxUtils.VoicevoxVoiceBase(qNotes, singerID);
                                            break;
                                        }
                                    }
                                } else {
                                    vvNotes = VoicevoxUtils.VoicevoxVoiceBase(qNotes, singerID);
                                }

                            } else {
                                vvNotes = PhraseToVoicevoxNotes(phrase);
                            }

                            int speaker = 0;
                            singer.voicevoxConfig.styles.ForEach(style => {
                                if (style.name.Equals(phrase.singer.Subbanks[1].Color)) {
                                    speaker = style.id;
                                }
                                if (style.name.Equals(phrase.phones.FirstOrDefault().suffix)) {
                                    speaker = style.id;
                                }
                            });
                            try {
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
                                    int channel = vvNotes.outputStereo ? 2 : 1;
                                    using (var waveFileWriter = new WaveFileWriter(wavPath, new WaveFormat(vvNotes.outputSamplingRate, 16, channel))) {
                                        waveFileWriter.Write(bytes, 0, bytes.Length);
                                    }
                                }
                            } catch (Exception e) {
                                Log.Error($"Failed to create a voice base.");
                            }
                            if (cancellation.IsCancellationRequested) {
                                return new RenderResult();
                            }
                        }
                    }
                    progress.Complete(phrase.phones.Length, progressInfo);
                    if (File.Exists(wavPath)) {
                        using (var waveStream = Wave.OpenFile(wavPath)) {
                            result.samples = Wave.GetSamples(waveStream.ToSampleProvider().ToMono(1, 0));
                        }
                        if (result.samples != null) {
                            Renderers.ApplyDynamics(phrase, result);
                        }
                    } else {
                        result.samples = new float[0];
                    }
                    return result;
                }
            });
            return task;
        }

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

            int vvTotalFrames = 0;
            double frameMs = 1 / 1000d * VoicevoxUtils.fps;
            notes.phonemes.ForEach(x => vvTotalFrames += x.frame_length);
            int totalFrames = (int)(vvTotalFrames / VoicevoxUtils.fps * 1000d);
            int frameRatio = vvTotalFrames / totalFrames;


            //var curve = phrase.pitches.SelectMany(item => Enumerable.Repeat(item, 5)).ToArray();
            List<double> f0 = VoicevoxUtils.SampleCurve(phrase, phrase.pitches, 0, frameMs, vvTotalFrames, headFrames, tailFrames, x => MusicMath.ToneToFreq(x * 0.01)).ToList();
            //notes.f0 = f0.Where((x, i) => i % frameRatio == 0).ToList();


            var volumeCurve = phrase.curves.FirstOrDefault(c => c.Item1 == VOLC);
            if (volumeCurve != null) {
                var volume = VoicevoxUtils.SampleCurve(phrase, volumeCurve.Item2, 0, frameMs, vvTotalFrames, headFrames, tailFrames, x => MusicMath.DecibelToLinear(x)).ToList();
                notes.volume = volume.Where((x, i) => i % frameRatio == 0).ToList();
            } else {
                notes.volume = Enumerable.Repeat(1d, vvTotalFrames).ToList();
            }

            notes.outputStereo = false;
            notes.outputSamplingRate = 44100;
            notes.volumeScale = 1;
            return notes;
        }


        public UExpressionDescriptor[] GetSuggestedExpressions(USinger singer, URenderSettings renderSettings) {
            var result = new List<UExpressionDescriptor> {
                new UExpressionDescriptor{
                    name="volume (curve)",
                    abbr=VOLC,
                    type=UExpressionType.Curve,
                    min=-20,
                    max=20,
                    defaultValue=0,
                    isFlag=false,
                },
                //new UExpressionDescriptor{
                //    name="key shift (curve)",
                //    abbr=KEYS,
                //    type=UExpressionType.Curve,
                //    min=-36,
                //    max=36,
                //    defaultValue=0,
                //    isFlag=false,
                //},
            };

            return result.ToArray();
        }

        public override string ToString() => Renderers.VOICEVOX;

        RenderPitchResult IRenderer.LoadRenderedPitch(RenderPhrase phrase) {
            throw new NotImplementedException();
        }
    }
}

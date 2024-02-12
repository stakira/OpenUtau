using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenUtau.Core.Format;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;
using Serilog;
using SharpCompress;
using static OpenUtau.Api.Phonemizer;

namespace OpenUtau.Core.Voicevox {
    public class VoicevoxRenderer : IRenderer {

        static readonly HashSet<string> supportedExp = new HashSet<string>(){
            Format.Ustx.DYN,
            Format.Ustx.PITD,
            Format.Ustx.CLR,
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
                    var wavPath = Path.Join(PathManager.Inst.CachePath, $"vv-{phrase.hash:x16}.wav");
                    var result = Layout(phrase);
                    if (!File.Exists(wavPath)) {
                        var config = VoicevoxConfig.Load(phrase.singer);
                        Log.Information($"Starting Voicevox synthesis");
                        VoicevoxNote vvNotes = new VoicevoxNote();
                        if (string.IsNullOrEmpty(config.PhonemizerType)) {
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

                            var qNotes = NoteGroupsToVoicevox(notes, phrase.timeAxis);
                            vvNotes = VoicevoxUtils.VoicevoxVoiceBase(qNotes, "6000");

                        } else if (config.PhonemizerType.Equals("VOICEVOX")) {
                            vvNotes = PhraseToVoicevoxNotes(phrase);
                        } else {
                            return new RenderResult();
                        }

                        int speaker = 0;
                        config.styles.ForEach(style => {
                            if (style.name.Equals(phrase.singer.Subbanks[0].Color)) {
                                speaker = style.id;
                            }
                            if (style.name.Equals(phrase.phones.FirstOrDefault().suffix)) {
                                speaker = style.id;
                            }
                        });
                        try {
                            var ins = VoicevoxClient.Inst;
                            var queryurl = new VoicevoxURL() { method = "POST", path = "/frame_synthesis", query = new Dictionary<string, string> { { "speaker", speaker.ToString() } }, body = JsonConvert.SerializeObject(vvNotes),accept= "audio/wav" };
                            ins.SendRequest(queryurl);
                            int channel = vvNotes.outputStereo ? 2 : 1;
                            using (var waveFileWriter = new WaveFileWriter(wavPath, new WaveFormat(vvNotes.outputSamplingRate, 16, channel))) {
                                waveFileWriter.Write(ins.bytes, 0, ins.bytes.Length);
                            }
                        } catch (Exception e) {
                            Log.Error($"Failed to create a voice base.");
                        }
                        if (cancellation.IsCancellationRequested) {
                            return new RenderResult();
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


        public VoicevoxQueryMain NoteGroupsToVoicevox(Note[][] notes, TimeAxis timeAxis) {
            BaseChinesePhonemizer.RomanizeNotes(notes);
            VoicevoxQueryMain qnotes = new VoicevoxQueryMain();
            int index = 0;
            int position = 0;
            qnotes.notes.Add(new VoicevoxQueryNotes {
                lyric = "",
                frame_length = 1,
                key = null,
                vqnindex = index
            });
            while (index < notes.Length) {
                if (position < notes[index][0].position) {
                    qnotes.notes.Add(new VoicevoxQueryNotes() {
                        lyric = notes[index][0].lyric,
                        frame_length = (int)timeAxis.TickPosToMsPos(notes[index][0].position - position) / 10,
                        key = notes[index][0].tone,
                        vqnindex = index
                    });
                    position = notes[index][0].position;
                } else {
                    qnotes.notes.Add(new VoicevoxQueryNotes {
                        lyric = notes[index][0].lyric,
                        frame_length = (int)timeAxis.TickPosToMsPos(notes[index].Sum(n => n.duration)) / 10,
                        key = notes[index][0].tone,
                        vqnindex = index
                    });
                    position += (int)timeAxis.MsPosToTickPos(qnotes.notes.Last().frame_length * 10);
                    index++;
                }
            }
            return qnotes;
        }

        static VoicevoxNote PhraseToVoicevoxNotes(RenderPhrase phrase) {
            VoicevoxNote notes = new VoicevoxNote {
                f0 = new List<float>(),
                volume = new List<float>(),
                phonemes = new List<Phonemes>(),

            };
            float[] vol = new float[0];
            phrase.curves.ForEach(curve => {
                if (curve.Item1.Equals(Format.Ustx.DYN)) {
                    vol = curve.Item2;
                }
            });
            foreach (var phone in phrase.phones) {
                notes.volume = vol.ToList();
                notes.f0 = phrase.pitches.ToList();
                notes.phonemes.Add(new Phonemes {
                    phoneme = phone.phoneme,
                    frame_length = phone.duration
                });
            }
            notes.outputStereo = false;
            notes.outputSamplingRate = 44100;
            notes.volumeScale = 1;
            return notes;
        }


        public VoicevoxNote VoicevoxVoiceBase(VoicevoxNote qNotes, string id) {
            JObject response = null;
            try {
                var ins = VoicevoxClient.Inst;
                var queryurl = new VoicevoxURL() { method = "POST", path = "/sing_frame_audio_query", query = new Dictionary<string, string> { { "speaker", id } }, body = JsonConvert.SerializeObject(qNotes) };
                ins.SendRequest(queryurl);
                Log.Information(ins.jObj.ToString());
                var configs = ins.jObj.ToObject<VoicevoxNote>();
                return configs;
            } catch (Exception e) {
                Log.Error($"Failed to create a voice base.:{e}");
            }
            return new VoicevoxNote();
        }

        public UExpressionDescriptor[] GetSuggestedExpressions(USinger singer, URenderSettings renderSettings) {
            return new UExpressionDescriptor[] { };
        }

        public override string ToString() => Renderers.VOICEVOX;

        RenderPitchResult IRenderer.LoadRenderedPitch(RenderPhrase phrase) {
            throw new NotImplementedException();
        }
    }
}

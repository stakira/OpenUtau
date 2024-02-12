using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenUtau.Api;
using OpenUtau.Core.Enunu;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Voicevox {
    [Phonemizer("Voicevox Phonemizer", "VOICEVOX")]
    public class VoicevoxPhonemizer : Phonemizer {
        readonly string PhonemizerType = "VOICEVOX";

        protected VoicevoxSinger singer;
        Dictionary<Note[], Phoneme[]> partResult = new Dictionary<Note[], Phoneme[]>();

        public override void SetSinger(USinger singer) {
            this.singer = singer as VoicevoxSinger;
            if(this.singer != null) {
                if (this.singer.voicevoxConfig.PhonemizerFlag) {
                    this.singer.voicevoxConfig.PhonemizerType = this.PhonemizerType;
                }
            }
        }

        public override void SetUp(Note[][] notes)  {
            partResult.Clear();
            var qNotes = NoteGroupsToVoicevox(notes, timeAxis);
            var vvNote = new VoicevoxNote();
            if (this.singer.voicevoxConfig.base_style != null) {
                foreach (var s in this.singer.voicevoxConfig.base_style) {
                    if (s.name.Equals(this.singer.voicevoxConfig.base_name)) {
                        vvNote = VoicevoxUtils.VoicevoxVoiceBase(qNotes, s.styles.id.ToString());
                        if (s.styles.name.Equals(this.singer.voicevoxConfig.base_style_name)) {
                            break;
                        }
                    } else {
                        vvNote = VoicevoxUtils.VoicevoxVoiceBase(qNotes, "6000");
                        break;
                    }
                }
            } else {
                vvNote = VoicevoxUtils.VoicevoxVoiceBase(qNotes, "6000");
            }

            //phoneme.ToArray().Zip(noteIndexes.ToArray(), (phoneme, noteIndex) => Tuple.Create(phoneme, noteIndex))
            //.GroupBy(tuple => tuple.Item2)
            //.ToList()
            //.ForEach(g => {
            //    if (g.Key >= 0) {
            //        var noteGroup = notes[g.Key];
            //        partResult[noteGroup] = g.Select(tu => tu.Item1).ToArray();
            //    }
            //});

            foreach (var note in qNotes.notes) {
                if (note.vqnindex <= 0) {
                    continue;
                }
                var noteGroup = notes[note.vqnindex];
                var phoneme = new List<Phoneme>();
                int duration = 0;
                int i = 0;
                var list = new List<Phonemes>(vvNote.phonemes);
                while (0 < list.Count) {
                    if (noteGroup[0].duration > duration) {
                        phoneme.Add(new Phoneme() { phoneme = list[i].phoneme, position = list[i].frame_length });
                        duration += (int)timeAxis.MsPosToTickPos(list[i].frame_length) * 10;
                        list.Remove(list[i]);
                        i++;
                    } else {
                        break;
                    }
                }
                partResult[noteGroup] = phoneme.ToArray();
            }

        }

        public VoicevoxQueryMain NoteGroupsToVoicevox(Note[][] notes, TimeAxis timeAxis) {
            BaseChinesePhonemizer.RomanizeNotes(notes);
            VoicevoxQueryMain qnotes = new VoicevoxQueryMain();
            int index = 0;
            int duration = 0;
            while (index < notes.Length) {
                if (duration < notes[index][0].duration) {
                    qnotes.notes.Add(new VoicevoxQueryNotes() {
                        lyric = "",
                        frame_length = (int)timeAxis.TickPosToMsPos(notes[index][0].duration - duration) / 10,
                        key = null,
                        vqnindex = -1
                    });
                    duration = notes[index][0].position + notes[index][0].duration;
                } else {
                    qnotes.notes.Add(new VoicevoxQueryNotes {
                        lyric = notes[index][0].lyric,
                        frame_length = (int)timeAxis.TickPosToMsPos(notes[index].Sum(n => n.duration)) / 10,
                        key = notes[index][0].tone,
                        vqnindex = index
                    });
                    duration += (int)timeAxis.MsPosToTickPos(qnotes.notes.Last().frame_length) * 10;
                    index++;
                }
            }
            return qnotes;
        }

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevs) {
            var ps = new List<Phoneme>();
            if (partResult.TryGetValue(notes, out var phonemes)) {
                return new Result {
                    phonemes = phonemes.Select(p => {
                        p.position = p.position - notes[0].position;
                        return p;
                    }).ToArray(),
                };
            }
            return new Result {
                phonemes = new Phoneme[] {
                    new Phoneme {
                        phoneme = "error",
                    }
                },
            };

        }

        public override void CleanUp() {
            partResult.Clear();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Melanchall.DryWetMidi.MusicTheory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Enunu;
using OpenUtau.Core.Ustx;
using Serilog;

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

        public override void SetUp(Note[][] notes) {
            partResult.Clear();
            var qNotes = VoicevoxUtils.NoteGroupsToVoicevox(notes, timeAxis);
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

            var phoneme = new List<Phoneme>();
            foreach (var ph in qNotes.notes) {
                
            }
            foreach (var ph in vvNote.phonemes) {
                phoneme.Add(new Phoneme() { phoneme = ph.phoneme,position = ph.frame_length });
            }
            var noteIndexes = new List<int>();
            foreach (var ph in qNotes.notes) {
                noteIndexes.Add(ph.vqnindex);
            }

            phoneme.Zip(noteIndexes, (phoneme, noteIndex) => Tuple.Create(phoneme, noteIndex))
            .GroupBy(tuple => tuple.Item2)
            .ToList()
            .ForEach(g => {
                if (g.Key >= 0) {
                    var noteGroup = notes[g.Key];
                    partResult[noteGroup] = g.Select(tu => tu.Item1).ToArray();
                }
            });

        }

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevs) {
            var ps = new List<Phoneme>();
            if (partResult.TryGetValue(notes, out var phonemes)) {
                if(prev != null) {
                    if (partResult.TryGetValue(new Note[] { (Note)prev }, out var phonemes2)) {
                        for (int i = 0; i < phonemes.Length; i++) {
                            phonemes[i].position = phonemes2[i].position - phonemes[i].position;
                        }
                    }
                }
                return new Result {
                    phonemes = phonemes,
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Voicevox {
    [Phonemizer("Voicevox Japanese Phonemizer", "VOICEVOX JA", language: "JA")]
    public class VoicevoxPhonemizer : Phonemizer {

        protected VoicevoxSinger singer;
        Dictionary<Note[], Phoneme[]> partResult = new Dictionary<Note[], Phoneme[]>();

        public override void SetSinger(USinger singer) {
            this.singer = singer as VoicevoxSinger;
            if (this.singer != null) {
                this.singer.voicevoxConfig.Tag = this.Tag;
            }
        }

        public override void SetUp(Note[][] notes)  {
            partResult.Clear();
            foreach(var lyric in notes) {
                lyric[0].lyric = lyric[0].lyric.Normalize();
                var lyricList = lyric[0].lyric.Split(" ");
                if (lyricList.Length > 1) {
                    lyric[0].lyric = lyricList[1];
                }
            }
            var qNotes = VoicevoxUtils.NoteGroupsToVoicevox(notes, timeAxis,this.singer);
            var vvNotes = new VoicevoxNote();
            string singerID = VoicevoxUtils.defaultID;
            if (this.singer.voicevoxConfig.base_singer_style != null) {
                foreach (var s in this.singer.voicevoxConfig.base_singer_style) {
                    if (s.name.Equals(this.singer.voicevoxConfig.base_singer_name)) {
                        vvNotes = VoicevoxUtils.VoicevoxVoiceBase(qNotes, s.styles.id.ToString());
                        if (s.styles.name.Equals(this.singer.voicevoxConfig.base_singer_style_name)) {
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

            var parentDirectory = Directory.GetParent(singer.Location).ToString();
            var yamlPath = Path.Join(parentDirectory, "phonemes.yaml");
            var yamlTxt = File.ReadAllText(yamlPath);
            var phonemes_list = Yaml.DefaultDeserializer.Deserialize<Phoneme_list>(yamlTxt);

            var list = new List<Phonemes>(vvNotes.phonemes);
            foreach (var note in qNotes.notes) {
                if (note.vqnindex < 0) {
                    list.Remove(list[0]);
                    continue;
                }
                var noteGroup = notes[note.vqnindex];
                var phoneme = new List<Phoneme>();
                int index = 0;
                while (list.Count > 0) {
                    if (phonemes_list.vowels.Contains(list[0].phoneme)) {
                        phoneme.Add(new Phoneme() { phoneme = list[0].phoneme, position = noteGroup[0].position });
                        index++;
                        list.Remove(list[0]);
                        break;
                    }else if (phonemes_list.consonants.Contains(list[0].phoneme)) {
                        phoneme.Add(new Phoneme() { phoneme = list[0].phoneme, position = noteGroup[0].position - (int)timeAxis.MsPosToTickPos((list[0].frame_length / VoicevoxUtils.fps) * 1000) });
                    }
                    list.Remove(list[0]);
                }
                partResult[noteGroup] = phoneme.ToArray();
            }
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

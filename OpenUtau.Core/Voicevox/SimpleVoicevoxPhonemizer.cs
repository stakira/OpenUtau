using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Voicevox;

namespace Voicevox {
    [Phonemizer("Simple Voicevox Japanese Phonemizer", "S-VOICEVOX JA", language: "JA")]
    public class SimpleVoicevoxPhonemizer : Phonemizer {

        protected VoicevoxSinger singer;

        public override void SetSinger(USinger singer) {
            this.singer = singer as VoicevoxSinger;
            if (this.singer != null) {
                this.singer.voicevoxConfig.Tag = this.Tag;
            }
        }

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            var note = notes[0];
            var currentLyric = note.lyric.Normalize(); //measures for Unicode

            Dictionary_list dic = new Dictionary_list();
            dic.Loaddic(singer.Location);
            int toneShift = 0;
            int? alt = null;
            if (note.phonemeAttributes != null) {
                var attr = note.phonemeAttributes.FirstOrDefault(attr => attr.index == 0);
                toneShift = attr.toneShift;
                alt = attr.alternate;
            }

            //currentLyric = note.phoneticHint.Normalize();
            Note[][] simplenotes = new Note[1][];
            var lyricList = notes[0].lyric.Split(" ");
            if (lyricList.Length > 1) {
                notes[0].lyric = lyricList[1];
            }
            if (VoicevoxUtils.IsHiraKana(notes[0].lyric)) {
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme {
                            phoneme = notes[0].lyric,
                        }
                    },
                };
            } else if (VoicevoxUtils.IsPau(notes[0].lyric)) {
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme {
                            phoneme = "R",
                        }
                    },
                };
            }
            else
            {
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme {
                            phoneme = "error",
                        }
                    },
                };
            }
        }
    }
}

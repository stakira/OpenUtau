using System.Collections.Generic;
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
            Phoneme[] phonemes = new Phoneme[notes.Length];
            for (int i = 0; i < notes.Length; i++) {
                var currentLyric = notes[i].lyric.Normalize(); //measures for Unicode
                int toneShift = 0;
                int? alt = null;
                if (notes[i].phonemeAttributes != null) {
                    var attr = notes[i].phonemeAttributes.FirstOrDefault(attr => attr.index == 0);
                    toneShift = attr.toneShift;
                    alt = attr.alternate;
                }

                //currentLyric = note.phoneticHint.Normalize();
                Note[][] simplenotes = new Note[1][];
                var lyricList = notes[i].lyric.Split(" ");
                if (lyricList.Length > 1) {
                    notes[i].lyric = lyricList[1];
                }
                if (VoicevoxUtils.IsHiraKana(notes[i].lyric)) {
                    phonemes[i] = new Phoneme { phoneme = notes[i].lyric };
                } else if (VoicevoxUtils.IsPau(notes[i].lyric)) {
                    phonemes[i] = new Phoneme { phoneme = notes[i].lyric };
                } else {
                    phonemes[i] = new Phoneme {
                        phoneme = "error",
                    };
                }
            }
            return new Result { phonemes = phonemes };
        }
    }
}

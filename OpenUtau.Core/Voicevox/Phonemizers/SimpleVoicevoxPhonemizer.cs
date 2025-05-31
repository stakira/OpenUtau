using System.Collections.Generic;
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
                VoicevoxUtils.Loaddic(this.singer);
            }
        }

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            List<Phoneme> phonemes = new List<Phoneme>();
            for (int i = 0; i < notes.Length; i++) {
                var currentLyric = notes[i].lyric.Normalize();
                var lyricList = currentLyric.Split(" ");
                if (lyricList.Length > 1) {
                    currentLyric = lyricList[1];
                }
                if (!VoicevoxUtils.IsSyllableVowelExtensionNote(notes[i].lyric)) {
                    string val = "error";
                    if (VoicevoxUtils.TryGetPau(notes[i].lyric, out string pau)) {
                        val = pau;
                    } else if (VoicevoxUtils.phoneme_List.kanas.ContainsKey(notes[i].lyric)) {
                        val = currentLyric;
                    } else if (VoicevoxUtils.dic.IsDic(notes[i].lyric)) {
                        val = VoicevoxUtils.dic.Lyrictodic(notes[i].lyric);
                    }
                    phonemes.Add(new Phoneme { phoneme = val });
                }
            }
            return new Result { phonemes = phonemes.ToArray() };
        }
    }
}

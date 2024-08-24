using System.Collections.Generic;
using System.Linq;
using IKg2p;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core {
    public abstract class BaseChinesePhonemizer : Phonemizer {
        public static Note[] ChangeLyric(Note[] group, string lyric) {
            var oldNote = group[0];
            group[0] = new Note {
                lyric = lyric,
                phoneticHint = oldNote.phoneticHint,
                tone = oldNote.tone,
                position = oldNote.position,
                duration = oldNote.duration,
                phonemeAttributes = oldNote.phonemeAttributes,
            };
            return group;
        }

        public static string[] Romanize(IEnumerable<string> lyrics) {
            var lyricsArray = lyrics.ToArray();
            var hanziLyrics = lyricsArray
                .Where(ZhG2p.MandarinInstance.IsHanzi)
                .ToList();
            List<G2pRes> g2pResults = ZhG2p.MandarinInstance.Convert(hanziLyrics.ToList(), false, false);
            var pinyinResult = g2pResults.Select(res => res.syllable).ToArray();
            if (pinyinResult == null) {
                return lyricsArray;
            }
            var pinyinIndex = 0;
            for (int i = 0; i < lyricsArray.Length; i++) {
                if (lyricsArray[i].Length == 1 && ZhG2p.MandarinInstance.IsHanzi(lyricsArray[i])) {
                    lyricsArray[i] = pinyinResult[pinyinIndex];
                    pinyinIndex++;
                }
            }
            return lyricsArray;
        }

        public static void RomanizeNotes(Note[][] groups) {
            var ResultLyrics = Romanize(groups.Select(group => group[0].lyric));
            Enumerable.Zip(groups, ResultLyrics, ChangeLyric).Last();
        }

        public override void SetUp(Note[][] groups, UProject project, UTrack track) {
            RomanizeNotes(groups);
        }
    }
}

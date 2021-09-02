using System.Collections.Generic;
using System.Globalization;

namespace OpenUtau.Core {
    public abstract class Phonemizer {
        public struct Note {
            public string lyric;
            public string hints;
            public int tone;
            public int position;
            public int duration;
        }

        public struct Phoneme {
            public string phoneme;
            public int duration;
        }

        public abstract string Name { get; }
        public abstract string Tag { get; }
        public abstract void SetSinger(Ustx.USinger singer);
        public abstract Phoneme[] Process(Note note, Note? prev, Note? next);

        public override string ToString() => $"[{Tag}] {Name}";

        public static IList<string> ToUnicodeElements(string lyric) {
            var result = new List<string>();
            var etor = StringInfo.GetTextElementEnumerator(lyric);
            while (etor.MoveNext()) {
                result.Add(etor.GetTextElement());
            }
            return result;
        }

        public static string TryMapPhoneme(string phoneme, int tone, Ustx.USinger singer) {
            var toneName = MusicMath.GetToneName(tone);
            if (singer.PrefixMap.TryGetValue(toneName, out var prefix)) {
                var phonemeMapped = prefix.Item1 + phoneme + prefix.Item2;
                if (singer.FindOto(phonemeMapped) != null) {
                    phoneme = phonemeMapped;
                }
            }
            return phoneme;
        }
    }
}

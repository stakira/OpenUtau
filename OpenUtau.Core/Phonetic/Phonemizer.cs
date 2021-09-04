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

        private double bpm;
        private int beatUnit;
        private int resolution;

        public abstract string Name { get; }
        public abstract string Tag { get; }
        public abstract void SetSinger(Ustx.USinger singer);

        /// <summary>
        /// Phonemize a consecutive sequence of notes.
        /// </summary>
        /// <param name="notes">A note and its extender notes. Always one or more.</param>
        /// <param name="prev">The consecutive note before the leading note.</param>
        /// <param name="next">The consecutive note after the last extender note.</param>
        /// <returns></returns>
        public abstract Phoneme[] Process(Note[] notes, Note? prev, Note? next);

        public override string ToString() => $"[{Tag}] {Name}";

        public void SetTiming(double bpm, int beatUnit, int resolution) {
            this.bpm = bpm;
            this.beatUnit = beatUnit;
            this.resolution = resolution;
        }

        protected double TickToMs(int tick) {
            return MusicMath.TickToMillisecond(tick, bpm, beatUnit, resolution);
        }

        protected int MsToTick(double ms) {
            return MusicMath.MillisecondToTick(ms, bpm, beatUnit, resolution);
        }

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

using System;
using System.Collections.Generic;
using System.Globalization;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Api {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PhonemizerAttribute : Attribute {
        public string Name { get; private set; }
        public string Tag { get; private set; }
        public string Author { get; private set; }

        /// <param name="name">Name of phonemizer. Required.</param>
        /// <param name="tag">Use IETF language code + phonetic type as tag, e.g., "EN ARPA", "JP VCV", etc. Required.</param>
        /// <param name="author">Author of this phonemizer.</param>
        public PhonemizerAttribute(string name, string tag, string author = null) {
            Name = name;
            Tag = tag;
            Author = author;
        }
    }

    public abstract class Phonemizer {
        public struct Note {
            public string lyric;
            public string phoneticHint;
            public int tone;
            public int position;
            public int duration;
        }

        public struct Phoneme {
            public string phoneme;
            public int position;

            public override string ToString() => $"\"{phoneme}\" pos:{position}";
        }

        public string Name { get; set; }
        public string Tag { get; set; }

        private double bpm;
        private int beatUnit;
        private int resolution;

        public abstract void SetSinger(USinger singer);

        /// <summary>
        /// Phonemize a consecutive sequence of notes.
        /// </summary>
        /// <param name="notes">A note and its extender notes. Always one or more.</param>
        /// <param name="prevNeighbour">The neighbour note before the leading note.</param>
        /// <param name="nextNeighbour">The neighbour note after the last extender note.</param>
        /// <returns></returns>
        public abstract Phoneme[] Process(Note[] notes, Note? prevNeighbour, Note? nextNeighbour);

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

        public static void MapPhonemes(Note[] notes, Phoneme[] phonemes, USinger singer) {
            int endPosition = 0;
            int index = 0;
            foreach (var note in notes) {
                endPosition += note.duration;
                while (index < phonemes.Length && phonemes[index].position < endPosition) {
                    phonemes[index].phoneme = MapPhoneme(phonemes[index].phoneme, note.tone, singer);
                    index++;
                }
            }
        }

        public static string MapPhoneme(string phoneme, int tone, USinger singer) {
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

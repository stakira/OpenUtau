using System;
using System.Collections.Generic;
using System.Globalization;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Api {
    /// <summary>
    /// Mark your Phonemizer class with this attribute for OpenUtau to load it.
    /// </summary>
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

    /// <summary>
    /// Phonemizer plugin interface.
    /// </summary>
    public abstract class Phonemizer {
        /// <summary>
        /// The input struct that represents a note.
        /// </summary>
        public struct Note {
            /// <summary>
            /// Lyric of the note, the part of lyric that is not enclosed in "[]".
            /// Example: if lyric on note is "read". The lyric is "read".
            /// Example: if lyric on note is "read[r iy d]". The lyric is "read".
            /// </summary>
            public string lyric;

            /// <summary>
            /// Phonetic hint,
            /// Example: if lyric on note is "read". The hint is null.
            /// Example: if lyric on note is "read[r iy d]". The hint is "r iy d".
            /// </summary>
            public string phoneticHint;

            /// <summary>
            /// Music tone of note. C4 = 60.
            /// </summary>
            public int tone;

            /// <summary>
            /// Position of note in part. Measured in ticks.
            /// Use TickToMs() and MsToTick() to convert between ticks and milliseconds .
            /// </summary>
            public int position;

            /// <summary>
            /// Duration of note in part. Measured in ticks.
            /// Use TickToMs() and MsToTick() to convert between ticks and milliseconds .
            /// </summary>
            public int duration;

            public override string ToString() => $"\"{lyric}\" pos:{position}";
        }

        /// <summary>
        /// The output struct that represents a phoneme.
        /// </summary>
        public struct Phoneme {
            /// <summary>
            /// Phoneme name. Should match one of oto alias.
            /// Note that you don't have to return tone-mapped phonemes. OpenUtau will do it afterwards.
            /// I.e., you can simply return "あ" even when best match is actually "あC5".
            /// OpenUtau will try to find the most suitable tone for this phoneme, and tone-map based on it.
            /// </summary>
            public string phoneme;

            /// <summary>
            /// Position of phoneme in note. Measured in ticks.
            /// Use TickToMs() and MsToTick() to convert between ticks and milliseconds .
            /// </summary>
            public int position;

            public override string ToString() => $"\"{phoneme}\" pos:{position}";
        }

        /// <summary>
        /// Result returned by Process().
        /// </summary>
        public struct Result {
            /// <summary>
            /// An array of phonemes that are corresponding to input notes.
            /// </summary>
            public Phoneme[] phonemes;
        }

        public string Name { get; set; }
        public string Tag { get; set; }

        private double bpm;
        private int beatUnit;
        private int resolution;

        /// <summary>
        /// Sets the current singer. Called by OpenUtau when user changes the singer.
        ///
        /// In addition to using data in the USinger class,
        /// a phonemizer can also use this method to load singer-specific resource,
        /// such as a custom dictionary file in the singer directory.
        /// Use singer.Location to access the singer directory.
        ///
        /// Do not modify the singer.
        /// </summary>
        /// <param name="singer"></param>
        public abstract void SetSinger(USinger singer);

        /// <summary>
        /// Phonemize a consecutive sequence of notes. This is the main logic of a phonemizer.
        /// </summary>
        /// <param name="notes">A note and its extender notes. Always one or more.</param>
        /// <param name="prev">The note before the leading note, if exists.</param>
        /// <param name="next">The note after the last extender note, if exists.</param>
        /// <param name="prevNeighbour">Same as prev if is immediate neighbour, otherwise null.</param>
        /// <param name="nextNeighbour">Same as next if is immediate neighbour, otherwise null.</param>
        public abstract Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour);

        public override string ToString() => $"[{Tag}] {Name}";

        /// <summary>
        /// Used by OpenUtau to set timing info for TickToMs() and MsToTick().
        /// Not need to call this method from within a phonemizer.
        /// </summary>
        public void SetTiming(double bpm, int beatUnit, int resolution) {
            this.bpm = bpm;
            this.beatUnit = beatUnit;
            this.resolution = resolution;
        }

        /// <summary>
        /// Utility method to convert ticks to milliseconds.
        /// </summary>
        protected double TickToMs(int tick) {
            return MusicMath.TickToMillisecond(tick, bpm, beatUnit, resolution);
        }

        /// <summary>
        /// Utility method to convert milliseconds to ticks.
        /// </summary>
        protected int MsToTick(double ms) {
            return MusicMath.MillisecondToTick(ms, bpm, beatUnit, resolution);
        }

        /// <summary>
        /// Utility method to convert plain string to Unicode elements.
        /// </summary>
        public static IList<string> ToUnicodeElements(string lyric) {
            var result = new List<string>();
            var etor = StringInfo.GetTextElementEnumerator(lyric);
            while (etor.MoveNext()) {
                result.Add(etor.GetTextElement());
            }
            return result;
        }

        /// <summary>
        /// Utility method to tone-map phonemes.
        /// Uses phoneme positions to find the most overlapped note, and tone-map based on it.
        /// </summary>
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

        /// <summary>
        /// Utility method to map a phoneme alias to proper pitch using prefixmap.
        /// For example, MapPhoneme("あ", 72, singer) may return "あC5".
        /// </summary>
        /// <param name="phoneme">Alias before pitch mapping.</param>
        /// <param name="tone">Music tone of note. C4 = 60.</param>
        /// <param name="singer">The singer.</param>
        /// <returns>Mapped alias.</returns>
        public static string MapPhoneme(string phoneme, int tone, USinger singer) {
            if (singer.TryGetMappedOto(phoneme, tone, out var oto)) {
                phoneme = oto.Alias;
            }
            return phoneme;
        }
    }
}

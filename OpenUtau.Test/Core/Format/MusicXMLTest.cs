using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

using OpenUtau.Core.Ustx;
using ReactiveUI;

namespace OpenUtau.Core.Format {
    public class MusicXMLTest {
        readonly ITestOutputHelper output;
        string basePath;

        public MusicXMLTest(ITestOutputHelper output){
            this.output = output;
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            basePath = Path.Join(dir, "Files", "MusicXML");
        }

        static void Pairwise<T>(IEnumerable<T> enumerable, Action<T, T> action){
            var enumerator = enumerable.GetEnumerator();
            if(!enumerator.MoveNext()) return;
            var prev = enumerator.Current;
            while(enumerator.MoveNext()){
                action(prev, enumerator.Current);
                prev = enumerator.Current;
            }
        }

        /// <summary>
        /// Assert that all notes in a part are legato,
        /// which means that there is no gap between two notes.
        /// </summary>
        /// <param name="part"></param>
        static void AssertLegato(UVoicePart part){
            Pairwise(part.notes, (n1, n2) => {
                Assert.Equal(n1.duration, n2.position - n1.position);
            });
        }

        [Fact]
        public void PitchesTest() {
            var project = MusicXML.LoadProject(Path.Join(basePath,"01a-Pitches-Pitches.musicxml"));
            var part = project.parts.First() as UVoicePart;
            int[] tonesOrig = new int[]{
                43, 45, 47, 48,
                50, 52, 53, 55,
                57, 59, 60, 62,
                64, 65, 67, 69,
                71, 72, 74, 76,
                77, 79, 81, 83,
                84, 86, 88, 89,
                91, 93, 95, 96,
            };
            int[] tones = tonesOrig
                .Concat(tonesOrig.Select(t => t+1))
                .Concat(tonesOrig.Select(t => t-1))
                .Concat(new int[]{64, 65, 67, 69, 71, 72, 74, 76, 74, 70, 73, 73, 73, 73})
                .ToArray();

            Assert.Equal(0, part.position);
            AssertLegato(part);
            Assert.Equal(0, part.notes.First().position);
            foreach(var (n, t) in part.notes.Zip(tones)){
                Assert.Equal(480, n.duration);
                Assert.Equal(t, n.tone);
            }
        }

        [Fact]
        public void RestsTest(){
            var project = MusicXML.LoadProject(Path.Join(basePath,"02a-Rests-Durations.musicxml"));
            var part = project.parts.First() as UVoicePart;
            Assert.Empty(part.notes);
        }

        [Fact]
        public void PitchedRestsTest(){
            var project = MusicXML.LoadProject(Path.Join(basePath,"02b-Rests-PitchedRests.musicxml"));
            var part = project.parts.First() as UVoicePart;
            Assert.Empty(part.notes);
        }

        [Fact]
        public void MultimeasureTimeSignaturesRestsTest(){
            var project = MusicXML.LoadProject(Path.Join(basePath,"02d-Rests-Multimeasure-TimeSignatures.musicxml"));
            var part = project.parts.First() as UVoicePart;
            Assert.Single(part.notes);
            Assert.Equal((2*4+3*3+2*2+2*4)*480, part.notes.First().position);
            Assert.Equal(4*480, part.notes.First().duration);
        }

        [Fact]
        public void RhythmDurationsTest(){
            var project = MusicXML.LoadProject(Path.Join(basePath,"03aa-Rhythm-Durations.musicxml"));
            var part = project.parts.First() as UVoicePart;

            Assert.Equal(0, part.notes.First().position);
            AssertLegato(part);
            var baseLengths = new int[] {128, 64, 32, 16, 8, 4, 2, 1, 1};
            var lengths = baseLengths.Select(l => l*30)
                .Concat(baseLengths.Select(l => l*45))
                .Concat(baseLengths.Skip(2).Select(l => l*105))
                .ToArray();
            foreach(var (n, l) in part.notes.Zip(lengths)){
                Assert.Equal(72, n.tone);
                Assert.Equal(l, n.duration);
            }
        }

        [Fact]
        public void LyricsTest(){
            var project = MusicXML.LoadProject(Path.Join(basePath, "61a-Lyrics.musicxml"));
            var part = project.parts.First() as UVoicePart;
            Assert.Equal(0, part.notes.First().position);
            AssertLegato(part);
            var notesList = part.notes.ToList();
            foreach(var n in notesList[..^1]){
                Assert.Equal(69, n.tone);
                Assert.Equal(480, n.duration);
            }
            Assert.Equal(69, notesList[^1].tone);
            Assert.Equal(960, notesList[^1].duration);
            Assert.Equal("Tra", notesList[0].lyric);
            Assert.Equal("la", notesList[1].lyric);
            Assert.Equal("li", notesList[2].lyric);
            Assert.Equal("Ja!", notesList[3].lyric);
            Assert.Equal("Tra", notesList[5].lyric);
            Assert.Equal("ra!", notesList[7].lyric);
            Assert.Equal("Bah!", notesList[9].lyric);
        }
    }
}

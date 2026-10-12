using System;
using System.IO;
using System.Linq;
using OpenUtau.Core.Neutrino;
using Xunit;

namespace OpenUtau.Test.Core.Neutrino {
    public class NeutrinoPhonemesTest {
        [Theory]
        [InlineData("br")]
        [InlineData("息")]
        [InlineData("R")]
        public void BreathAliasesProduceUnvoicedBreathPhones(string lyric) {
            var phones = NeutrinoPhonemes.Build(NeutrinoScore.Build(new[] {
                new NeutrinoScoreNote(lyric, 60, 0, 500),
                new NeutrinoScoreNote("p", 60, 500, 500),
            }));
            Assert.Equal(new[] { "pau", "br", "pau" }, phones.Select(p => p.Symbol));
            Assert.Equal(0, phones[1].Hz);
            Assert.Equal(.5, phones[1].Start, 7);
            Assert.Equal(1, phones[1].End, 7);
        }

        [Fact]
        public void PlusSustainsVowelAcrossPitchChangesWithoutRepeatingConsonants() {
            var score = NeutrinoScore.Build(new[] {
                new NeutrinoScoreNote("か", 60, 0, 500),
                new NeutrinoScoreNote("+", 60, 500, 500),
                new NeutrinoScoreNote("+", 64, 1000, 500),
                new NeutrinoScoreNote("+", 67, 1500, 500),
            });
            var phones = NeutrinoPhonemes.Build(score);
            Assert.Equal(new[] { "pau", "k", "a", "a", "a", "pau" }, phones.Select(p => p.Symbol));
            Assert.Equal(1.5, phones[1].End, 7);
            Assert.Equal(1.5, phones[3].Start, 7);
            Assert.Equal(2.0, phones[4].Start, 7);
            Assert.InRange(phones[3].Hz, 329.62f, 329.64f);
            Assert.InRange(phones[4].Hz, 391.99f, 392.01f);
            Assert.Equal(0, phones[3].InNote);
        }

        [Theory]
        [InlineData("p", 500)]
        [InlineData("か", 600)]
        public void PlusCannotCrossRestOrGap(string lyric, int start) {
            Assert.Throws<ArgumentException>(() => NeutrinoScore.Build(new[] {
                new NeutrinoScoreNote(lyric, 60, 0, 500),
                new NeutrinoScoreNote("+", 64, start, 500),
            }));
        }

        [Theory]
        [InlineData("あ", "a")]
        [InlineData("シ", "sh i")]
        [InlineData("きゃ", "ky a")]
        [InlineData("クァ", "k w a")]
        [InlineData("ヴョ", "by o")]
        [InlineData("ん", "N")]
        [InlineData("っ", "cl")]
        [InlineData("でぇ", "dy e")]
        [InlineData("シィ", "s i")]
        public void KanaMatchesNeutrinoAlphabet(string lyric, string expected) {
            Assert.Equal(expected, string.Join(" ", NeutrinoPhonemes.ConvertLyric(lyric)));
        }

        [Fact]
        public void TiedNotesAndRestsKeepNativeScoreFeatures() {
            // The long first note crosses a measure; every phone shares its score interval.
            var score = NeutrinoScore.Build(new[] {
                new NeutrinoScoreNote("きゃ", 60, 0, 4200),
                new NeutrinoScoreNote("p", 60, 4200, 300),
                new NeutrinoScoreNote("シ", 62, 4500, 500),
            });
            var phones = NeutrinoPhonemes.Build(score);
            Assert.Equal(new[] { "pau", "ky", "a", "pau", "sh", "i", "pau" }, phones.Select(p => p.Symbol));
            Assert.Equal(.5, phones[1].Start, 7);
            Assert.Equal(4.7, phones[1].End, 7);
            Assert.Equal(phones[1].Start, phones[2].Start);
            Assert.Equal(phones[1].End, phones[2].End);
            Assert.Equal(0, phones[1].InNote);
            Assert.Equal(1, phones[2].InNote);
            Assert.Equal(5, phones[4].Start, 7);
            Assert.Equal(5.5, phones[4].End, 7);
            Assert.InRange(phones[1].Hz, 261.62f, 261.64f);
        }

        [Fact]
        public void UnknownSyllableFailsClearly() {
            Assert.Throws<InvalidDataException>(() => NeutrinoPhonemes.ConvertLyric("未知"));
        }
    }
}

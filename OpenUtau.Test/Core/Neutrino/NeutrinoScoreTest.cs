using System;
using System.Linq;
using OpenUtau.Core.Neutrino;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;
using Xunit;
using OpenUtau.Core;
using OpenUtau.Core.Pipeline;

namespace OpenUtau.Test.Core.Neutrino {
    public class NeutrinoScoreTest {
        class TestSinger : USinger {
            public TestSinger() { found = true; loaded = true; }
            public override string Id => "neutrino-score-test";
            public override USingerType SingerType => USingerType.Neutrino;
            public override System.Collections.Generic.IList<USubbank> Subbanks => Array.Empty<USubbank>();
            public override bool TryGetOto(string phoneme, out UOto oto) { oto = UOto.OfDummy(phoneme); return true; }
        }

        [Fact]
        public void ImportedBreathIsRestAndTempoBeforePhraseIsPreserved() {
            var project = OpenUtau.Core.Format.Ustx.Create();
            project.tempos.Clear();
            project.tempos.Add(new UTempo(0, 148));
            project.timeAxis.BuildSegments(project);
            var track = project.tracks[0];
            track.Singer = new TestSinger();
            track.RendererSettings.Renderer = new NeutrinoRenderer();
            var part = new UVoicePart { position = 0, duration = 12000, trackNo = 0 };
            project.parts.Add(part);
            UNote? previous = null;
            UPhoneme? previousPhone = null;
            foreach (var input in new[] { (10320, 480, "br_inA3"), (11040, 120, "あ") }) {
                var note = UNote.Create();
                note.position = input.Item1; note.duration = input.Item2; note.lyric = input.Item3; note.tone = 60;
                note.ExtendedDuration = note.duration; note.Prev = previous;
                if (previous != null) { previous.Next = note; }
                previous = note; part.notes.Add(note);
                var phone = new UPhoneme { position = note.position, phoneme = note.lyric, Parent = note, Prev = previousPhone };
                if (previousPhone != null) { previousPhone.Next = phone; }
                previousPhone = phone; part.phonemes.Add(phone);
            }
            foreach (var phone in part.phonemes) { phone.Validate(new ValidateOptions(), project, track, part, phone.Parent); }
            var source = PhraseSource.FromPart(project, track, part, 1)!;
            var score = NeutrinoScore.Build(source.BuildPhrases()[0]);
            Assert.Equal(480, (int)score.Descendants("divisions").Single());
            Assert.Equal(new[] { 60d, 148d, 60d }, score.Descendants("sound").Select(sound => (double)sound.Attribute("tempo")!));
            Assert.Equal("あ", score.Descendants("lyric").Single().Value);
            Assert.All(score.Descendants("measure"), measure => Assert.Equal(1920, measure.Elements("note").Sum(n => (int)n.Element("duration")!)));
        }

        [Fact]
        public void BreathAliasesBecomeRestsAndUnknownAliasesAreRejected() {
            Assert.True(NeutrinoScore.IsRestLyric("br_inA3"));
            Assert.True(NeutrinoScore.IsRestLyric("br_outC4"));
            Assert.False(NeutrinoScore.IsRestLyric("あ"));
            var score = NeutrinoScore.Build(new[] { new NeutrinoScoreNote("br_inA3", 60, 0, 500), new NeutrinoScoreNote("あ", 60, 500, 500) });
            Assert.Equal("あ", score.Descendants("lyric").Single().Value);
            var error = Assert.Throws<ArgumentException>(() => NeutrinoScore.Build(new[] { new NeutrinoScoreNote("aA3", 60, 0, 500) }));
            Assert.Contains("aA3", error.Message);
        }
        [Fact]
        public void LongNoteHasTiesAndEveryMeasureIsComplete() {
            var score = NeutrinoScore.Build(new[] { new NeutrinoScoreNote("あ", 61, 0, 9000) });
            Assert.All(score.Descendants("measure"), measure =>
                Assert.Equal(4000, measure.Elements("note").Sum(note => (int)note.Element("duration")!)));
            Assert.Equal(2, score.Descendants("tie").Count(tie => (string?)tie.Attribute("type") == "start"));
            Assert.Equal(2, score.Descendants("tie").Count(tie => (string?)tie.Attribute("type") == "stop"));
            Assert.Single(score.Descendants("lyric"));
            Assert.All(score.Descendants("pitch"), pitch => {
                Assert.Equal("C", (string?)pitch.Element("step"));
                Assert.Equal(1, (int)pitch.Element("alter")!);
                Assert.Equal(4, (int)pitch.Element("octave")!);
            });
        }

        [Fact]
        public void MillisecondTimingAndGapArePreserved() {
            var score = NeutrinoScore.Build(new[] {
                new NeutrinoScoreNote("あ", 60, 1234, 250),
                new NeutrinoScoreNote("い", 62, 1584, 600),
            });
            var durations = score.Descendants("note").Select(note => (int)note.Element("duration")!).ToArray();
            Assert.Equal(new[] { 500, 250, 100, 600, 500, 2050 }, durations);
        }

        [Fact]
        public void OverlapsAndExtendersFailClearly() {
            Assert.Throws<ArgumentException>(() => NeutrinoScore.Build(new[] { new NeutrinoScoreNote("+", 60, 0, 500) }));
            Assert.Throws<ArgumentException>(() => NeutrinoScore.Build(new[] {
                new NeutrinoScoreNote("あ", 60, 0, 500), new NeutrinoScoreNote("い", 62, 400, 500),
            }));
        }

        [Fact]
        public void NeutrinoBankSelectsItsRenderer() {
            Assert.Equal(USingerType.Neutrino, SingerTypeUtils.SingerTypeFromName["neutrino"]);
            Assert.Equal(Renderers.NEUTRINO, Renderers.GetDefaultRenderer(USingerType.Neutrino));
            Assert.IsType<NeutrinoRenderer>(Renderers.CreateRenderer(Renderers.NEUTRINO));
        }
    }
}

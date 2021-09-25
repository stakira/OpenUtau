using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;

namespace OpenUtau.Core.Ustx {
    public class UstxSerializationTest {
        readonly ITestOutputHelper output;
        readonly UExpressionDescriptor descriptor;
        readonly UNote note;

        public UstxSerializationTest(ITestOutputHelper output) {
            this.output = output;
            descriptor = new UExpressionDescriptor("velocity", "vel", 0, 200, 100);

            note = UNote.Create();
            note.position = 120;
            note.duration = 60;
            note.tone = 42;
            note.lyric = "あ";
            note.noteExpressions.Add(new UExpression(descriptor) {
                value = 99,
            });
            note.phonemeExpressions.Add(new UExpression(descriptor) {
                index = 0,
                value = 123,
            });
        }

        [Fact]
        public void UExpressionDeserializationTest() {
            var exp = new UExpression(descriptor) { value = 123 };
            var json = JsonConvert.SerializeObject(exp, Formatting.Indented);
            output.WriteLine(json);

            var actual = JsonConvert.DeserializeObject<UExpression>(json);

            Assert.Null(actual.descriptor);
            Assert.Equal(123, actual.value);
        }

        [Fact]
        public void UNoteSerializationTest() {
            var actual = JsonConvert.SerializeObject(note, Formatting.Indented);
            output.WriteLine(actual);

            string expected = @"{
  'pos': 120,
  'dur': 60,
  'num': 42,
  'lrc': 'あ',
  'pit': {
    'data': [],
    'snapFirst': true
  },
  'vbr': {
    'length': 0.0,
    'period': 175.0,
    'depth': 25.0,
    'in': 10.0,
    'out': 10.0,
    'shift': 0.0,
    'drift': 0.0
  },
  'exp': null,
  'nex': [
    {
      'index': null,
      'abbr': 'vel',
      'value': 99.0
    }
  ],
  'pex': [
    {
      'index': 0,
      'abbr': 'vel',
      'value': 123.0
    }
  ],
  'phm': []
}";

            Assert.Equal(expected.Replace('\'', '\"').Replace("\r\n", "\n"), actual.Replace("\r\n", "\n"));
        }

        [Fact]
        public void UNoteDeserializationTest() {
            var json = JsonConvert.SerializeObject(note, Formatting.Indented);

            var actual = JsonConvert.DeserializeObject<UNote>(json);

            Assert.Equal(120, actual.position);
            Assert.Equal(60, actual.duration);
            Assert.Equal(42, actual.tone);
            Assert.Equal("あ", actual.lyric);
            Assert.Empty(actual.phonemes);
            Assert.Single(actual.phonemeExpressions);
            var vel = actual.phonemeExpressions[0];
            Assert.NotNull(vel);
            Assert.Null(vel.descriptor);
            Assert.Equal(123, vel.value);
        }
    }
}

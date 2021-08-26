using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;
using OpenUtau.Core.Formats;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace OpenUtau.Core.Ustx {
    public class UstxSerializationTest {
        readonly ITestOutputHelper output;
        readonly JsonConverter converter;
        readonly UExpressionDescriptor descriptor;
        readonly UNote note;

        public UstxSerializationTest(ITestOutputHelper output) {
            this.output = output;
            converter = new UExpressionConverter();
            descriptor = new UExpressionDescriptor("velocity", "vel", 0, 200, 100);

            note = UNote.Create();
            note.position = 120;
            note.duration = 60;
            note.noteNum = 42;
            note.lyric = "あ";
            note.expressions.Clear();
            var exp = new UExpression(descriptor) { value = 123 };
            note.expressions.Add(descriptor.abbr, exp);
        }

        [Fact]
        public void UExpressionSerializationTest() {
            var exp = new UExpression(descriptor) { value = 123 };

            var actual = JsonConvert.SerializeObject(exp, Formatting.Indented, converter);
            Assert.Equal("123.0", actual);
        }

        [Fact]
        public void UExpressionDeserializationTest() {
            var exp = new UExpression(descriptor) { value = 123 };
            var json = JsonConvert.SerializeObject(exp, Formatting.Indented, converter);
            output.WriteLine(json);

            var actual = JsonConvert.DeserializeObject<UExpression>(json, converter);

            Assert.Null(actual.descriptor);
            Assert.Equal(123, actual.value);
        }

        [Fact]
        public void UNoteSerializationTest() {
            var actual = JsonConvert.SerializeObject(note, Formatting.Indented, converter);
            output.WriteLine(actual);

            string expected = @"{
  'pos': 120,
  'dur': 60,
  'num': 42,
  'lrc': 'あ',
  'pho': [
    {
      'position': 0,
      'phoneme': 'a'
    }
  ],
  'pit': {
    'data': [],
    'snapFirst': true
  },
  'vbr': {
    'length': 0.0,
    'period': 100.0,
    'depth': 32.0,
    'in': 10.0,
    'out': 10.0,
    'shift': 0.0,
    'drift': 0.0
  },
  'exp': {
    'vel': 123.0
  }
}";

            Assert.Equal(expected.Replace('\'', '\"').Replace("\r\n", "\n"), actual.Replace("\r\n", "\n"));
        }

        [Fact]
        public void UNoteDeserializationTest() {
            var json = JsonConvert.SerializeObject(note, Formatting.Indented, converter);

            var actual = JsonConvert.DeserializeObject<UNote>(json, converter);

            Assert.Equal(120, actual.position);
            Assert.Equal(60, actual.duration);
            Assert.Equal(42, actual.noteNum);
            Assert.Equal("あ", actual.lyric);
            Assert.Single(actual.phonemes);
            Assert.Equal(0, actual.phonemes[0].position);
            Assert.Equal("a", actual.phonemes[0].phoneme);
            Assert.Single(actual.expressions);
            var vel = actual.expressions["vel"];
            Assert.NotNull(vel);
            Assert.Null(vel.descriptor);
            Assert.Equal(123, vel.value);
        }

        [Fact]
        public void Start() {
            var info = new ProcessStartInfo("powershell.exe") {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                StandardInputEncoding = Encoding.UTF8,
            };

            using (Process p = new Process()) {
                p.StartInfo = info;
                p.Start();
                using (StreamWriter writer = p.StandardInput) {
                    if (writer.BaseStream.CanWrite) {
                        writer.WriteLine("");
                        writer.WriteLine("chcp 65001");
                        writer.BaseStream.Write(Encoding.UTF8.GetBytes("echo 哈哈 > I:\\Code\\OpenUtau\\OpenUtau\\bin\\Debug\\1.txt\n"));
                    }
                }
                while (!p.StandardOutput.EndOfStream) {
                    output.WriteLine(p.StandardOutput.ReadLine());
                }
                p.WaitForExit();
            }
        }
    }
}

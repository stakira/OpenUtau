using OpenUtau.Core;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Classic {
    public class VoicebankConfigTest {
        readonly ITestOutputHelper output;

        public VoicebankConfigTest(ITestOutputHelper output) {
            this.output = output;
        }

        static VoicebankConfig CreateConfig() {
            return new VoicebankConfig() {
                SymbolSet = new SymbolSet() {
                    Preset = SymbolSetPreset.hiragana,
                },
                Subbanks = new Subbank[] {
                    new Subbank() {
                        ToneStart = "C1",
                        ToneEnd = "C4",
                    },
                    new Subbank() {
                        Dir = "D4",
                        Suffix = "D4",
                        ToneStart = "C#4",
                        ToneEnd = "F4",
                    },
                    new Subbank() {
                        Dir = "G4",
                        Suffix = "G4",
                        ToneStart = "F#4",
                        ToneEnd = "A#4",
                    },
                    new Subbank() {
                        Dir = "C5",
                        Suffix = "C5",
                        ToneStart = "B4",
                        ToneEnd = "B7",
                    },
                    new Subbank() {
                        Dir = "C5power",
                        Suffix = "C5P",
                        Flavor = "power",
                        ToneStart = "B4",
                        ToneEnd = "B7",
                    },
                    new Subbank() {
                        Dir = "C5shout",
                        Suffix = "C5S",
                        Flavor = "shout",
                        ToneStart = "B4",
                        ToneEnd = "B7",
                    },
                }
            };
        }

        [Fact]
        public void SerializationTest() {
            var yaml = Yaml.DefaultSerializer.Serialize(CreateConfig());
            output.WriteLine(yaml);

            Assert.Equal(@"symbol_set:
  preset: hiragana
  head: '-'
  tail: R
subbanks:
- tone_start: C1
  tone_end: C4
- dir: D4
  suffix: D4
  tone_start: C#4
  tone_end: F4
- dir: G4
  suffix: G4
  tone_start: F#4
  tone_end: A#4
- dir: C5
  suffix: C5
  tone_start: B4
  tone_end: B7
- dir: C5power
  suffix: C5P
  flavor: power
  tone_start: B4
  tone_end: B7
- dir: C5shout
  suffix: C5S
  flavor: shout
  tone_start: B4
  tone_end: B7
".Replace("\r\n", "\n"), yaml.Replace("\r\n", "\n"));
        }

        [Fact]
        public void RoundTripTest() {
            var yaml = Yaml.DefaultSerializer.Serialize(CreateConfig());
            var config = Yaml.DefaultDeserializer.Deserialize<VoicebankConfig>(yaml);
            var yaml2 = Yaml.DefaultSerializer.Serialize(config);

            Assert.Equal(yaml, yaml2);
        }
    }
}

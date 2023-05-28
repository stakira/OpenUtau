using Xunit;

namespace OpenUtau.Classic.Flags {
    public class UstFlagParserTest {
        [Fact]
        public void ParseTest() {
            // FIXME: no value flag support only "N"
            var given = "g-10Ny20Y30Mt+40";

            // then
            var parser = new UstFlagParser();
            var result = parser.Parse(given);

            // when
            Assert.Equal(5, result.Count);
            Assert.Equal("g", result[0].Key);
            Assert.Equal(-10, result[0].Value);
            Assert.Equal("N", result[1].Key);
            Assert.Equal(0, result[1].Value);
            Assert.Equal("y", result[2].Key);
            Assert.Equal(20, result[2].Value);
            Assert.Equal("Y", result[3].Key);
            Assert.Equal(30, result[3].Value);
            Assert.Equal("Mt", result[4].Key);
            Assert.Equal(40, result[4].Value);
        }

        [Fact]
        public void ParseSelector() {
            var result = new UstFlagParser().Parse("/1g10Mt-20");

            Assert.Equal(2, result.Count);
            Assert.Equal("g", result[0].Key);
            Assert.Equal(10, result[0].Value);
            Assert.Equal("Mt", result[1].Key);
            Assert.Equal(-20, result[1].Value);
        }

        [Fact]
        public void ParseInvalid() {
            var result = new UstFlagParser().Parse(" ?=Mt5 uu!");

            Assert.Equal(3, result.Count);
            Assert.Equal("Mt", result[0].Key);
            Assert.Equal(5, result[0].Value);
            Assert.Equal("u", result[1].Key);
            Assert.Equal("u", result[2].Key);
        }
    }
}

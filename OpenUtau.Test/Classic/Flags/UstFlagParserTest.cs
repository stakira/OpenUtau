using Xunit;

namespace OpenUtau.Classic.Flags {
    public class UstFlagParserTest {
        [Fact]
        public void ParseTest() {
            // FIXME: no value flag support only "N"
            var given = "g-10Ny20Y30Mt40";

            // then
            var parser = new UstFlagParser(); 
            var result = parser.Parse(given);

            // when
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
    }
}

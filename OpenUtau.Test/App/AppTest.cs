using Xunit;

namespace OpenUtau.App {
    public class AppTest {
        [Fact]
        public void BuildTest() {
            Assert.False(typeof(OpenUtau.App.App).IsAbstract);
            Assert.False(typeof(OpenUtau.App.Program).IsAbstract);
        }
    }
}

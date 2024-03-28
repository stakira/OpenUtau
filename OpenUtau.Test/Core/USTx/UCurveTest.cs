using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Core.Ustx {
    public class UCurveTest {
        readonly ITestOutputHelper output;
        readonly UCurve curve; 
        public UCurveTest(ITestOutputHelper output){
            this.output = output;
            var descriptor = new UExpressionDescriptor("testcurve", "TEST", 0, 200, 99);
            curve = new UCurve(descriptor);
            curve.xs = new List<int> { 10, 30, 60, 100 };
            curve.ys = new List<int> { 110, 40, 50, 140 };
        }

        [Fact]
        public void UCurveSampleTest() {
            //If the x value exists in xs, return the corresponding y value
            Assert.Equal(110, curve.Sample(10));
            Assert.Equal(40, curve.Sample(30));
            Assert.Equal(50, curve.Sample(60));
            Assert.Equal(140, curve.Sample(100));
            //If the x value is greater than xs[^1] or less than xs[0], return the default value specified in the descriptor
            Assert.Equal(99, curve.Sample(5));
            Assert.Equal(99, curve.Sample(120));
            //If the x value is between xs[i] and xs[i+1], return the linear interpolation of ys[i] and ys[i+1]
            Assert.Equal(47, curve.Sample(50));
            Assert.Equal(61, curve.Sample(65));
        }

        [Fact]
        public void UCurveSamplesTest() {
            //UCurve.Samples should give the same reault as UCurve.Sample
            var samples = curve.Samples(5, 24, 5).ToArray();
            Assert.Equal(110, samples[1]);
            Assert.Equal(40, samples[5]);
            Assert.Equal(50, samples[11]);
            Assert.Equal(140, samples[19]);
            Assert.Equal(99, samples[0]);
            Assert.Equal(99, samples[^1]);
            Assert.Equal(47, samples[9]);
            Assert.Equal(61, samples[12]);
        }
    }
}
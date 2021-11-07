using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;

namespace OpenUtau.Core.SignalChain {
    public class WaveSourceTest {
        readonly ITestOutputHelper output;

        public WaveSourceTest(ITestOutputHelper output) {
            this.output = output;
        }

        byte[] GetWavBytes() {
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var file = Path.Join(dir, "Files", "sine.wav");
            return File.ReadAllBytes(file);
        }

        float[] GetSamples() {
            var source = new WaveSource(0, 50, new List<Vector2>() {
                new Vector2(0, 1),
                new Vector2(50, 1),
            }, 0, 1);
            source.SetWaveData(GetWavBytes());
            var data = new float[2205];
            source.Mix(0, data, 0, 2205);
            return data;
        }

        [Fact]
        public void ReadTest() {
            var source = new WaveSource(0, 25, new List<Vector2>() {
                new Vector2(0, 1),
                new Vector2(25, 1),
            }, 0, 2);
            source.SetWaveData(GetWavBytes());

            var buffer = new float[500];
            int pos = 0;
            int newPos;
            while ((newPos = source.Mix(pos, buffer, 0, 500)) > pos) {
                output.WriteLine($"new pos {newPos}");
                pos = newPos;
            }
            output.WriteLine($"pos {pos}");
            Assert.Equal(44100 * 50 / 1000, pos);
        }

        [Fact]
        public void ReadWithOffsetTest() {
            var source = new WaveSource(15, 25, new List<Vector2>() {
                new Vector2(0, 1),
                new Vector2(25, 1),
            }, 0, 2);
            source.SetWaveData(GetWavBytes());

            var buffer = new float[500];
            int pos = 0;
            int newPos;
            while ((newPos = source.Mix(pos, buffer, 0, 500)) > pos) {
                output.WriteLine($"new pos {newPos}");
                pos = newPos;
            }
            output.WriteLine($"pos {pos}");
            Assert.Equal(44100 * 80 / 1000 - 1, pos);
        }

        [Fact]
        public void IsReadyTest() {
            var source = new WaveSource(0, 50, new List<Vector2>() {
                new Vector2(0, 1),
                new Vector2(50, 1),
            }, 0, 1);
            Assert.False(source.IsReady(0, 100));
            source.SetWaveData(GetWavBytes());
            Assert.True(source.IsReady(0, 100));
        }

        [Fact]
        public void IsReadyTestOutOfRange() {
            var source = new WaveSource(50, 50, new List<Vector2>() {
                new Vector2(0, 1),
                new Vector2(50, 1),
            }, 0, 1);
            Assert.True(source.IsReady(0, 100));
            Assert.True(source.IsReady(44100 * 50 / 1000 - 100, 100));
            Assert.False(source.IsReady(44100 * 50 / 1000 - 100 + 1, 100));
            Assert.True(source.IsReady(44100 * 100 / 1000, 100));
            Assert.False(source.IsReady(44100 * 100 / 1000 - 1, 100));
            source.SetWaveData(GetWavBytes());
            Assert.True(source.IsReady(44100 * 50 / 1000 - 100 + 1, 100));
            Assert.True(source.IsReady(44100 * 100 / 1000 - 1, 100));
        }
    }
}

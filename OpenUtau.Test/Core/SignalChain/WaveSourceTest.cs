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
            }, 0);
            source.SetWaveData(GetWavBytes());
            var data = new float[2205];
            source.Mix(0, data, 0, 2205);
            return data;
        }

        [Fact]
        public void ReadTest() {
            var source = new WaveSource(0, 50, new List<Vector2>() {
                new Vector2(0, 1),
                new Vector2(50, 1),
            }, 0);
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
            var source = new WaveSource(30, 50, new List<Vector2>() {
                new Vector2(0, 1),
                new Vector2(50, 1),
            }, 0);
            source.SetWaveData(GetWavBytes());

            var buffer = new float[500];
            int pos = 0;
            int newPos;
            while ((newPos = source.Mix(pos, buffer, 0, 500)) > pos) {
                output.WriteLine($"new pos {newPos}");
                pos = newPos;
            }
            output.WriteLine($"pos {pos}");
            Assert.Equal(44100 * 80 / 1000, pos);
        }

        [Fact]
        public void IsReadyTest() {
            var source = new WaveSource(0, 50, new List<Vector2>() {
                new Vector2(0, 1),
                new Vector2(50, 1),
            }, 0);
            Assert.False(source.IsReady(0, 100));
            source.SetWaveData(GetWavBytes());
            Assert.True(source.IsReady(0, 100));
        }

        [Fact]
        public void IsReadyTestOutOfRange() {
            var source = new WaveSource(50, 50, new List<Vector2>() {
                new Vector2(0, 1),
                new Vector2(50, 1),
            }, 0);
            Assert.True(source.IsReady(0, 100));
            Assert.True(source.IsReady(44100 * 50 / 1000 - 100, 100));
            Assert.False(source.IsReady(44100 * 50 / 1000 - 100 + 1, 100));
            Assert.True(source.IsReady(44100 * 100 / 1000, 100));
            Assert.False(source.IsReady(44100 * 100 / 1000 - 1, 100));
            source.SetWaveData(GetWavBytes());
            Assert.True(source.IsReady(44100 * 50 / 1000 - 100 + 1, 100));
            Assert.True(source.IsReady(44100 * 100 / 1000 - 1, 100));
        }

        [Fact]
        public void MixTest() {
            var source0 = new WaveSource(0, 50, new List<Vector2>() {
                new Vector2(0, 1),
                new Vector2(50, 1),
            }, 0);
            var source1 = new WaveSource(30, 50, new List<Vector2>() {
                new Vector2(0, 1),
                new Vector2(50, 1),
            }, 0);
            source0.SetWaveData(GetWavBytes());
            source1.SetWaveData(GetWavBytes());
            var mix = new WaveMix(new[] { source0, source1 });

            var samples = GetSamples();
            var buffer = new float[44100 * 80 / 1000];
            int pos = 0;
            int newPos;
            while ((newPos = mix.Mix(pos, buffer, 0, buffer.Length)) > pos) {
                output.WriteLine($"new pos {newPos}");
                pos = newPos;
            }
            output.WriteLine($"pos {pos}");
            Assert.Equal(44100 * 80 / 1000, pos);
            int p0 = 0;
            int p1 = 0;
            for (int i = 0; i < 44100 * 30 / 1000; ++i) {
                Assert.Equal(samples[p0++], buffer[p1++]);
            }
            p0 = 0;
            p1 = 44100 * 30 / 1000;
            int p2 = 44100 * 30 / 1000;
            for (int i = 0; i < 44100 * 20 / 1000; ++i) {
                Assert.Equal(samples[p0++] + samples[p2++], buffer[p1++]);
            }
            p0 = 44100 * 20 / 1000;
            p1 = 44100 * 50 / 1000;
            for (int i = 0; i < 44100 * 30 / 1000; ++i) {
                Assert.Equal(samples[p0++], buffer[p1++]);
            }
        }

        [Fact]
        public void MixTestEmptyStart() {
            var source0 = new WaveSource(40, 50, new List<Vector2>() {
                new Vector2(0, 1),
                new Vector2(50, 1),
            }, 0);
            var source1 = new WaveSource(60, 50, new List<Vector2>() {
                new Vector2(0, 1),
                new Vector2(50, 1),
            }, 0);
            source0.SetWaveData(GetWavBytes());
            source1.SetWaveData(GetWavBytes());
            var mix = new WaveMix(new[] { source0, source1 });

            var samples = GetSamples();
            var buffer = new float[44100 * 110 / 1000];
            int pos = 0;
            int newPos;
            while ((newPos = mix.Mix(pos, buffer, 0, buffer.Length)) > pos) {
                output.WriteLine($"new pos {newPos}");
                pos = newPos;
            }
            output.WriteLine($"pos {pos}");
            Assert.Equal(44100 * 110 / 1000, pos);

            int p0 = 0;
            for (int i = 0; i < 44100 * 40 / 1000; ++i) {
                Assert.Equal(0, buffer[p0++]);
            }
            p0 = 0;
            int p1 = 44100 * 40 / 1000;
            for (int i = 0; i < 44100 * 20 / 1000; ++i) {
                Assert.Equal(samples[p0++], buffer[p1++]);
            }
            p0 = 44100 * 20 / 1000;
            p1 = 44100 * 60 / 1000;
            int p2 = 0;
            for (int i = 0; i < 44100 * 30 / 1000; ++i) {
                Assert.Equal(samples[p0++] + samples[p2++], buffer[p1++]);
            }
            p0 = 44100 * 30 / 1000;
            p1 = 44100 * 90 / 1000;
            for (int i = 0; i < 44100 * 20 / 1000; ++i) {
                Assert.Equal(samples[p0++], buffer[p1++]);
            }
        }


        [Fact]
        public void NestedMixTest() {
            var source0 = new WaveSource(40, 50, new List<Vector2>() {
                new Vector2(0, 1),
                new Vector2(50, 1),
            }, 0);
            var source1 = new WaveSource(60, 50, new List<Vector2>() {
                new Vector2(0, 1),
                new Vector2(50, 1),
            }, 0);
            var source2 = new WaveSource(80, 50, new List<Vector2>() {
                new Vector2(0, 1),
                new Vector2(50, 1),
            }, 0);
            var source3 = new WaveSource(90, 50, new List<Vector2>() {
                new Vector2(0, 1),
                new Vector2(50, 1),
            }, 0);
            source0.SetWaveData(GetWavBytes());
            source1.SetWaveData(GetWavBytes());
            source2.SetWaveData(GetWavBytes());
            source3.SetWaveData(GetWavBytes());
            var mix0 = new WaveMix(new[] { source0, source1 });
            var mix1 = new WaveMix(new[] { source2, source3 });
            var mix2 = new WaveMix(new[] { mix0, mix1 });

            var buffer = new float[1000];
            int pos = 0;
            int newPos;
            while ((newPos = mix2.Mix(pos, buffer, 0, buffer.Length)) > pos) {
                Assert.True(mix2.IsReady(pos, buffer.Length));
                output.WriteLine($"new pos {newPos}");
                pos = newPos;
            }
            output.WriteLine($"pos {pos}");
            Assert.Equal(44100 * 140 / 1000, pos);
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenUtau.Classic {
    public class Frq {
        public const int kHopSize = 256;

        public int hopSize;
        public double averageF0;
        public double[] f0 = new double[0];
        public double[] amp = new double[0];

        public void Load(Stream stream) {
            using (var reader = new BinaryReader(stream)) {
                string header = new string(reader.ReadChars(8));
                if (header != "FREQ0003") {
                    throw new FormatException("FREQ0003 header not found.");
                }
                hopSize = reader.ReadInt32();
                averageF0 = reader.ReadDouble();
                _ = reader.ReadBytes(16); // blank
                int length = reader.ReadInt32();
                f0 = new double[length];
                amp = new double[length];
                for (int i = 0; i < length; i++) {
                    f0[i] = reader.ReadDouble();
                    amp[i] = reader.ReadDouble();
                }
            }
        }

        public void Save(Stream stream) {
            using (var writer = new BinaryWriter(stream)) {
                writer.Write(Encoding.ASCII.GetBytes("FREQ0003"));
                writer.Write(hopSize);
                writer.Write(averageF0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(f0.Length);
                for (int i = 0; i < f0.Length; ++i) {
                    writer.Write(f0[i]);
                    writer.Write(amp[i]);
                }
            }
        }

        public static Frq Build(float[] samples, double[] f0) {
            var frq = new Frq();
            frq.hopSize = kHopSize;
            frq.f0 = f0;
            frq.averageF0 = frq.f0.Where(f => f > 0).DefaultIfEmpty(0).Average();

            double ampMult = Math.Pow(2, 15);
            frq.amp = new double[frq.f0.Length];
            for (int i = 0; i < frq.amp.Length; ++i) {
                double sum = 0;
                int count = 0;
                for (int j = frq.hopSize * i; j < frq.hopSize * (i + 1) && j < samples.Length; ++j) {
                    sum += Math.Abs(samples[j]);
                    count++;
                }
                frq.amp[i] = count == 0 ? 0 : sum * ampMult / count;
            }
            return frq;
        }
    }
}

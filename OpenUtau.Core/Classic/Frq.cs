using System;
using System.IO;

namespace OpenUtau.Classic {
    public class Frq {
        public int hopSize;
        public double averageF0;
        public double[] f0;
        public double[] amp;

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
    }
}

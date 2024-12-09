using System;
using System.Collections.Generic;
using System.Text;

namespace OpenUtau.Core.Util {
    public static class Zstd {
        public static byte[] Compress(byte[] data) {
            using var compressor = new ZstdSharp.Compressor();
            var compressed = compressor.Wrap(data);
            return compressed.ToArray();
        }

        public static byte[] Decompress(byte[] data) {
            using var decompressor = new ZstdSharp.Decompressor();
            var decompressed = decompressor.Unwrap(data);
            return decompressed.ToArray();
        }
    }
}

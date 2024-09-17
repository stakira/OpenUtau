using System.Collections.Generic;
using System.Threading;

namespace OpenUtau.Classic {
    public interface IWavtool {
        // <output file> <input file> <STP> <note length>
        // [<p1> <p2> <p3> <v1> <v2> <v3> [<v4> <overlap> <p4> [<p5> <v5>]]]
        float[] Concatenate(List<ResamplerItem> resamplerItems, string tempPath, CancellationTokenSource cancellation);
        void CheckPermissions();
    }
}

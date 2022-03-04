using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using OpenUtau.Core;
using OpenUtau.Core.SignalChain;

namespace OpenUtau.Classic {
    interface IWavtool {
        // <output file> <input file> <STP> <note length>
        // [<p1> <p2> <p3> <v1> <v2> <v3> [<v4> <overlap> <p4> [<p5> <v5>]]]
        ISignalSource Concatenate(List<ResamplerItem> resamplerItems, CancellationTokenSource cancellation);
    }

    class SharpWavtool : IWavtool {
        public ISignalSource Concatenate(List<ResamplerItem> resamplerItems, CancellationTokenSource cancellation) {
            if (cancellation.IsCancellationRequested) {
                return null;
            }
            var mix = new WaveMix(resamplerItems.Select(item => {
                var posMs = item.phone.position * item.phrase.tickToMs - item.phone.preutterMs;
                var source = new WaveSource(posMs, item.requiredLength, item.phone.envelope, item.skipOver, 1);
                if (File.Exists(item.outputFile)) {
                    source.SetWaveData(File.ReadAllBytes(item.outputFile));
                } else {
                    source.SetWaveData(new byte[0]);
                }
                return source;
            }));
            return mix;
        }
    }
}

using System.Collections.Generic;
using System.Linq;

namespace OpenUtau.Core.SignalChain {
    public class WaveMix : ISignalSource {
        private readonly IEnumerable<ISignalSource> sources;

        public WaveMix(IEnumerable<ISignalSource> sources) {
            this.sources = sources.ToList();
        }

        public bool IsReady(int position, int count) {
            return sources.All(source => source.IsReady(position, count));
        }

        public int Mix(int position, float[] buffer, int index, int count) {
            return sources.Max(source => source.Mix(position, buffer, index, count));
        }
    }
}

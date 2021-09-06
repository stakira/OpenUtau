using System;

namespace OpenUtau.Core {
    public abstract class Transformer {
        public abstract string Name { get; }
        public abstract string Transform(string lyric);
        public override string ToString() => Name;
    }
}

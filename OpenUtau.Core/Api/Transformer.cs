using System;

namespace OpenUtau.Api {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class TransformerAttribute : Attribute {
        public string Name { get; private set; }
        public string Author { get; private set; }
        public TransformerAttribute(string name, string author = null) {
            Name = name;
            Author = author;
        }
    }

    public abstract class Transformer {
        public string Name { get; set; }
        public abstract string Transform(string lyric);
        public override string ToString() => Name;
    }
}

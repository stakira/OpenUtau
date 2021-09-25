using System;

namespace OpenUtau.Api {
    /// <summary>
    /// Mark your Transformer class with this attribute for OpenUtau to load it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class TransformerAttribute : Attribute {
        public string Name { get; private set; }
        public string Author { get; private set; }
        public TransformerAttribute(string name, string author = null) {
            Name = name;
            Author = author;
        }
    }

    /// <summary>
    /// Transformer plugin interface.
    /// </summary>
    public abstract class Transformer {
        /// <summary>
        /// The method to implement. It simply receives lyric of one note and returns a one lyric.
        /// </summary>
        /// <param name="lyric"></param>
        /// <returns></returns>
        public abstract string Transform(string lyric);

        public string Name { get; set; }
        public override string ToString() => Name;
    }
}

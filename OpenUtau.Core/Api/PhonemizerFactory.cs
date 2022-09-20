using System;
using System.Collections.Generic;
using System.Reflection;

namespace OpenUtau.Api {
    public class PhonemizerFactory {
        public Type type;
        public string name;
        public string tag;
        public string author;
        public string language;

        public Phonemizer Create() {
            var phonemizer = Activator.CreateInstance(type) as Phonemizer;
            phonemizer.Name = name;
            phonemizer.Tag = tag;
            phonemizer.Language = language;
            return phonemizer;
        }

        public override string ToString() => string.IsNullOrEmpty(author)
            ? $"[{tag}] {name}"
            : $"[{tag}] {name} (Contributed by {author})";

        private static Dictionary<Type, PhonemizerFactory> factories = new Dictionary<Type, PhonemizerFactory>();
        public static PhonemizerFactory Get(Type type) {
            if (!factories.TryGetValue(type, out var factory)) {
                var attr = type.GetCustomAttribute<PhonemizerAttribute>();
                if (attr == null || string.IsNullOrEmpty(attr.Name) || string.IsNullOrEmpty(attr.Tag)) {
                    return null;
                }
                factory = new PhonemizerFactory() {
                    type = type,
                    name = attr.Name,
                    tag = attr.Tag,
                    author = attr.Author,
                    language = attr.Language,
                };
                factories[type] = factory;
            }
            return factory;
        }
    }
}

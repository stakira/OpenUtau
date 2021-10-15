using System;
using System.Collections.Generic;
using System.Reflection;

namespace OpenUtau.Api {
    public class TransformerFactory {
        public Type type;
        public string name;
        public string author;
        public string Name => name;

        public Transformer Create() {
            var transformer = Activator.CreateInstance(type) as Transformer;
            transformer.Name = name;
            return transformer;
        }

        public override string ToString() => string.IsNullOrEmpty(author)
            ? $"{name}"
            : $"{name} by {author}";

        private static Dictionary<Type, TransformerFactory> factories = new Dictionary<Type, TransformerFactory>();
        public static TransformerFactory Get(Type type) {
            if (!factories.TryGetValue(type, out var factory)) {
                var attr = type.GetCustomAttribute<TransformerAttribute>();
                if (attr == null || string.IsNullOrEmpty(attr.Name)) {
                    return null;
                }
                factory = new TransformerFactory() {
                    type = type,
                    name = attr.Name,
                    author = attr.Author,
                };
                factories[type] = factory;
            }
            return factory;
        }
    }
}

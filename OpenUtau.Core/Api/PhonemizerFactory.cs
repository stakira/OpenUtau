using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OpenUtau.Api {
    public class PhonemizerFactory {
        public Type type;
        public string name;
        public string tag;
        public string author;
        public string language;

        public virtual Phonemizer Create() {
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
        private static Dictionary<string, PhonemizerFactory> luaFactories = new Dictionary<string, PhonemizerFactory>();
        private static PhonemizerFactory[] orderedFactories = [];
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

        public static PhonemizerFactory? Get(string? identity) {
            if (identity == null) return null;
            foreach (var factory in factories.Values) {
                if (factory.type.FullName == identity) {
                    return factory;
                }
            }
            if (luaFactories.TryGetValue(identity, out var luaFactory)) {
                return luaFactory;
            }
            return null;
        }

        public static void RegisterLua(PhonemizerFactory factory) {
            // Key must match LuaPhonemizer.PhonemizerIdentity so AfterLoad can find it.
            string key = factory is LuaPhonemizerFactory lua
                ? $"{Path.GetFileName(lua.packageDir)}:{Path.GetRelativePath(lua.packageDir, lua.scriptPath)}"
                : (factory.tag != null ? $"lua:{factory.tag}" : factory.name);
            luaFactories[key] = factory;
        }

        public static void BuildList() {
            orderedFactories = factories.Values
                .Concat(luaFactories.Values)
                .OrderBy(f => f.tag)
                .ToArray();
        }

        public static PhonemizerFactory[] GetAll() => orderedFactories;
    }
}

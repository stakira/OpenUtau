using System;

namespace OpenUtau.Api {
    public class LuaPhonemizerFactory : PhonemizerFactory {
        public string scriptPath;
        public string packageDir;
        public string scriptId;

        public override string ToString() => string.IsNullOrEmpty(author)
            ? $"[{tag}] {name} (Lua)"
            : $"[{tag}] {name} (Lua, contributed by {author})";

        public override Phonemizer Create() {
            var phonemizer = new LuaPhonemizer(scriptPath, packageDir);
            phonemizer.Name = name;
            phonemizer.Tag = tag;
            phonemizer.Language = language;
            return phonemizer;
        }
    }
}

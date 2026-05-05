using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Neo.IronLua;
using Serilog;
using YamlDotNet.Serialization;

namespace OpenUtau.Api {
    /// <summary>
    /// Builds and manages a sandboxed NeoLua environment.
    /// File access is URI-based: scripts use prefixed URIs such as "package:data/file" or
    /// "config:config.yaml". Each prefix maps to one real directory via AllowFileAccess.
    /// Dangerous globals are removed or replaced. OpenUtau APIs exposed under openutau.*.
    /// </summary>
    public class LuaSandbox : IDisposable {
        private readonly Lua lua;
        private LuaGlobal? env;
        private readonly Dictionary<string, (string ActualPath, bool Writeable)> prefixMap
            = new Dictionary<string, (string, bool)>(StringComparer.OrdinalIgnoreCase);
        private bool disposed;

        public LuaSandbox(Lua lua) {
            this.lua = lua;
        }

        /// <summary>Access the global environment (null before Initialize).</summary>
        public LuaGlobal? Env => env;

        /// <summary>
        /// Register a URI prefix that maps to <paramref name="actualPath"/>.
        /// Lua scripts access files via URIs like "prefix:relative/path".
        /// Each prefix may only be registered once.
        /// </summary>
        public LuaSandbox AllowFileAccess(string actualPath, string prefix, bool writeable) {
            prefixMap[prefix] = (NormalizePath(actualPath), writeable);
            return this;
        }

        /// <summary>Returns true if the given URI prefix has already been registered.</summary>
        public bool HasPrefix(string prefix) => prefixMap.ContainsKey(prefix);

        private static string NormalizePath(string path) {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Resolves a URI of the form "prefix:relative/path" to an absolute file-system path.
        /// Throws <see cref="LuaRuntimeException"/> if the prefix is unknown, the path escapes
        /// the registered directory, or (when <paramref name="requireWriteable"/> is true) the
        /// prefix was registered as read-only.
        /// </summary>
        private (string AbsPath, bool Writeable) ResolveUri(string uri, bool requireWriteable = false) {
            int colon = uri.IndexOf(':');
            if (colon < 0)
                throw new LuaRuntimeException($"Sandbox: invalid URI (missing prefix): {uri}", null);
            string prefix = uri.Substring(0, colon);
            string relative = uri.Substring(colon + 1).TrimStart('/', '\\');
            if (!prefixMap.TryGetValue(prefix, out var entry))
                throw new LuaRuntimeException($"Sandbox: unknown URI prefix '{prefix}'", null);
            string abs = string.IsNullOrEmpty(relative)
                ? entry.ActualPath
                : Path.GetFullPath(Path.Combine(entry.ActualPath, relative));
            string rootWithSep = entry.ActualPath + Path.DirectorySeparatorChar;
            if (!abs.Equals(entry.ActualPath, StringComparison.OrdinalIgnoreCase)
                && !abs.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase))
                throw new LuaRuntimeException($"Sandbox: path traversal denied: {uri}", null);
            if (requireWriteable && !entry.Writeable)
                throw new LuaRuntimeException($"Sandbox: write denied for URI: {uri}", null);
            return (abs, entry.Writeable);
        }

        /// <summary>
        /// Initialize the sandbox: block unsafe globals, set up safe io/os.
        /// Call this after configuring AllowFileAccess entries.
        /// </summary>
        public LuaGlobal Initialize() {
            env = lua.CreateEnvironment<LuaGlobal>();

            // Block unsafe globals
            env["dofile"] = null;
            env["loadfile"] = null;
            env["load"] = null;
            env["clr"] = null;
            env["package"] = null;
            env["coroutine"] = null;
            env["debug"] = null;
            env["collectgarbage"] = null;

            // Replace io with URI-aware safe version
            var originalIo = env["io"] as LuaTable ?? new LuaTable();
            env["io"] = BuildSafeIo(originalIo);

            // Replace os with safe version (only clock/time/date allowed)
            env["os"] = BuildSafeOs(env);

            return env;
        }

        /// <summary>
        /// Injects the openutau global table into the environment.
        /// All OpenUtau APIs live under openutau.*; no loose globals are added.
        /// </summary>
        public void InjectOpenUtauApi(
                Func<int, double> tickToMs,
                Func<double, int> msToTick,
                Func<string, string[]> queryG2p,
                Action<string> logInfo = null,
                Action<string> logWarn = null) {

            var ou = new LuaTable();

            // ── Timing ──────────────────────────────────────────────────────
            ou["tick_to_ms"] = (Func<int, double>)(tick => tickToMs(tick));
            ou["ms_to_tick"] = (Func<double, int>)(ms => msToTick(ms));

            // ── Text ────────────────────────────────────────────────────────
            ou["unicode_elements"] = (Func<string, LuaTable>)(str => {
                var elems = Phonemizer.ToUnicodeElements(str);
                var t = new LuaTable();
                for (int i = 0; i < elems.Count; i++) t[i + 1] = elems[i];
                return t;
            });

            // ── Logging ─────────────────────────────────────────────────────
            ou["log"] = (Action<object>)(msg => {
                string s = msg?.ToString() ?? "nil";
                Log.Information("[Lua] {Msg}", s);
                logInfo?.Invoke(s);
            });
            ou["log_warn"] = (Action<object>)(msg => {
                string s = msg?.ToString() ?? "nil";
                Log.Warning("[Lua] {Msg}", s);
                (logWarn ?? logInfo)?.Invoke("[warn] " + s);
            });
            // Convenience alias so scripts can write openutau.print(...)
            ou["print"] = ou["log"];

            // ── YAML ────────────────────────────────────────────────────────
            var yamlTable = new LuaTable();
            yamlTable["load"] = BuildYamlLoad();
            yamlTable["dump"] = BuildYamlDump();
            ou["yaml"] = yamlTable;

            // ── G2P ─────────────────────────────────────────────────────────
            if (queryG2p != null) {
                var g2pTable = new LuaTable();
                g2pTable["query"] = (Func<string, object>)(word => {
                    if (string.IsNullOrEmpty(word)) return null;
                    var result = queryG2p(word.ToLowerInvariant());
                    if (result == null || result.Length == 0) return null;
                    var t = new LuaTable();
                    for (int i = 0; i < result.Length; i++) t[i + 1] = result[i];
                    return t;
                });
                ou["g2p"] = g2pTable;
            }

            env!["openutau"] = ou;

            // ── Sandboxed require() ─────────────────────────────────────────
            var loaded = new LuaTable();
            env!["_LOADED"] = loaded;
            env!["require"] = (Func<string, object>)(modName => {
                string absPath;
                string uri = modName.EndsWith(".lua", StringComparison.OrdinalIgnoreCase)
                    ? modName : modName + ".lua";
                (absPath, _) = ResolveUri(uri, requireWriteable: false);
                if (!File.Exists(absPath)) {
                    throw new LuaRuntimeException($"require: module '{modName}' not found at {absPath}", null);
                }
                if (loaded[modName] != null) return loaded[modName];
                string code = File.ReadAllText(absPath);
                var chunk = this.lua.CompileChunk(code, Path.GetFileName(absPath), new LuaCompileOptions { ClrEnabled = false, DebugEngine = LuaExceptionDebugger.Default });
                LuaResult result = env!.DoChunk(chunk);
                object module = result.Count > 0 ? result[0] : true;
                loaded[modName] = module;
                return module;
            });
        }

        /// <summary>Execute a compiled chunk in the sandbox environment.</summary>
        public LuaResult DoChunk(LuaChunk chunk) {
            if (env == null) throw new InvalidOperationException("Sandbox not initialized. Call Initialize() first.");
            return env.DoChunk(chunk);
        }

        /// <summary>Get a value from the global environment.</summary>
        public object? GetGlobal(string name) {
            return env?[name];
        }

        public void Dispose() {
            if (!disposed) {
                disposed = true;
                lua.Dispose();
            }
        }

        ~LuaSandbox() {
            Dispose();
        }

        // ── Private builders ─────────────────────────────────────────────────

        private LuaTable BuildSafeIo(LuaTable originalIo) {
            var io = new LuaTable();

            io["open"] = (Func<string, string, object>)((uri, mode) => {
                bool write = mode != null && (mode.Contains('w') || mode.Contains('a') || mode.Contains('+'));
                var (abs, _) = ResolveUri(uri, requireWriteable: write);
                if (write) {
                    string? dir = Path.GetDirectoryName(abs);
                    if (dir != null) Directory.CreateDirectory(dir);
                }
                return ((dynamic)originalIo["open"])(abs, mode);
            });

            io["lines"] = (Func<string, object>)(uri => {
                var (abs, _) = ResolveUri(uri);
                return ((dynamic)originalIo["lines"])(abs);
            });

            return io;
        }

        private static LuaTable BuildSafeOs(LuaGlobal env) {
            var oldOs = env["os"] as LuaTable ?? new LuaTable();
            var os = new LuaTable();
            os["clock"] = oldOs["clock"];
            os["time"] = oldOs["time"];
            os["date"] = oldOs["date"];
            return os;
        }

        private Func<string, object> BuildYamlLoad() {
            return uri => {
                var (abs, _) = ResolveUri(uri);
                if (!File.Exists(abs)) {
                    throw new LuaRuntimeException($"openutau.yaml.load: file not found: {uri}", null);
                }
                string text;
                try {
                    text = File.ReadAllText(abs);
                } catch (Exception ex) {
                    throw new LuaRuntimeException($"openutau.yaml.load: cannot read {uri}: {ex.Message}", null);
                }
                var deserializer = new DeserializerBuilder().Build();
                var obj = deserializer.Deserialize(new StringReader(text));
                return ObjectToLua(obj);
            };
        }

        private Action<object, string> BuildYamlDump() {
            return (data, uri) => {
                var (abs, _) = ResolveUri(uri, requireWriteable: true);
                string? dir = Path.GetDirectoryName(abs);
                if (dir != null) Directory.CreateDirectory(dir);
                var serializer = new SerializerBuilder().Build();
                string yaml = serializer.Serialize(LuaToObject(data));
                File.WriteAllText(abs, yaml);
            };
        }

        private static bool IsPathAllowed(string absPath, List<string> allowedPaths) {
            foreach (var allowed in allowedPaths) {
                string normalizedAllowed = Path.GetFullPath(allowed).TrimEnd(Path.DirectorySeparatorChar);
                if (absPath.StartsWith(normalizedAllowed, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Recursively converts a YAML-deserialized object to a Lua table.</summary>
        internal static object ObjectToLua(object? obj) {
            if (obj == null) return null;
            if (obj is Dictionary<object, object> dict) {
                var t = new LuaTable();
                foreach (var kv in dict) t[kv.Key?.ToString() ?? ""] = ObjectToLua(kv.Value);
                return t;
            }
            if (obj is List<object> list) {
                var t = new LuaTable();
                for (int i = 0; i < list.Count; i++) t[i + 1] = ObjectToLua(list[i]);
                return t;
            }
            return obj;
        }

        private static object LuaToObject(object obj) {
            if (obj is LuaTable t) {
                bool isArray = true;
                int maxIdx = 0;
                foreach (var kv in t) {
                    if (kv.Key is int idx && idx >= 1) {
                        maxIdx = Math.Max(maxIdx, idx);
                    } else {
                        isArray = false;
                        break;
                    }
                }
                if (isArray && maxIdx > 0) {
                    var list = new List<object?>();
                    for (int i = 1; i <= maxIdx; i++) list.Add(LuaToObject(t[i]));
                    return list;
                } else {
                    var dict = new Dictionary<string, object?>();
                    foreach (var kv in t) dict[kv.Key?.ToString() ?? ""] = LuaToObject(kv.Value);
                    return dict;
                }
            }
            return obj;
        }
    }
}

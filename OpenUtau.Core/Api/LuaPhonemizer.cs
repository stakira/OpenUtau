using System;
using System.Collections.Generic;
using System.IO;
using Neo.IronLua;
using OpenUtau.Core.G2p;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Api {
    /// <summary>
    /// Exposes a USinger's OTO lookup and metadata to a Lua script.
    /// </summary>
    public class LuaSingerProxy {
        private readonly USinger singer;

        public LuaSingerProxy(USinger singer) {
            this.singer = singer;
        }

        public string name => singer.Name;
        public string location => singer.Location;

        public bool has_oto(string alias) => singer.TryGetOto(alias, out _);

        public string map_phoneme(string alias, int tone, string color = "") {
            return Phonemizer.MapPhoneme(alias, tone, color, "", singer);
        }

        /// <summary>
        /// Returns the tone-mapped OTO alias if found, or nil if the alias doesn't exist in the voicebank.
        /// Use this for fallback chains where you need to check existence before committing to an alias.
        /// </summary>
        public object? try_map_phoneme(string alias, int tone, string color = "") {
            if (singer.TryGetMappedOto(alias, tone, color, out var oto)) {
                return oto.Alias;
            }
            return null;
        }
    }

    /// <summary>
    /// A Phonemizer backed by a Lua script running in a sandboxed NeoLua environment.
    /// Scripts must define get_info() and process(notes, ctx). Optional: set_singer, setup, cleanup.
    /// </summary>
    public class LuaPhonemizer : Phonemizer {
        private readonly string scriptPath;
        private readonly string packageDir;
        private LuaSandbox? sandbox;
        private LuaChunk? chunk;
        private object? processFn;
        private object? setupFn;
        private object? setSingerFn;
        private object? cleanupFn;
        private bool hasError;
        private Action<string>? logInfoOverride;
        private Action<string>? logWarnOverride;

        private static readonly Lazy<ArpabetG2p> sharedArpabetG2p =
            new Lazy<ArpabetG2p>(() => new ArpabetG2p(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        public override string PhonemizerIdentity => $"{Path.GetFileName(packageDir)}:{Path.GetRelativePath(packageDir, scriptPath)}";

        public LuaPhonemizer(string scriptPath, string packageDir) {
            this.scriptPath = scriptPath;
            this.packageDir = packageDir;
        }

        /// <summary>
        /// Redirect Lua log/print output to custom actions (useful for tests and headless tools).
        /// </summary>
        public void SetLogOverrides(Action<string> info, Action<string> warn = null) {
            logInfoOverride = info;
            logWarnOverride = warn ?? info;
        }

        private void EnsureLoaded() {
            if (sandbox != null || hasError) return;
            try {
                var lua = new Lua();
                sandbox = new LuaSandbox(lua);

                // Register URI prefixes for file access
                sandbox.AllowFileAccess(packageDir, "package", writeable: false);
                sandbox.AllowFileAccess(OpenUtau.Core.PathManager.Inst.DictionariesPath, "dicts", writeable: false);

                // Per-package config dir (writeable; created lazily on first write)
                sandbox.AllowFileAccess(GetConfigDir(), "config", writeable: true);

                // Per-package cache dir (writeable; created lazily on first write)
                sandbox.AllowFileAccess(GetCacheDir(), "cache", writeable: true);

                // Initialize the sandbox environment
                sandbox.Initialize();

                string code = File.ReadAllText(scriptPath);
                chunk = lua.CompileChunk(code, Path.GetFileName(scriptPath), new LuaCompileOptions());

                sandbox.InjectOpenUtauApi(
                    tick => timeAxis != null ? timeAxis.TickPosToMsPos(tick) : tick / 8.0,
                    ms => timeAxis != null ? timeAxis.MsPosToTickPos(ms) : (int)(ms * 8),
                    word => sharedArpabetG2p.Value.Query(word),
                    logInfoOverride, logWarnOverride);

                sandbox.DoChunk(chunk);
                CacheFunctions();
            } catch (Exception ex) {
                Log.Error(ex, "LuaPhonemizer: failed to load script {Path}", scriptPath);
                hasError = true;
                sandbox?.Dispose();
                sandbox = null;
            }
        }

        private string GetConfigDir() {
            string packageName = Path.GetFileName(packageDir);
            return Path.Combine(OpenUtau.Core.PathManager.Inst.DependencyConfigsPath, packageName);
        }

        private string GetCacheDir() {
            string packageName = Path.GetFileName(packageDir);
            return Path.Combine(OpenUtau.Core.PathManager.Inst.CachePath, packageName);
        }

        private void CacheFunctions() {
            if (sandbox?.Env == null) return;
            var env = sandbox.Env;
            if (env["process"] is object pf) processFn = pf;
            if (env["setup"] is object sf) setupFn = sf;
            if (env["set_singer"] is object ssf) setSingerFn = ssf;
            if (env["cleanup"] is object cf) cleanupFn = cf;
        }

        public override void SetSinger(USinger singer) {
            EnsureLoaded();
            if (sandbox?.Env == null || hasError) return;

            // Add singer's folder as a read-only prefix (only on first singer assignment)
            if (singer != null && singer.Found && !string.IsNullOrEmpty(singer.Location)
                    && !sandbox.HasPrefix("singer")) {
                sandbox.AllowFileAccess(singer.Location, "singer", writeable: false);
            }

            try {
                if (setSingerFn != null) {
                    var proxy = new LuaSingerProxy(singer);
                    ((dynamic)setSingerFn)(proxy);
                }
            } catch (Exception ex) {
                Log.Warning(ex, "LuaPhonemizer: set_singer failed in {Path}", scriptPath);
            }
        }

        public override void SetUp(Note[][] notes, UProject project, UTrack track) {
            EnsureLoaded();
            if (sandbox?.Env == null || hasError) return;
            try {
                if (setupFn != null) {
                    var luaGroups = NoteGroupsToLua(notes);
                    ((dynamic)setupFn)(luaGroups);
                }
            } catch (Exception ex) {
                Log.Warning(ex, "LuaPhonemizer: setup failed in {Path}", scriptPath);
            }
        }

        public override Result Process(Note[] notes, Note? prev, Note? next,
                                       Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            EnsureLoaded();
            if (sandbox?.Env == null || hasError) {
                return new Result { phonemes = Array.Empty<Phoneme>() };
            }
            try {
                var luaNotes = NoteArrayToLua(notes);
                var ctx = BuildContext(prev, next, prevNeighbour, nextNeighbour, prevNeighbours);
                LuaResult raw = processFn != null ? ((dynamic)processFn)(luaNotes, ctx) : LuaResult.Empty;
                return raw.Count > 0 ? ParseResult(raw) : new Result { phonemes = Array.Empty<Phoneme>() };
            } catch (Exception ex) {
                Log.Warning(ex, "LuaPhonemizer: process failed in {Path}", scriptPath);
                return new Result { phonemes = Array.Empty<Phoneme>() };
            }
        }

        public override void CleanUp() {
            if (sandbox?.Env == null || hasError) return;
            try {
                if (cleanupFn != null) {
                    ((dynamic)cleanupFn)();
                }
            } catch (Exception ex) {
                Log.Warning(ex, "LuaPhonemizer: cleanup failed in {Path}", scriptPath);
            }
        }

        // ── Type conversion helpers ──────────────────────────────────────────

        private static LuaTable NoteToLua(Note note) {
            var t = new LuaTable();
            t["lyric"] = note.lyric;
            t["phonetic_hint"] = string.IsNullOrEmpty(note.phoneticHint) ? null : (object)note.phoneticHint;
            t["tone"] = note.tone;
            t["position"] = note.position;
            t["duration"] = note.duration;
            if (note.phonemeAttributes != null && note.phonemeAttributes.Length > 0) {
                var pa = new LuaTable();
                for (int i = 0; i < note.phonemeAttributes.Length; i++) {
                    var attr = note.phonemeAttributes[i];
                    var at = new LuaTable();
                    at["index"] = attr.index;
                    at["consonant_stretch_ratio"] = attr.consonantStretchRatio != null ? (object)attr.consonantStretchRatio : null;
                    at["tone_shift"] = attr.toneShift;
                    at["alternate"] = attr.alternate != null ? (object)attr.alternate : null;
                    at["voice_color"] = string.IsNullOrEmpty(attr.voiceColor) ? null : (object)attr.voiceColor;
                    pa[i + 1] = at;
                }
                t["phoneme_attributes"] = pa;
            } else {
                t["phoneme_attributes"] = null;
            }
            return t;
        }

        private static LuaTable NoteArrayToLua(Note[] notes) {
            var t = new LuaTable();
            for (int i = 0; i < notes.Length; i++) {
                t[i + 1] = NoteToLua(notes[i]);
            }
            return t;
        }

        private static LuaTable NoteGroupsToLua(Note[][] groups) {
            var t = new LuaTable();
            for (int i = 0; i < groups.Length; i++) {
                t[i + 1] = NoteArrayToLua(groups[i]);
            }
            return t;
        }

        private static LuaTable BuildContext(Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            var ctx = new LuaTable();
            ctx["prev"] = prev.HasValue ? (object)NoteToLua(prev.Value) : null;
            ctx["prev_adjacent"] = prevNeighbour.HasValue;
            ctx["next"] = next.HasValue ? (object)NoteToLua(next.Value) : null;
            ctx["next_adjacent"] = nextNeighbour.HasValue;
            if (prevNeighbours != null && prevNeighbours.Length > 0) {
                ctx["prev_neighbours"] = NoteArrayToLua(prevNeighbours);
            } else {
                ctx["prev_neighbours"] = null;
            }
            return ctx;
        }

        private Result ParseResult(LuaResult raw) {
            var phonemes = new List<Phoneme>();
            var table = raw[0] as LuaTable;
            if (table == null) {
                Log.Warning("LuaPhonemizer: process() in {Path} must return a table", scriptPath);
                return new Result { phonemes = Array.Empty<Phoneme>() };
            }

            int idx = 1;
            while (true) {
                var entry = table[idx];
                if (entry == null) break;

                if (entry is LuaTable row) {
                    string? phoneme = row["phoneme"]?.ToString();
                    if (string.IsNullOrEmpty(phoneme)) {
                        Log.Warning("LuaPhonemizer: entry #{Idx} missing 'phoneme' in {Path}", idx, scriptPath);
                        idx++;
                        continue;
                    }

                    var posObj = row["position"];
                    if (posObj == null) {
                        Log.Warning("LuaPhonemizer: entry #{Idx} missing 'position' in {Path}", idx, scriptPath);
                        idx++;
                        continue;
                    }

                    int position;
                    try {
                        position = Convert.ToInt32(posObj);
                    } catch {
                        Log.Warning("LuaPhonemizer: entry #{Idx} 'position' not an integer in {Path}", idx, scriptPath);
                        idx++;
                        continue;
                    }

                    var p = new Phoneme { phoneme = phoneme, position = position };

                    if (row["expressions"] is LuaTable exprs) {
                        var exprList = new List<PhonemeExpression>();
                        int ei = 1;
                        while (exprs[ei] is LuaTable expr) {
                            string? abbr = expr["abbr"]?.ToString();
                            if (!string.IsNullOrEmpty(abbr) && expr["value"] != null) {
                                exprList.Add(new PhonemeExpression {
                                    abbr = abbr,
                                    value = Convert.ToSingle(expr["value"]),
                                });
                            }
                            ei++;
                        }
                        if (exprList.Count > 0) p.expressions = exprList;
                    }

                    phonemes.Add(p);
                } else {
                    Log.Warning("LuaPhonemizer: entry #{Idx} is not a table in {Path}", idx, scriptPath);
                }
                idx++;
            }

            return new Result { phonemes = phonemes.ToArray() };
        }

        // ── Static discovery helper ──────────────────────────────────────────

        /// <summary>
        /// Loads a script, calls get_info(), and returns a factory descriptor.
        /// Returns null on any error.
        /// </summary>
        public static LuaPhonemizerFactory? TryLoadFactory(string scriptPath, string packageDir) {
            try {
                var lua = new Lua();
                using var sandbox = new LuaSandbox(lua);
                sandbox.AllowFileAccess(packageDir, "package", writeable: false);
                sandbox.Initialize();

                string code = File.ReadAllText(scriptPath);
                var chunk = lua.CompileChunk(code, Path.GetFileName(scriptPath), new LuaCompileOptions());

                sandbox.InjectOpenUtauApi(
                    tick => tick / 8.0, ms => (int)(ms * 8),
                    queryG2p: null);

                sandbox.DoChunk(chunk);

                if (sandbox.Env?["get_info"] == null) {
                    Log.Warning("LuaPhonemizer: {Path} missing get_info()", scriptPath);
                    return null;
                }

                LuaResult infoResult = ((dynamic)sandbox.Env!).get_info();
                var info = infoResult[0] as LuaTable;
                if (info == null) {
                    Log.Warning("LuaPhonemizer: get_info() in {Path} did not return a table", scriptPath);
                    return null;
                }

                string? name = info["name"]?.ToString();
                string? tag = info["tag"]?.ToString();
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(tag)) {
                    Log.Warning("LuaPhonemizer: get_info() in {Path} missing 'name' or 'tag'", scriptPath);
                    return null;
                }

                return new LuaPhonemizerFactory {
                    type = typeof(LuaPhonemizer),
                    name = name,
                    tag = tag,
                    author = info["author"]?.ToString() ?? "",
                    language = info["language"]?.ToString() ?? "",
                    scriptPath = scriptPath,
                    packageDir = packageDir,
                    scriptId = $"lua:{tag}",
                };
            } catch (Exception ex) {
                Log.Warning(ex, "LuaPhonemizer: failed to load factory from {Path}", scriptPath);
                return null;
            }
        }
    }
}

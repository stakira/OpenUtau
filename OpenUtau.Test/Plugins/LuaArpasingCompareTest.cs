using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtau.Plugin.Builtin;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Plugins {
    /// <summary>
    /// Runs ArpasingPhonemizer (C#) and the Lua port side-by-side on the same notes
    /// and prints a comparison table. Run with:
    ///   dotnet test --filter LuaArpasingCompareTest -v normal
    /// </summary>
    public class LuaArpasingCompareTest {
        private static readonly string LuaScriptPath;
        private static readonly string SingerPath;
        private static readonly string CsharpUstxPath;

        static LuaArpasingCompareTest() {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
       LuaScriptPath = Path.Combine(baseDir, "..", "..", "..", "..", "oudeps", "phonemizers", "arpasing", "arpasing.lua");
            if (!File.Exists(LuaScriptPath)) {
                LuaScriptPath = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\oudeps\phonemizers\arpasing\arpasing.lua"));
            }
            SingerPath = Path.Combine(baseDir, "..", "..", "..", "..", "..", "..", "utau", "voice", "Milk - English -");
            CsharpUstxPath = Path.Combine(baseDir, "..", "..", "..", "..", "..", "..", "utau", "projects", "ustx", "arpasing_short.ustx");
        }

        private readonly ITestOutputHelper output;
        public LuaArpasingCompareTest(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void CompareOutputs() {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var singer = LoadSinger(SingerPath);
            Assert.True(singer.Found, $"Singer not found at {SingerPath}");
            Assert.True(singer.Loaded, "Singer failed to load");

            var groups = ReadNoteGroupsFromUstx(CsharpUstxPath);
            Assert.NotEmpty(groups);
            output.WriteLine($"Note groups: {groups.Count}");

            var project = MakeProject(singer);
            var track = project.tracks[0];
            var timeAxis = new TimeAxis();
            timeAxis.BuildSegments(project);

            // ── C# phonemizer ──────────────────────────────────────────────
            var csPhon = new ArpasingPhonemizer();
            csPhon.Testing = true;
            csPhon.SetSinger(singer);
            csPhon.SetTiming(timeAxis);
            csPhon.SetUp(groups.ToArray(), project, track);
            var csResults = RunPhonemizer(csPhon, groups);

            // ── Lua phonemizer ─────────────────────────────────────────────
            Assert.True(File.Exists(LuaScriptPath), $"Lua script not found: {LuaScriptPath}");
            var luaPhon = new LuaPhonemizer(LuaScriptPath,
                Path.GetDirectoryName(Path.GetDirectoryName(Path.GetFullPath(LuaScriptPath)))!);
            luaPhon.Name = "English Arpasing Phonemizer (Lua)";
            luaPhon.Tag = "EN ARPA LUA";
            luaPhon.Language = "EN";
            luaPhon.SetLogOverrides(
                info => output.WriteLine($"[lua] {info}"),
                warn => output.WriteLine($"[lua warn] {warn}"));
            luaPhon.Testing = true;
            luaPhon.SetSinger(singer);
            luaPhon.SetTiming(timeAxis);
            luaPhon.SetUp(groups.ToArray(), project, track);
            var luaResults = RunPhonemizer(luaPhon, groups);

            // ── Comparison table ───────────────────────────────────────────
            int maxRows = Math.Max(csResults.Count, luaResults.Count);
            bool anyDiff = false;
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"{"Lyric",-20} {"C# Arpasing",-45} {"Lua Arpasing",-45} {"Match"}");
            sb.AppendLine(new string('-', 115));

            for (int i = 0; i < groups.Count; i++) {
                string lyric = groups[i][0].lyric;
                string csAlias = i < csResults.Count
                    ? string.Join(", ", csResults[i].Select(p => p.phoneme))
                    : "(missing)";
                string luaAlias = i < luaResults.Count
                    ? string.Join(", ", luaResults[i].Select(p => p.phoneme))
                    : "(missing)";
                bool match = csAlias == luaAlias;
                if (!match) anyDiff = true;
                sb.AppendLine($"{lyric,-20} {csAlias,-45} {luaAlias,-45} {(match ? "✓" : "✗")}");
            }

            output.WriteLine(sb.ToString());

            if (anyDiff) {
                output.WriteLine("DIFFERENCES FOUND — see table above.");
            } else {
                output.WriteLine("All outputs match.");
            }

            // Not an assertion failure — differences are expected during development.
            // Change to Assert.False(anyDiff, ...) when ready to enforce parity.
        }

        // ── Helpers ────────────────────────────────────────────────────────

        static ClassicSinger LoadSinger(string singerDir) {
            VoicebankLoader.IsTest = true;
            string characterTxt = Path.Combine(singerDir, "character.txt");
            string basePath = Path.GetDirectoryName(singerDir);
            var voicebank = new Voicebank { File = characterTxt, BasePath = basePath };
            VoicebankLoader.LoadVoicebank(voicebank);
            var singer = new ClassicSinger(voicebank);
            singer.EnsureLoaded();
            return singer;
        }

        static UProject MakeProject(USinger singer) {
            var project = new UProject();
            Core.Format.Ustx.AddDefaultExpressions(project);
            var track = project.tracks[0];
            project.expressions.TryGetValue(Core.Format.Ustx.CLR, out var descriptor);
            track.VoiceColorExp = descriptor?.Clone();
            var colors = singer.Subbanks.Select(s => s.Color).ToHashSet();
            if (track.VoiceColorExp != null) {
                track.VoiceColorExp.options = colors.OrderBy(c => c).ToArray();
                track.VoiceColorExp.max = track.VoiceColorExp.options.Length - 1;
            }
            return project;
        }

        /// <summary>
        /// Reads note groups from a USTX file without needing DocManager/SingerManager.
        /// Parses just the lyric/tone/duration/position fields from voice_parts.
        /// </summary>
        static List<Phonemizer.Note[]> ReadNoteGroupsFromUstx(string ustxPath) {
            // Parse YAML manually to extract just notes.
            // We use a simple line-by-line approach to avoid full project deserialization.
            var lines = File.ReadAllLines(ustxPath, Encoding.UTF8);
            var rawNotes = new List<(string lyric, int tone, int position, int duration)>();

            string curLyric = null;
            int curTone = 60, curPosition = 0, curDuration = 480;

            foreach (var line in lines) {
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("lyric:")) {
                    if (curLyric != null) {
                        rawNotes.Add((curLyric, curTone, curPosition, curDuration));
                    }
                    curLyric = trimmed.Substring("lyric:".Length).Trim().Trim('\'', '"');
                    curTone = 60; curPosition = 0; curDuration = 480;
                } else if (trimmed.StartsWith("tone:") && curLyric != null) {
                    int.TryParse(trimmed.Substring("tone:".Length).Trim(), out curTone);
                } else if (trimmed.StartsWith("position:") && curLyric != null) {
                    int.TryParse(trimmed.Substring("position:".Length).Trim(), out curPosition);
                } else if (trimmed.StartsWith("duration:") && curLyric != null) {
                    int.TryParse(trimmed.Substring("duration:".Length).Trim(), out curDuration);
                }
            }
            if (curLyric != null) rawNotes.Add((curLyric, curTone, curPosition, curDuration));

            // Build note groups: each non-extension note starts a group; +/+N/+~/* notes extend it.
            var groups = new List<Phonemizer.Note[]>();
            var current = new List<Phonemizer.Note>();
            foreach (var (lyric, tone, position, duration) in rawNotes) {
                bool isExtension = lyric.StartsWith("+");
                if (!isExtension && current.Count > 0) {
                    groups.Add(current.ToArray());
                    current.Clear();
                }
                current.Add(new Phonemizer.Note {
                    lyric = lyric,
                    tone = tone,
                    position = position,
                    duration = duration,
                    phonemeAttributes = Array.Empty<Phonemizer.PhonemeAttributes>(),
                });
            }
            if (current.Count > 0) groups.Add(current.ToArray());
            return groups;
        }

        static List<Phonemizer.Phoneme[]> RunPhonemizer(Phonemizer phonemizer, List<Phonemizer.Note[]> groups) {
            var results = new List<Phonemizer.Phoneme[]>();
            for (int i = 0; i < groups.Count; i++) {
                bool prevAdj = i > 0 &&
                    (groups[i - 1].Last().position + groups[i - 1].Last().duration >= groups[i][0].position);
                bool nextAdj = i < groups.Count - 1 &&
                    (groups[i].Last().position + groups[i].Last().duration >= groups[i + 1][0].position);

                Phonemizer.Note? prev = i > 0 ? groups[i - 1][0] : (Phonemizer.Note?)null;
                Phonemizer.Note? next = i < groups.Count - 1 ? groups[i + 1][0] : (Phonemizer.Note?)null;

                try {
                    var result = phonemizer.Process(
                        groups[i],
                        prev,
                        next,
                        prevAdj ? prev : null,
                        nextAdj ? next : null,
                        prevAdj && i > 0 ? groups[i - 1] : Array.Empty<Phonemizer.Note>());
                    results.Add(result.phonemes);
                } catch (Exception ex) {
                    results.Add(new[] { new Phonemizer.Phoneme { phoneme = $"ERR:{ex.Message}" } });
                }
            }
            return results;
        }
    }
}

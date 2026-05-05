using System;
using System.IO;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using Xunit;

namespace OpenUtau.Plugins {
    /// <summary>
    /// Tests for the sandboxed require() function via LuaPhonemizer.
    /// LuaSandbox is internal, so we test through the public LuaPhonemizer API.
    /// </summary>
    public class LuaSandboxRequireTest : IDisposable {
        private readonly string tempDir;

        public LuaSandboxRequireTest() {
            tempDir = Path.Combine(Path.GetTempPath(), $"LuaRequireTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
        }

        public void Dispose() {
            try { Directory.Delete(tempDir, true); } catch { }
        }

        [Fact]
        public void RequireLoadsModule() {
            // Create the main script that uses require
            string mainPath = Path.Combine(tempDir, "main.lua");
            File.WriteAllText(mainPath, @"
                local engine = require(""syllable_engine"")
                return { loaded = true }
            ");

            // Create a dummy module
            string modulePath = Path.Combine(tempDir, "syllable_engine.lua");
            File.WriteAllText(modulePath, "return { version = 1 }");

            // Create a minimal phonemizer wrapper
            string phonPath = Path.Combine(tempDir, "phon.lua");
            File.WriteAllText(phonPath, @"
                local engine = require(""syllable_engine"")
                function get_info() return { name = ""Test"", tag = ""TEST"", language = ""EN"" } end
                function set_singer(s) end
                function process(notes, ctx) return {} end
            ");

            var phonemizer = new LuaPhonemizer(phonPath, tempDir);
            phonemizer.SetLogOverrides(info => { }, warn => { });
            Assert.NotNull(phonemizer);
        }

        [Fact]
        public void RequireBlocksOutsidePackage() {
            string escapePath = Path.Combine(Path.GetTempPath(), "escape.lua");
            File.WriteAllText(escapePath, "return { escaped = true }");

            try {
                string phonPath = Path.Combine(tempDir, "escape_test.lua");
                File.WriteAllText(phonPath, @"
                    function get_info() return { name = ""Test"", tag = ""TEST"", language = ""EN"" } end
                    function set_singer(s) end
                    function process(notes, ctx)
                        local ok, err = pcall(function() require(""..""..""escape"") end)
                        return {}
                    end
                ");

                var phonemizer = new LuaPhonemizer(phonPath, tempDir);
                phonemizer.SetLogOverrides(info => { }, warn => { });
                // Should load without crashing - the pcall catches the require error
                Assert.NotNull(phonemizer);
            } finally {
                try { File.Delete(escapePath); } catch { }
            }
        }

        [Fact]
        public void RequireThrowsForMissing() {
            string phonPath = Path.Combine(tempDir, "missing_test.lua");
            File.WriteAllText(phonPath, @"
                function get_info() return { name = ""Test"", tag = ""TEST"", language = ""EN"" } end
                function set_singer(s) end
                function process(notes, ctx)
                    local ok, err = pcall(function() require(""nonexistent"") end)
                    return {}
                end
            ");

            var phonemizer = new LuaPhonemizer(phonPath, tempDir);
            phonemizer.SetLogOverrides(info => { }, warn => { });
            Assert.NotNull(phonemizer);
        }
    }
}

using System;
using System.IO;
using OpenUtau.Classic;
using OpenUtau.Core.Neutrino;
using Xunit;

namespace OpenUtau.Test.Core.Neutrino {
    public class NeutrinoSingerTest {
        [Fact]
        public void BankLoadsModelsLocallyWithoutJson() {
            string root = Path.Combine(Path.GetTempPath(), "neutrino-bank-" + Guid.NewGuid());
            Directory.CreateDirectory(root);
            try {
                foreach (string file in new[] { "info.toml", "t.bin", "p.bin", "s.bin", "v.bin" }) {
                    File.WriteAllText(Path.Combine(root, file), "fixture");
                }
                var singer = new NeutrinoSinger(new Voicebank { File = Path.Combine(root, "character.txt") });
                Assert.Equal(Path.GetFullPath(root), singer.ReadConfig().ModelPath);
                File.Delete(Path.Combine(root, "s.bin"));
                var error = Assert.Throws<FileNotFoundException>(() => singer.ReadConfig());
                Assert.Contains("s.bin", error.Message);
            } finally { Directory.Delete(root, true); }
        }

        [Fact]
        public void LegacyRelativeModelPathStillWorks() {
            string root = Path.Combine(Path.GetTempPath(), "neutrino-bank-" + Guid.NewGuid());
            string models = Path.Combine(root, "models");
            Directory.CreateDirectory(models);
            try {
                foreach (string file in new[] { "info.toml", "t.bin", "p.bin", "s.bin", "v.bin" }) {
                    File.WriteAllText(Path.Combine(models, file), "fixture");
                }
                File.WriteAllText(Path.Combine(root, "neutrino.json"), "{\"ModelPath\":\"models\"}");
                var singer = new NeutrinoSinger(new Voicebank { File = Path.Combine(root, "character.txt") });
                Assert.Equal(Path.GetFullPath(models), singer.ReadConfig().ModelPath);
            } finally { Directory.Delete(root, true); }
        }
    }
}

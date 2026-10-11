using System;
using System.IO;
using OpenUtau.Classic;
using OpenUtau.Core.Neutrino;
using Xunit;

namespace OpenUtau.Test.Core.Neutrino {
    public class NeutrinoSingerTest {
        static void CreateModel(string folder) {
            Directory.CreateDirectory(folder);
            foreach (var name in new[] { "info.toml", "t.bin", "p.bin", "s.bin", "v.bin" }) {
                File.WriteAllText(Path.Combine(folder, name), "fixture");
            }
        }

        [Fact]
        public void NestedArchiveFoldersAreFoundWithoutJson() {
            string root = Path.Combine(Path.GetTempPath(), "neutrino-bank-" + Guid.NewGuid());
            string models = Path.Combine(root, "archive", "library", "SOMA");
            try {
                CreateModel(models);
                var singer = new NeutrinoSinger(new Voicebank { File = Path.Combine(root, "character.txt") });
                Assert.Equal(Path.GetFullPath(models), singer.ReadConfig().ModelPath);
            } finally { Directory.Delete(root, true); }
        }

        [Fact]
        public void RootModelWinsOverNestedCopies() {
            string root = Path.Combine(Path.GetTempPath(), "neutrino-bank-" + Guid.NewGuid());
            try {
                CreateModel(root); CreateModel(Path.Combine(root, "backup"));
                Assert.Equal(root, NeutrinoSinger.FindModelFolder(root));
            } finally { Directory.Delete(root, true); }
        }

        [Fact]
        public void MultipleNestedModelsAreReportedInsteadOfPickingAVoice() {
            string root = Path.Combine(Path.GetTempPath(), "neutrino-bank-" + Guid.NewGuid());
            try {
                CreateModel(Path.Combine(root, "one")); CreateModel(Path.Combine(root, "two"));
                var error = Assert.Throws<InvalidDataException>(() => NeutrinoSinger.FindModelFolder(root));
                Assert.Contains(Path.Combine(root, "one"), error.Message);
                Assert.Contains(Path.Combine(root, "two"), error.Message);
            } finally { Directory.Delete(root, true); }
        }

        [Fact]
        public void FilesFromDifferentFoldersAreNeverCombined() {
            string root = Path.Combine(Path.GetTempPath(), "neutrino-bank-" + Guid.NewGuid());
            try {
                CreateModel(Path.Combine(root, "one")); CreateModel(Path.Combine(root, "two"));
                File.Delete(Path.Combine(root, "one", "s.bin"));
                File.Delete(Path.Combine(root, "two", "p.bin"));
                Assert.Throws<FileNotFoundException>(() => NeutrinoSinger.FindModelFolder(root));
            } finally { Directory.Delete(root, true); }
        }

        [Theory]
        [InlineData("ENUNU")]
        [InlineData("WORLDLINE-R")]
        public void OldProjectRendererIsReplacedForNeutrino(string previous) {
            var singer = new NeutrinoSinger(new Voicebank { File = "character.txt" });
            var track = new OpenUtau.Core.Ustx.UTrack { Singer = singer };
            var settings = new OpenUtau.Core.Ustx.URenderSettings { renderer = previous };
            settings.Validate(track);
            Assert.Equal("NEUTRINO", settings.renderer);
            Assert.IsType<NeutrinoRenderer>(settings.Renderer);
        }

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

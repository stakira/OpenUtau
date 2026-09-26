using System;
using System.IO;
using OpenUtau.Core.Enunu;
using Xunit;

namespace OpenUtau.Core {
    public class EnunuConfigTest : IDisposable {
        readonly string location = Path.Combine(Path.GetTempPath(), $"enunu-config-{Guid.NewGuid():N}");

        public EnunuConfigTest() {
            Directory.CreateDirectory(Path.Join(location, "model"));
        }

        public void Dispose() {
            try { Directory.Delete(location, true); } catch { }
        }

        [Fact]
        public void ModelFolderPathsAreRelativeToTheVoicebank() {
            // As SimpleEnunu voicebanks ship: config.yaml and qst.hed in model/, a copy of the table at the top.
            File.WriteAllText(Path.Join(location, "model", "config.yaml"), "feature_type: world\ntable_path: ./kana.table\n");
            File.WriteAllText(Path.Join(location, "model", "kana.table"), "");
            File.WriteAllText(Path.Join(location, "model", "qst.hed"), "");
            File.WriteAllText(Path.Join(location, "other.table"), "");
            var config = EnunuConfig.SetSimpleENUNUConfig(location);
            Assert.Equal(Path.Join("model", "kana.table"), config.tablePath);
            Assert.Equal(Path.Join("model", "qst.hed"), config.questionPath);
            Assert.True(File.Exists(Path.Join(location, config.tablePath)));
        }

        [Fact]
        public void FindsTheTableWhenConfigHasNone() {
            File.WriteAllText(Path.Join(location, "config.yaml"), "feature_type: world\n");
            File.WriteAllText(Path.Join(location, "kana.table"), "");
            File.WriteAllText(Path.Join(location, "qst.hed"), "");
            var config = EnunuConfig.SetSimpleENUNUConfig(location);
            Assert.Equal("kana.table", config.tablePath);
            Assert.Equal("qst.hed", config.questionPath);
        }
    }
}

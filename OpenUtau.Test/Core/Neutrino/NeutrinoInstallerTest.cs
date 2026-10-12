using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using OpenUtau.Classic;
using Xunit;

namespace OpenUtau.Test.Core.Neutrino {
    public class NeutrinoInstallerTest {
        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void SelectedNeutrinoTypeIsSavedForOfficialAndExistingBanks(bool hasYaml, bool hasCharacter) {
            string root = Path.Combine(Path.GetTempPath(), "neutrino-install-" + Guid.NewGuid());
            Directory.CreateDirectory(root);
            try {
                string zip = Path.Combine(root, "library.zip");
                using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create)) {
                    using (var writer = new StreamWriter(archive.CreateEntry("info.toml").Open())) { writer.Write("fixture"); }
                    if (hasCharacter) {
                        using var writer = new StreamWriter(archive.CreateEntry("character.txt").Open());
                        writer.Write("name=Fixture");
                    }
                    if (hasYaml) {
                        using var writer = new StreamWriter(archive.CreateEntry("character.yaml").Open());
                        writer.Write("singer_type: enunu\nname: PreservedName\ntext_file_encoding: utf-8\n");
                    }
                }
                string singers = Path.Combine(root, "Singers");
                new VoicebankInstaller(singers, (_, _) => {}, Encoding.UTF8, Encoding.UTF8).Install(zip, "neutrino");
                using var input = File.OpenRead(Path.Combine(singers, "library", "character.yaml"));
                var config = VoicebankConfig.Load(input);
                Assert.Equal("neutrino", config.SingerType);
                if (hasYaml) { Assert.Equal("PreservedName", config.Name); }
            } finally { Directory.Delete(root, true); }
        }
    }
}

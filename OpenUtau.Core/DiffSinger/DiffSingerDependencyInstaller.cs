using System;
using System.IO;
using System.Linq;
using System.Text;
using SharpCompress.Archives;

namespace OpenUtau.Core.DiffSinger {
    //Installation of Diffsinger voicebanks' dependencies, including vocoder and phoneme timing model
    [Serializable]
    public class DependencyConfig {
        public string name = "vocoder";
    }

    public class DiffSingerDependencyInstaller {
        public static string FileExt = ".dsvocoder";
        public static void Install(string archivePath) {
            DependencyConfig dependencyConfig;
            using (var archive = ArchiveFactory.Open(archivePath)) {
                DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, "Installing dependency"));
                var configEntry = archive.Entries.First(e => e.Key == "vocoder.yaml");
                if (configEntry == null) {
                    throw new ArgumentException("missing vocoder.yaml");
                }
                using (var stream = configEntry.OpenEntryStream()) {
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    dependencyConfig = Core.Yaml.DefaultDeserializer.Deserialize<DependencyConfig>(reader);
                }
                string name = dependencyConfig.name;
                var basePath = Path.Combine(PathManager.Inst.DependencyPath, name);
                foreach (var entry in archive.Entries) {
                    if (entry.Key.Contains("..")) {
                        // Prevent zipSlip attack
                        continue;
                    }
                    var filePath = Path.Combine(basePath, entry.Key);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    if (!entry.IsDirectory) {
                        entry.WriteToFile(Path.Combine(basePath, entry.Key));
                    }
                }
                DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, $"dependency \"{name}\" installaion finished"));
            }
        }
    }
}

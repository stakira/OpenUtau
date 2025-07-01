using System;
using System.IO;
using System.Linq;
using System.Text;
using SharpCompress.Archives;

namespace OpenUtau.Core {
    //Installation of dependencies (primarilly for diffsinger), including vocoder and phoneme timing model
    [Serializable]
    public class DependencyConfig {
        public string name;
    }

    public class DependencyInstaller {
        public static string FileExt = ".oudep";
        public static void Install(string archivePath) {
            DependencyConfig dependencyConfig;
            using (var archive = ArchiveFactory.Open(archivePath)) {
                DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, "Installing dependency"));
                var configEntry = archive.Entries.First(e => e.Key == "oudep.yaml");
                if (configEntry == null) {
                    throw new ArgumentException("missing oudep.yaml");
                }
                using (var stream = configEntry.OpenEntryStream()) {
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    dependencyConfig = Core.Yaml.DefaultDeserializer.Deserialize<DependencyConfig>(reader);
                }
                string name = dependencyConfig.name;
                if(string.IsNullOrEmpty(name)){
                    throw new ArgumentException("missing name in oudep.yaml");
                }
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
                DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, $"Installed dependency \"{name}\""));
            }
        }
    }
}

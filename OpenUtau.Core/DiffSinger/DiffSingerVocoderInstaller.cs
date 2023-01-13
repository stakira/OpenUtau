using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using OpenUtau.Core.Render;
using SharpCompress.Archives;

namespace OpenUtau.Core.DiffSinger {
    public class DiffSingerVocoderInstaller {
        public static string FileExt = ".dsvocoder";
        public static void Install(string archivePath) {
            DsVocoderConfig vocoderConfig;
            using (var archive = ArchiveFactory.Open(archivePath)) {
                DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, "Installing vocoder"));
                var configEntry = archive.Entries.First(e => e.Key == "vocoder.yaml");
                if (configEntry == null) {
                    throw new ArgumentException("missing vocoder.yaml");
                }
                using (var stream = configEntry.OpenEntryStream()) {
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    vocoderConfig = Core.Yaml.DefaultDeserializer.Deserialize<DsVocoderConfig>(reader);
                }
                string name = vocoderConfig.name;
                var basePath = Path.Combine(PathManager.Inst.VocodersPath, name);
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
                DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, $"vocoder \"{name}\" installaion finished"));
            }
        }
    }
}

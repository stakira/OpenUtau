using Ignore;
using System;
using System.Collections.Generic;
using System.IO;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Writers;
using System.Linq;

using OpenUtau.Core.Ustx;

namespace OpenUtau.Classic {
    public class VoicebankPublisher {
        private readonly Action<double, string> progress;
        private readonly Ignore.Ignore? ignore;

        public VoicebankPublisher(Action<double, string> progress, string? gitIgnore) {
            this.progress = progress;
            if(gitIgnore != null) {
                ignore = new Ignore.Ignore();
                ignore.Add(gitIgnore.Split("\n"));
            }
        }

        private static void ModifyConfig(USinger singer, Action<VoicebankConfig> modify) {
            var yamlFile = Path.Combine(singer.Location, "character.yaml");
            VoicebankConfig? config = null;
            if (File.Exists(yamlFile)) {
                using (var stream = File.OpenRead(yamlFile)) {
                    config = VoicebankConfig.Load(stream);
                }
            }
            if (config == null) {
                config = new VoicebankConfig();
            }
            modify(config);
            using (var stream = File.Open(yamlFile, FileMode.Create)) {
                config.Save(stream);
            }
        }

        private bool IsIgnored(string relativePath){
            return ignore?.IsIgnored(relativePath) ?? false;
        }

        private List<string> GetFilesToPack(string singerPath)
        {
            List<string> fileList = Directory.EnumerateFiles(singerPath, "*.*", SearchOption.AllDirectories).ToList();
            List<string> packList = fileList.FindAll(x => !(IsIgnored(System.IO.Path.GetRelativePath(singerPath, x))));
            return packList;
        }

        public void Publish(USinger singer, string outputPath){
            ///<summary>
            ///Compress a voicebank into an optimized zip archive for distribution.
            ///This function only supports voicebanks that follow the classic packaging model,
            ///including utau, enunu and diffsinger.
            ///Vogen voicebanks aren't supported.
            ///</summary>
            var location = singer.Location;
            if(!Directory.Exists(location)){
                return;
            }
            progress.Invoke(0, $"Publishing {singer.Name}");
            //Write singer type into character.yaml
            try {
                ModifyConfig(singer, config => config.SingerType = singer.SingerType.ToString().ToLower());
            } catch (Exception e) {  }
            var packList = GetFilesToPack(location);
            int index = 0;
            int fileCount = packList.Count();
            var options = new WriterOptions(compressionType: CompressionType.Deflate);
            options.ArchiveEncoding = new ArchiveEncoding{
                Forced = System.Text.Encoding.UTF8
            };
            using(var archive = ZipArchive.Create())
            {
                foreach (var absFilePath in packList)
                {
                    index++;
                    progress.Invoke(100.0 * index / fileCount, $"Compressing {absFilePath}");
                    string reFilePath = Path.GetRelativePath(location, absFilePath);
                    using(var inputStream = File.OpenRead(absFilePath)){
                        archive.AddEntry(reFilePath, inputStream);
                    }
                }
                using(var outputStream = File.OpenWrite(outputPath)){
                    archive.SaveTo(outputStream, options);
                }
            }
            progress.Invoke(0, $"Published {singer.Name} to {outputPath}");
        }
    }
}

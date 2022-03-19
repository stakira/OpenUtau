using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using OpenUtau.Core.Ustx;
using Serilog;
using SharpCompress.Archives;

namespace OpenUtau.Core.Vogen {
    [Serializable]
    class VogenMeta {
        public string name;
        public string id;
        public string version;
        public string builtBy;
        public string voiceBy;
    }

    class VogenSingerLoader {
        readonly string basePath;

        public static IEnumerable<USinger> FindAllSingers() {
            List<USinger> singers = new List<USinger>();
            foreach (var path in new string[] {
                PathManager.Inst.SingersPathOld,
                PathManager.Inst.SingersPath,
                PathManager.Inst.AdditionalSingersPath,
            }) {
                var loader = new VogenSingerLoader(path);
                singers.AddRange(loader.SearchAll());
            }
            return singers;
        }

        public VogenSingerLoader(string basePath) {
            this.basePath = basePath;
        }

        public IEnumerable<USinger> SearchAll() {
            var result = new List<USinger>();
            if (!Directory.Exists(basePath)) {
                return result;
            }
            result.AddRange(Directory.EnumerateFiles(basePath, "*.vogeon", SearchOption.AllDirectories)
                .Select(filePath => {
                    try {
                        return LoadSinger(filePath);
                    } catch (Exception e) {
                        Log.Error(e, "Failed to load Vogen singer.");
                        return null;
                    }
                })
                .OfType<USinger>());
            return result;
        }

        public USinger LoadSinger(string filePath) {
            VogenMeta meta;
            byte[] model;
            using (var archive = ArchiveFactory.Open(filePath)) {
                var metaEntry = archive.Entries.First(e => e.Key == "meta.json");
                if (metaEntry == null) {
                    throw new ArgumentException("missing meta.json");
                }
                using (var stream = metaEntry.OpenEntryStream()) {
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    JsonSerializer serializer = new JsonSerializer();
                    meta = (VogenMeta)serializer.Deserialize(reader, typeof(VogenMeta));
                }
                var modelEntry = archive.Entries.FirstOrDefault(e => e.Key == "model.onnx");
                if (metaEntry == null) {
                    throw new ArgumentException("missing model.onnx");
                }
                using (var stream = modelEntry.OpenEntryStream()) {
                    using var mem = new MemoryStream();
                    stream.CopyTo(mem);
                    model = mem.ToArray();
                }
            }
            return new VogenSinger(filePath, meta, model);
        }
    }
}

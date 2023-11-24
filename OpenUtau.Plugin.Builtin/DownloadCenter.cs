using System;
using System.IO;
using System.Text;
using OpenUtau.Core;

namespace OpenUtau.Plugin.Builtin {
    public class DownloadCenterManager {
        public Categories categories;
        public DownloadCenterManager() {
            var yamlFile = Path.Combine(PathManager.Inst.PluginsPath, "download center.yaml");
            if (File.Exists(yamlFile)) {
                using (var stream = File.OpenRead(yamlFile)) {
                    categories = Load(stream);
                }
            }
            else {
                Directory.CreateDirectory(PathManager.Inst.PluginsPath);
                File.WriteAllBytes(yamlFile, Data.Resources.download_center_template);
                using (var stream = File.OpenRead(yamlFile)) {
                    categories = Load(stream);
                }
            }
        }

        private static Categories Load(Stream stream) {
            using (var reader = new StreamReader(stream, Encoding.UTF8)) {
                // TODO FIx bug
                var categoriesConfig = Yaml.DefaultDeserializer.Deserialize<Categories>(reader); 
                return categoriesConfig;
            }
        }
    }

    public class Categories {
        public string[] categories;
        public string[] dependencies;

        public Categories(string[] categories, string[] dependencies) {
            this.categories = categories;
            this.dependencies = dependencies;
        }
    }
}
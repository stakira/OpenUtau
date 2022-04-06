using System.IO;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Enunu {
    class EnunuConfig {
        public string tablePath;
        public int sampleRate;
        public double framePeriod;

        public static EnunuConfig Load(USinger singer) {
            var configPath = Path.Join(singer.Location, "enuconfig.yaml");
            var configTxt = File.ReadAllText(configPath);
            return Yaml.DefaultDeserializer.Deserialize<EnunuConfig>(configTxt);
        }
    }
}

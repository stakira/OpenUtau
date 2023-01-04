using System.IO;
using System.Text;
namespace OpenUtau.Core.DiffSinger {
    public class DsVocoder {
        public string Location;
        public DsVocoderConfig config;
        public byte[] model = new byte[0];

        //通过名称获取声码器
        public DsVocoder(string name) {
            Location = Path.Combine(PathManager.Inst.VocodersPath,name);
            config = Core.Yaml.DefaultDeserializer.Deserialize<DsVocoderConfig>(
                File.ReadAllText(Path.Combine(Location, "vocoder.yaml"),
                    System.Text.Encoding.UTF8));
        }

        public byte[] getModel() {
            if (model.Length == 0) {
                model = File.ReadAllBytes(Path.Combine(Location, config.model));
            }
            return model;
        }

        public float frameMs() {
            return 1000f * config.hop_size / config.sample_rate;
        }
    }

    
}

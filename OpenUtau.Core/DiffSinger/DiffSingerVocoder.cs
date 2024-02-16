using System;
using System.IO;
using Microsoft.ML.OnnxRuntime;

namespace OpenUtau.Core.DiffSinger {
    public class DsVocoder : IDisposable {
        public string Location;
        public DsVocoderConfig config;
        public InferenceSession session;

        public int num_mel_bins => config.num_mel_bins;
        public int hop_size => config.hop_size;
        public int sample_rate => config.sample_rate;

        //Get vocoder by package name
        public DsVocoder(string name) {
            byte[] model;
            try {
                Location = Path.Combine(PathManager.Inst.DependencyPath, name);
                config = Core.Yaml.DefaultDeserializer.Deserialize<DsVocoderConfig>(
                    File.ReadAllText(Path.Combine(Location, "vocoder.yaml"),
                        System.Text.Encoding.UTF8));
                model = File.ReadAllBytes(Path.Combine(Location, config.model));
            }
            catch (Exception ex) {
                throw new Exception($"Error loading vocoder {name}. Please download vocoder from https://github.com/xunmengshe/OpenUtau/wiki/Vocoders");
            }
            session = Onnx.getInferenceSession(model);
        }

        public float frameMs() {
            return 1000f * config.hop_size / config.sample_rate;
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    session?.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }

    [Serializable]
    public class DsVocoderConfig {
        public string name = "vocoder";
        public string model = "model.onnx";
        public int num_mel_bins = 128;
        public int hop_size = 512;
        public int sample_rate = 44100;
    }
}

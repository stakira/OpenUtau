using System;
using System.IO;
using System.Text.Json;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Neutrino {
    // The wrapper supplies bank metadata; audio and phoneme timing come from NEUTRINO.
    public class NeutrinoSinger : ClassicSinger {
        public override USingerType SingerType => USingerType.Neutrino;
        public override string DefaultPhonemizer => typeof(DefaultPhonemizer).FullName!;

        public NeutrinoSinger(Voicebank bank) : base(bank) { }

        public override bool TryGetOto(string phoneme, out UOto oto) {
            oto = UOto.OfDummy(phoneme);
            return !string.IsNullOrWhiteSpace(phoneme);
        }
        public override bool TryGetMappedOto(string phoneme, int tone, out UOto oto)
            => TryGetOto(phoneme, out oto);
        public override bool TryGetMappedOto(string phoneme, int tone, string color, out UOto oto)
            => TryGetOto(phoneme, out oto);

        public NeutrinoConfig ReadConfig() {
            var path = Path.Combine(Location, "neutrino.json");
            // A self-contained bank keeps info.toml and the four ONNX models next to character.yaml.
            // Optional legacy configuration can still point to an external model folder.
            var config = File.Exists(path)
                ? JsonSerializer.Deserialize<NeutrinoConfig>(File.ReadAllText(path))
                    ?? throw new InvalidDataException($"Invalid NEUTRINO configuration: {path}")
                : new NeutrinoConfig();
            config.ModelPath = Path.GetFullPath(string.IsNullOrWhiteSpace(config.ModelPath) ? "." : config.ModelPath, Location);
            if (!File.Exists(Path.Combine(config.ModelPath, "info.toml"))) {
                throw new FileNotFoundException("NEUTRINO model info.toml was not found in the singer folder.");
            }
            foreach (string model in new[] { "t.bin", "p.bin", "s.bin", "v.bin" }) {
                if (!File.Exists(Path.Combine(config.ModelPath, model))) { throw new FileNotFoundException($"NEUTRINO Tau ONNX model was not found: {Path.Combine(config.ModelPath, model)}"); }
            }
            return config;
        }
    }

    public class NeutrinoConfig {
        public string EnginePath { get; set; } = "";
        public string ModelPath { get; set; } = "";
    }
}

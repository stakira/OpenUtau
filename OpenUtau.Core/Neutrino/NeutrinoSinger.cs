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
            var config = JsonSerializer.Deserialize<NeutrinoConfig>(File.ReadAllText(path))
                ?? throw new InvalidDataException($"Invalid NEUTRINO configuration: {path}");
            config.EnginePath = Path.GetFullPath(config.EnginePath, Location);
            config.ModelPath = Path.GetFullPath(config.ModelPath, Location);
            if (!File.Exists(Path.Combine(config.EnginePath, "bin", "neutrino_client.exe")) ||
                !File.Exists(Path.Combine(config.EnginePath, "bin", "neutrino_server.exe")) ||
                !File.Exists(Path.Combine(config.EnginePath, "bin", "musicXMLtoLabel.exe"))) {
                throw new FileNotFoundException("NEUTRINO Tau 3.2.5.ex bin executables were not found. Check EnginePath in neutrino.json.");
            }
            if (!File.Exists(Path.Combine(config.ModelPath, "info.toml"))) {
                throw new FileNotFoundException("NEUTRINO model info.toml was not found. Check ModelPath in neutrino.json.");
            }
            return config;
        }
    }

    public class NeutrinoConfig {
        public string EnginePath { get; set; } = "";
        public string ModelPath { get; set; } = "";
    }
}

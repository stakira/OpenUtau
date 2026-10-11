using System;
using System.IO;
using System.Linq;
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
            // Banks may keep models at the root or inside nested archive folders.
            // Optional legacy configuration can still point to an external model folder.
            var config = File.Exists(path)
                ? JsonSerializer.Deserialize<NeutrinoConfig>(File.ReadAllText(path))
                    ?? throw new InvalidDataException($"Invalid NEUTRINO configuration: {path}")
                : new NeutrinoConfig();
            config.ModelPath = string.IsNullOrWhiteSpace(config.ModelPath)
                ? FindModelFolder(Location)
                : Path.GetFullPath(config.ModelPath, Location);
            if (!File.Exists(Path.Combine(config.ModelPath, "info.toml"))) {
                throw new FileNotFoundException("NEUTRINO model info.toml was not found in the singer folder.");
            }
            foreach (string model in new[] { "t.bin", "p.bin", "s.bin", "v.bin" }) {
                if (!File.Exists(Path.Combine(config.ModelPath, model))) { throw new FileNotFoundException($"NEUTRINO Tau ONNX model was not found: {Path.Combine(config.ModelPath, model)}"); }
            }
            return config;
        }

        static readonly string[] modelFiles = { "info.toml", "t.bin", "p.bin", "s.bin", "v.bin" };

        internal static string FindModelFolder(string singerFolder) {
            string root = Path.GetFullPath(singerFolder);
            bool IsComplete(string folder) => modelFiles.All(name => File.Exists(Path.Combine(folder, name)));
            // Prefer an explicitly installed root model over optional copies in subfolders.
            if (IsComplete(root)) { return root; }
            var options = new EnumerationOptions {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint,
            };
            var candidates = Directory.EnumerateFiles(root, "info.toml", options)
                .Select(file => Path.GetDirectoryName(file)!)
                .Where(IsComplete).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(folder => folder, StringComparer.OrdinalIgnoreCase).ToArray();
            if (candidates.Length == 1) { return candidates[0]; }
            if (candidates.Length > 1) {
                throw new InvalidDataException("Multiple NEUTRINO models were found in this singer folder. Keep one model or set ModelPath in neutrino.json: " + string.Join(", ", candidates));
            }
            // Keep validation's precise missing-file error for incomplete root models.
            if (File.Exists(Path.Combine(root, "info.toml"))) { return root; }
            throw new FileNotFoundException($"No complete NEUTRINO model was found in {root} or its subfolders. Keep info.toml, t.bin, p.bin, s.bin and v.bin together in one folder.");
        }
    }

    public class NeutrinoConfig {
        public string EnginePath { get; set; } = "";
        public string ModelPath { get; set; } = "";
    }
}

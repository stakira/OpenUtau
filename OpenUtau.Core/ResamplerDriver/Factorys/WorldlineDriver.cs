using System.IO;
using NAudio.Wave;
using OpenUtau.Core.SignalChain;
using Serilog;

namespace OpenUtau.Core.ResamplerDriver.Factorys {
    internal class WorldlineDriver : DriverModels, IResamplerDriver {
        public string Name => "worldline";
        public string FilePath { get; private set; }

        public WorldlineDriver() {
            string ext = OS.IsWindows() ? ".dll" : OS.IsMacOS() ? ".dylib" : ".so";
            FilePath = Path.Join(PathManager.Inst.RootPath, Name + ext);
        }

        public float[] DoResampler(EngineInput args, ILogger logger) {
            return Worldline.Resample(args, logger);
        }

        public string DoResamplerReturnsFile(EngineInput args, ILogger logger) {
            var samples = DoResampler(args, logger);
            var source = new WaveSource(0, 0, 0, 1);
            source.SetSamples(samples);
            WaveFileWriter.CreateWaveFile16(args.outputWaveFile, new ExportAdapter(source).ToMono(1, 0));
            return args.outputWaveFile;
        }

        public void CheckPermissions() { }

        public override string ToString() => Name;
    }
}

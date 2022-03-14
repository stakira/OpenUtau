using Serilog;

namespace OpenUtau.Classic {
    public interface IResampler {
        string Name { get; }
        string FilePath { get; }
        float[] DoResampler(ResamplerItem args, ILogger logger);
        string DoResamplerReturnsFile(ResamplerItem args, ILogger logger);
        void CheckPermissions();
    }
}

﻿using Serilog;

namespace OpenUtau.Classic {
    public interface IResampler {
        string FilePath { get; }
        float[] DoResampler(ResamplerItem args, ILogger logger);
        string DoResamplerReturnsFile(ResamplerItem args, ILogger logger);
        void CheckPermissions();
        ResamplerManifest Manifest { get; }
        bool SupportsFlag(string abbr);
    }
}

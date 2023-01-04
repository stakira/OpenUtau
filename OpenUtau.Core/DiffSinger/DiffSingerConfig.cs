using System;
using System.Collections.Generic;
using System.Text;

namespace OpenUtau.Core.DiffSinger {
    [Serializable]
    public class DsConfig {
        public string phonemes = "phonemes.txt";
        public string acoustic = "acoustic.onnx";
        public string vocoder = "nsf_hifigan";
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

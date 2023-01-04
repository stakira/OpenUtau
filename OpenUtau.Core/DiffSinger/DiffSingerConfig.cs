using System;
using System.Collections.Generic;
using System.Text;

namespace OpenUtau.Core.DiffSinger {
    [Serializable]
    public class DiffSingerConfig {
        
        public string dictionary = "dsdict.txt";
        public int reserved_tokens = 0;
        
        public string acoustic = "acoustic.onnx";
        public int speedup = 10;

        public string vocoder = "vocoder.onnx";
        public int num_mel_bins = 128;
        public int hop_size = 512;
        public int sample_rate = 44100;
        
        public float frameMs() {
            return 1000f * this.hop_size / this.sample_rate;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace OpenUtau.Core.DiffSinger {

    [Serializable]
    public class RandomPitchShifting {
        public float[] range;
    }

    [Serializable]
    public class AugmentationArgs {
        public RandomPitchShifting randomPitchShifting;
    }

    [Serializable]
    public class DsConfig {
        public string phonemes = "phonemes.txt";
        public string acoustic;
        public string vocoder;
        public List<string> speakers;
        public int hiddenSize = 256;
        public bool useKeyShiftEmbed = false;
        public bool useSpeedEmbed = false;
        public bool useEnergyEmbed = false;
        public bool useBreathinessEmbed= false;
        public AugmentationArgs augmentationArgs;
        public string dur;
        public string linguistic;
        public string pitch;
        public string variance;
        public int hop_size = 512;
        public int sample_rate = 44100;
        public bool predict_dur = true;
        public float frameMs(){
            return 1000f * hop_size / sample_rate;
        }
    }
}

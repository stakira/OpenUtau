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
        public bool useKeyShiftEmbed = false;
        public AugmentationArgs augmentationArgs;
    }
}

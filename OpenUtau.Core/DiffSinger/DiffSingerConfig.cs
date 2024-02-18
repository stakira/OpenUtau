using System;
using System.Collections.Generic;

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
        public bool useBreathinessEmbed = false;
        public bool useVoicingEmbed = false;
        public bool useTensionEmbed = false;
        public AugmentationArgs augmentationArgs;
        public bool useShallowDiffusion = false;
        public int maxDepth = -1;
        public string dur;
        public string linguistic;
        public string pitch;
        public string variance;
        public int hop_size = 512;
        public int sample_rate = 44100;
        public bool predict_dur = true;
        public bool predict_energy = true;
        public bool predict_breathiness = true;
        public bool predict_voicing = false;
        public bool predict_tension = false;
        public bool use_expr = false;
        public bool use_note_rest = false;
        public float frameMs(){
            return 1000f * hop_size / sample_rate;
        }
    }
}

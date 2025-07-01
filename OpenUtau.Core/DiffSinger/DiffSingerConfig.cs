using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

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
        public string languages;
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
        public bool useContinuousAcceleration = false;
        public bool use_lang_id = false;
        [YamlMember(Alias = "use_shallow_diffusion")] public bool? _useShallowDiffusion;
        [YamlMember(Alias = "use_variable_depth")] public bool? _useVariableDepth;
        [YamlIgnore]
        public bool useVariableDepth {
            get {
                // coalesce _useDepth and _useShallowDiffusion
                if (_useVariableDepth.HasValue) {
                    return _useVariableDepth.Value;
                }
                if (_useShallowDiffusion.HasValue) {
                    return _useShallowDiffusion.Value;
                }
                return false;
            }
        }
        [YamlMember(Alias = "max_depth")] public double _maxDepth;
        [YamlIgnore] public double maxDepth => useContinuousAcceleration ? _maxDepth : _maxDepth / 1000.0;
        public string dur;
        public string linguistic;
        public string pitch;
        public string variance;
        public bool predict_dur = true;
        public bool predict_energy = true;
        public bool predict_breathiness = true;
        public bool predict_voicing = false;
        public bool predict_tension = false;
        public bool use_expr = false;
        public bool use_note_rest = false;
        public int sample_rate = 44100;
        public int hop_size = 512;
        public int win_size = 2048;
        public int fft_size = 2048;
        public int num_mel_bins = 128;
        public double mel_fmin = 40;
        public double mel_fmax = 16000;
        public string mel_base = "10";  // or "e"
        public string mel_scale = "slaney";  // or "htk"

        public float frameMs() {
            return 1000f * hop_size / sample_rate;
        }
    }
}

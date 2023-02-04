using System.Collections.Generic;
using System.IO;
using OpenUtau.Core;

//Instead of using Enunu/EnunuConfig.cs, we created a copy to add EnunuOnnx-specific features
//without potentially breaking the existing Enunu Phonemizers and renderers.
namespace OpenUtau.Plugin.Builtin.EnunuOnnx {
    class EnunuConfig {
        public string tablePath;
        public string questionPath;
        public int sampleRate;
        public double framePeriod;
        public string modelDir;
        public string statsDir;
        public EnunuExtensions extensions;
        public EnunuDurationConfig duration;
        public EnunuTimelagConfig timelag;

        public static EnunuConfig Load(string configPath) {
            var configTxt = File.ReadAllText(configPath);
            RawEnunuConfig config = Yaml.DefaultDeserializer.Deserialize<RawEnunuConfig>(configTxt);
            return config.Convert();
        }
    }

    class EnunuExtensions {
        public List<string> ust_editor = new List<string>();
        public List<string> ust_converter = new List<string>();
        public List<string> score_editor = new List<string>();
        public List<string> timing_calculator = new List<string>();
        public List<string> timing_editor = new List<string>();
        public List<string> acoustic_calculator = new List<string>();
        public List<string> acoustic_editor = new List<string>();
        public List<string> wav_synthesizer = new List<string>();
        public List<string> wav_editor = new List<string>();
    }

    class RawEnunuExtensions {
        public object ust_editor;
        public object ust_converter;
        public object score_editor;
        public object timing_calculator;
        public object timing_editor;
        public object acoustic_calculator;
        public object acoustic_editor;
        public object wav_synthesizer;
        public object wav_editor;
    }

    class EnunuTimelagConfig {
        public string checkpoint;
        public List<int> allowedRange;
        public List<int> allowedRangeRest;
    }
    class EnunuDurationConfig {
        public string checkpoint;

    }

    class RawEnunuConfig {
        public string tablePath;
        public string questionPath;
        public int sampleRate;
        public double framePeriod;
        public string modelDir;
        public string statsDir;
        public RawEnunuExtensions extensions;
        public EnunuDurationConfig duration;
        public EnunuTimelagConfig timelag;

        public EnunuConfig Convert() {
            EnunuConfig enunuConfig = new EnunuConfig();
            enunuConfig.tablePath = this.tablePath;
            enunuConfig.questionPath = this.questionPath;
            enunuConfig.sampleRate = this.sampleRate;
            enunuConfig.framePeriod = this.framePeriod;
            enunuConfig.duration = this.duration;
            enunuConfig.modelDir = this.modelDir;
            enunuConfig.statsDir = this.statsDir;
            enunuConfig.timelag = this.timelag;
            enunuConfig.extensions = new EnunuExtensions();
            ParseEnunuExtension(enunuConfig.extensions.ust_editor, this.extensions.ust_editor);
            ParseEnunuExtension(enunuConfig.extensions.ust_converter, this.extensions.ust_converter);
            ParseEnunuExtension(enunuConfig.extensions.score_editor, this.extensions.score_editor);
            ParseEnunuExtension(enunuConfig.extensions.timing_calculator, this.extensions.timing_calculator);
            ParseEnunuExtension(enunuConfig.extensions.timing_editor, this.extensions.timing_editor);
            ParseEnunuExtension(enunuConfig.extensions.acoustic_calculator, this.extensions.acoustic_calculator);
            ParseEnunuExtension(enunuConfig.extensions.acoustic_editor, this.extensions.acoustic_editor);
            ParseEnunuExtension(enunuConfig.extensions.wav_synthesizer, this.extensions.wav_synthesizer);
            ParseEnunuExtension(enunuConfig.extensions.wav_editor, this.extensions.wav_editor);
            return enunuConfig;
        }

        private void ParseEnunuExtension(List<string> enunuExtension, object rawEnunuExtension) {
            if (rawEnunuExtension is string str) {
                enunuExtension.Add(str);
            } else if (rawEnunuExtension is List<object> list) {
                foreach (object o in list) {
                    if (o is string s) {
                        enunuExtension.Add(s);
                    }
                }
            }
        }
    }
}

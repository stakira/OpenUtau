using System.Collections.Generic;
using System.IO;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Enunu {
    class EnunuConfig {
        public string tablePath;
        public string questionPath;
        public int sampleRate;
        public double framePeriod;
        public EnunuExtensions extensions;

        public static EnunuConfig Load(USinger singer) {
            var configPath = Path.Join(singer.Location, "enuconfig.yaml");
            var config = new RawEnunuConfig();
            if (File.Exists(configPath)) {
                var configTxt = File.ReadAllText(configPath);
                config = Yaml.DefaultDeserializer.Deserialize<RawEnunuConfig>(configTxt);
            } else {
                config = SetSimpleENUNUConfig(singer.Location);
            }
            return config.Convert();
        }

        public static RawEnunuConfig SetSimpleENUNUConfig(string location) {
            string[] modelPaths = new string[] { location, location + @"\model" };
            string configYaml = "config.yaml";
            var config = new RawEnunuConfig();
            foreach (string modelPath in modelPaths) {
                if (File.Exists(Path.Join(modelPath, configYaml))) {
                    var configTxt = File.ReadAllText(Path.Join(modelPath, configYaml));
                    config = Yaml.DefaultDeserializer.Deserialize<RawEnunuConfig>(configTxt);
                    IEnumerable<string> files = Directory.EnumerateFiles(location, "*", SearchOption.TopDirectoryOnly);
                    foreach (string f in files) {
                        if (f.EndsWith(".table")) {
                            config.tablePath = Path.GetRelativePath(modelPath, f);
                        }
                        if (f.EndsWith(".hed")) {
                            config.questionPath = Path.GetRelativePath(modelPath, f);
                        }
                    }
                }
            }
            return config;
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

    class RawEnunuConfig {
        public string tablePath;
        public string questionPath;
        public int sampleRate;
        public double framePeriod;
        public RawEnunuExtensions extensions;

        public EnunuConfig Convert() {
            EnunuConfig enunuConfig = new EnunuConfig();
            enunuConfig.tablePath = this.tablePath;
            enunuConfig.questionPath = this.questionPath;
            enunuConfig.sampleRate = this.sampleRate;
            enunuConfig.framePeriod = this.framePeriod;
            enunuConfig.extensions = new EnunuExtensions();
            if (this.extensions != null) {
                ParseEnunuExtension(enunuConfig.extensions.ust_editor, this.extensions.ust_editor);
                ParseEnunuExtension(enunuConfig.extensions.ust_converter, this.extensions.ust_converter);
                ParseEnunuExtension(enunuConfig.extensions.score_editor, this.extensions.score_editor);
                ParseEnunuExtension(enunuConfig.extensions.timing_calculator, this.extensions.timing_calculator);
                ParseEnunuExtension(enunuConfig.extensions.timing_editor, this.extensions.timing_editor);
                ParseEnunuExtension(enunuConfig.extensions.acoustic_calculator, this.extensions.acoustic_calculator);
                ParseEnunuExtension(enunuConfig.extensions.acoustic_editor, this.extensions.acoustic_editor);
                ParseEnunuExtension(enunuConfig.extensions.wav_synthesizer, this.extensions.wav_synthesizer);
                ParseEnunuExtension(enunuConfig.extensions.wav_editor, this.extensions.wav_editor);
            }
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

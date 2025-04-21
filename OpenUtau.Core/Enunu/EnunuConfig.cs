using System.Collections.Generic;
using System.IO;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Enunu {
    class EnunuConfig {
        public string enunu_type = string.Empty;

        public string feature_type = string.Empty;
        public string tablePath = string.Empty;
        public string questionPath = string.Empty;
        public int sampleRate;
        public double framePeriod;
        public bool unLoadSubBanks = false;
        public EnunuExtensions extensions;

        public static EnunuConfig Load(USinger singer) {
            var configPath = Path.Join(singer.Location, "enuconfig.yaml");
            var config = new RawEnunuConfig();
            if (File.Exists(configPath)) {
                var configTxt = File.ReadAllText(configPath);
                config = Yaml.DefaultDeserializer.Deserialize<RawEnunuConfig>(configTxt);
                config.enunu_type = "ENUNU";
            } else {
                config = SetSimpleENUNUConfig(singer.Location);
                config.enunu_type = "SimpleENUNU";
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

    public class ExpressionDetail {
        public string Name { get; set; }
        public string Type { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }
        public float Default_Value { get; set; }
        public string Flag { get; set; }
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
        public Dictionary<string, Dictionary<string, string>> style_format = new Dictionary<string, Dictionary<string, string>>();
        public Dictionary<string, ExpressionDetail> styles = new Dictionary<string, ExpressionDetail>();
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
        public object style_format;
        public object styles;
    }

    class RawEnunuConfig {
        public string enunu_type = string.Empty;
        public string feature_type = string.Empty;
        public string tablePath = string.Empty;
        public string questionPath = string.Empty;
        public int sampleRate;
        public double framePeriod;
        public bool unload_subbanks;
        public RawEnunuExtensions extensions;

        public EnunuConfig Convert() {
            EnunuConfig enunuConfig = new EnunuConfig();
            enunuConfig.enunu_type = this.enunu_type;

            enunuConfig.feature_type = this.feature_type;
            enunuConfig.tablePath = this.tablePath;
            enunuConfig.questionPath = this.questionPath;
            enunuConfig.sampleRate = this.sampleRate;
            enunuConfig.framePeriod = this.framePeriod;
            enunuConfig.unLoadSubBanks = this.unload_subbanks;
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
                ParseEnunuStyleFormat(enunuConfig.extensions.style_format, this.extensions.style_format);
                ParseEnunuStyles(enunuConfig.extensions.styles, this.extensions.styles);
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

        private void ParseEnunuStyles(Dictionary<string, ExpressionDetail> enunuStyles, object rawEnunuStyle) {
            if (rawEnunuStyle is Dictionary<object, object> rawDict) {
                foreach (var kvp in rawDict) {
                    if (kvp.Key is string key && kvp.Value is Dictionary<object, object> innerDict) {
                        var exp = new ExpressionDetail();
                        foreach (var innerKvp in innerDict) {
                            if (innerKvp.Key is string innerKey && innerKvp.Value is string innerValue) {
                                switch (innerKey) {
                                    case "name":
                                        exp.Name = innerValue;
                                        break;
                                    case "type":
                                        exp.Type = innerValue;
                                        break;
                                    case "min":
                                        if (float.TryParse(innerValue, out var min)) {
                                            exp.Min = min;
                                        }
                                        break;
                                    case "max":
                                        if (float.TryParse(innerValue, out var max)) {
                                            exp.Max = max;
                                        }
                                        break;
                                    case "default_value":
                                        if (float.TryParse(innerValue, out var defaultValue)) {
                                            exp.Default_Value = defaultValue;
                                        }
                                        break;
                                    case "flag":
                                        exp.Flag = innerValue;
                                        break;
                                }
                            }
                        }
                        enunuStyles.Add(key, exp);
                    }
                }
            }
        }

        private void ParseEnunuStyleFormat(Dictionary<string, Dictionary<string, string>> enunuStyleFormat, object rawEnunuStyleFormat)
        {
            if (rawEnunuStyleFormat is Dictionary<object, object> rawDict)
            {
                foreach (var kvp in rawDict)
                {
                    if (kvp.Key is string key && kvp.Value is Dictionary<object, object> innerDict)
                    {
                        var innerFormattedDict = new Dictionary<string, string>();
                        foreach (var innerKvp in innerDict)
                        {
                            if (innerKvp.Key is string innerKey && innerKvp.Value is string innerValue)
                            {
                                innerFormattedDict.Add(innerKey, innerValue);
                            }
                        }
                        enunuStyleFormat.Add(key, innerFormattedDict);
                    }
                }
            }
        }
    }
}

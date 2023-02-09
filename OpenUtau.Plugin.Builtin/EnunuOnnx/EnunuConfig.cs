using System.Collections.Generic;
using System.IO;
using System.Text;
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
        public EnunuDurationConfig duration;
        public EnunuTimelagConfig timelag;

        public static EnunuConfig Load(string configPath, Encoding encoding = null) {
            encoding = encoding ?? Encoding.UTF8;
            var configTxt = File.ReadAllText(configPath,encoding);
            RawEnunuConfig config = Yaml.DefaultDeserializer.Deserialize<RawEnunuConfig>(configTxt);
            return config.Convert();
        }
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
            return enunuConfig;
        }
    }
}

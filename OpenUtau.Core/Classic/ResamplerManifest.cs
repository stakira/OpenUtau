using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Classic {
    public class ResamplerManifest {
        public Dictionary<string, UExpressionDescriptor> expressions = new Dictionary<string, UExpressionDescriptor> { };
        public bool expressionFilter = false;

        public ResamplerManifest() { }

        public static ResamplerManifest Load(string path) {
            return Yaml.DefaultDeserializer.Deserialize<ResamplerManifest>(
                File.ReadAllText(path, encoding: Encoding.UTF8)
                );
        }
    }
}

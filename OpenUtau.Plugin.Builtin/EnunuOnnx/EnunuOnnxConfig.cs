using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OpenUtau.Core;

//data class used to deserialize enunux.yaml, ENUNU X specific settings

namespace OpenUtau.Plugin.Builtin.EnunuOnnx {
    public struct RedirectionData {
        public string[] from;
        public string to;
    }

    class EnunuOnnxConfig {
        
        public RedirectionData[] redirections;

        public static EnunuOnnxConfig Load(string configPath, Encoding encoding = null) {
            encoding = encoding ?? Encoding.UTF8;
            var configTxt = File.ReadAllText(configPath, encoding);
            EnunuOnnxConfig config = Yaml.DefaultDeserializer.Deserialize<EnunuOnnxConfig>(configTxt);
            if(config.redirections == null) {
                config.redirections = new RedirectionData[] { };
            }
            return config;
        }
    }

    class RedirectionDict {
        //reference: https://stackoverflow.com/questions/1321331/replace-multiple-string-elements-in-c-sharp
        //if no redirection, regex is null
        Regex? regex = null;
        Dictionary<string, string> replacements = new Dictionary<string, string>();

        public RedirectionDict(RedirectionData[] datas) {
            if (datas == null || datas.Length == 0) {
                return;
            }
            //sort redirection keys from long to short
            Array.Sort(datas, (x1,x2)=>- x1.from.Length.CompareTo(x2.from.Length));
            StringBuilder regexBuilder = new StringBuilder("(");
            foreach(var line in datas) {
                string key = string.Join("\n", line.from);
                replacements[key] = line.to + new string('\n',line.from.Length - 1);
                regexBuilder.Append(Regex.Escape(key)+"|");
            }
            regexBuilder[^1] = ')';
            regex = new Regex(regexBuilder.ToString());
        }

        public string[] process(IEnumerable<string> symbols) {
            if (regex == null) {
                return symbols.ToArray();
            }
            string input = string.Join("\n", symbols);
            string output = regex.Replace(input, delegate (Match m) { return replacements[m.Value]; });
            return output.Split("\n");
        }
    }
}

using Serilog;
using System;
using System.Collections.Generic;
using System.IO;

using OpenUtau.Api;

namespace OpenUtau.Core.DiffSinger
{
    public class G2pReplacementsData{
        public struct Replacement{
            public string from;
            public string to;
        }
        public Replacement[]? replacements;
        
        public static G2pReplacementsData Load(string text){
            return OpenUtau.Core.Yaml.DefaultDeserializer.Deserialize<G2pReplacementsData>(text);
        }

        public Dictionary<string, string> toDict(){
            var dict = new Dictionary<string, string>();
            if(replacements!=null){
                foreach(var r in replacements){
                    dict[r.from] = r.to;
                }
            }
            return dict;
        }
    }

    public abstract class DiffSingerG2pPhonemizer : DiffSingerBasePhonemizer
    {
        protected virtual string GetDictionaryName()=>"dsdict.yaml";

        protected virtual IG2p LoadBaseG2p()=>null;
        //vowels and consonants of BaseG2p
        protected virtual string[] GetBaseG2pVowels()=>new string[]{};
        protected virtual string[] GetBaseG2pConsonants()=>new string[]{};
        
        protected override IG2p LoadG2p(string rootPath) {
            var dictionaryName = GetDictionaryName();
            var g2ps = new List<IG2p>();
            // Load dictionary from plugin folder.
            string path = Path.Combine(PluginDir, dictionaryName);
            if (File.Exists(path)) {
                try {
                    g2ps.Add(G2pDictionary.NewBuilder().Load(File.ReadAllText(path)).Build());
                } catch (Exception e) {
                    Log.Error(e, $"Failed to load {path}");
                }
            }

            // Load dictionary from singer folder.
            var replacements = new Dictionary<string,string>();
            string file = Path.Combine(rootPath, dictionaryName);
            if (File.Exists(file)) {
                try {
                    g2ps.Add(G2pDictionary.NewBuilder().Load(File.ReadAllText(file)).Build());
                    replacements = G2pReplacementsData.Load(File.ReadAllText(file)).toDict();
                } catch (Exception e) {
                    Log.Error(e, $"Failed to load {file}");
                }
            }

            // Load base g2p.
            var baseG2p = LoadBaseG2p();
            if(baseG2p == null){
                return new G2pFallbacks(g2ps.ToArray());
            }
            var phonemeSymbols = new Dictionary<string, bool>();
            foreach(var v in GetBaseG2pVowels()){
                phonemeSymbols[v]=true;
            }
            foreach(var c in GetBaseG2pConsonants()){
                phonemeSymbols[c]=false;
            }
            foreach(var from in replacements.Keys){
                var to = replacements[from];
                if(baseG2p.IsValidSymbol(to)){
                    if(baseG2p.IsVowel(to)){
                        phonemeSymbols[from]=true;
                    }else{
                        phonemeSymbols[from]=false;
                    }
                }
            }
            g2ps.Add(new G2pRemapper(baseG2p,phonemeSymbols, replacements));
            return new G2pFallbacks(g2ps.ToArray());
        }
    }
}

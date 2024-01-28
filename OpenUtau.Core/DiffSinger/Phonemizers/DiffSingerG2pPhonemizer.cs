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

    /// <summary>
    /// Base class for DiffSinger phonemizers based on OpenUtau's builtin G2p.
    /// </summary>
    public abstract class DiffSingerG2pPhonemizer : DiffSingerBasePhonemizer
    {
        protected virtual IG2p LoadBaseG2p()=>null;
        //vowels and consonants of BaseG2p
        protected virtual string[] GetBaseG2pVowels()=>new string[]{};
        protected virtual string[] GetBaseG2pConsonants()=>new string[]{};
        
        protected override IG2p LoadG2p(string rootPath) {
            //Each phonemizer has a delicated dictionary name, such as dsdict-en.yaml, dsdict-ru.yaml.
            //If this dictionary exists, load it.
            //If not, load dsdict.yaml.
            var dictionaryNames = new string[] {GetDictionaryName(), "dsdict.yaml"};
            var g2ps = new List<IG2p>();

            // Load dictionary from singer folder.
            var replacements = new Dictionary<string,string>();
            foreach(var dictionaryName in dictionaryNames){
                string dictionaryPath = Path.Combine(rootPath, dictionaryName);
                if (File.Exists(dictionaryPath)) {
                    try {
                        g2ps.Add(G2pDictionary.NewBuilder().Load(File.ReadAllText(dictionaryPath)).Build());
                        replacements = G2pReplacementsData.Load(File.ReadAllText(dictionaryPath)).toDict();
                    } catch (Exception e) {
                        Log.Error(e, $"Failed to load {dictionaryPath}");
                    }
                    break;
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
